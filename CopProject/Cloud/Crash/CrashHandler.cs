using System.Diagnostics;

namespace Cloud.Crash;

/// <summary>
/// Catches unhandled exceptions, saves a crash report locally and tries to upload it.
/// Reporting never throws, because it runs while the application is failing.
/// </summary>
public sealed class CrashHandler : IDisposable
{
    /// <summary>
    /// How long a crash waits for the upload before it gives up and keeps the local copy.
    /// </summary>
    public static readonly TimeSpan DefaultUploadTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The most reports one process sends, so an exception storm cannot flood the cloud.
    /// </summary>
    public const int MaxReportsPerSession = 20;

    private static readonly object s_installLock = new();
    private static CrashHandler? s_current;

    [ThreadStatic]
    private static bool s_isReporting;

    private readonly LocalCrashStore _store;
    private readonly ICrashUploader _uploader;
    private readonly TimeSpan _uploadTimeout;
    private int _reportCount;
    private bool _attached;
    private bool _disposed;

    /// <summary>
    /// Creates a handler. Call <see cref="Attach"/> to start catching exceptions, or use <see cref="Install"/>.
    /// </summary>
    /// <param name="store">Where reports are saved when the upload fails.</param>
    /// <param name="uploader">Sends reports to the cloud.</param>
    /// <param name="uploadTimeout">How long to wait for an upload; defaults to <see cref="DefaultUploadTimeout"/>.</param>
    public CrashHandler(LocalCrashStore store, ICrashUploader uploader, TimeSpan? uploadTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(uploader);

        _store = store;
        _uploader = uploader;
        _uploadTimeout = uploadTimeout ?? DefaultUploadTimeout;
        PendingUploader = new CrashPendingUploader(store, uploader);
    }

    /// <summary>
    /// Gets the handler created by <see cref="Install"/>, or null if none is installed.
    /// </summary>
    public static CrashHandler? Current
    {
        get
        {
            lock (s_installLock)
            {
                return s_current;
            }
        }
    }

    /// <summary>
    /// Gets or sets the ID of the current user, added to every report.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets the uploader for reports that are waiting on disk.
    /// </summary>
    public CrashPendingUploader PendingUploader { get; }

    /// <summary>
    /// Creates the handler with the default store and the Cosmos uploader, hooks it into the process and starts
    /// uploading reports left over from earlier runs. Calling it again returns the same handler.
    /// </summary>
    /// <param name="userId">The ID of the current user, if known.</param>
    /// <returns>The installed handler.</returns>
    public static CrashHandler Install(string? userId = null)
    {
        CrashHandler handler;

        lock (s_installLock)
        {
            if (s_current is not null)
            {
                Trace.TraceInformation("CrashHandler.Install: reusing the existing crash handler.");
                s_current.UserId = userId ?? s_current.UserId;
                return s_current;
            }

            Trace.TraceInformation("CrashHandler.Install: creating and attaching the crash handler.");
            handler = new CrashHandler(new LocalCrashStore(), new CosmosCrashUploader());
            handler.UserId = userId;
            handler.Attach();
            s_current = handler;
        }

        handler.FlushPendingInBackground();
        return handler;
    }

    /// <summary>
    /// Subscribes to unhandled exceptions of the current AppDomain and unobserved task exceptions.
    /// WinUI exceptions are not covered here; the UI project forwards them to <see cref="Report"/>.
    /// </summary>
    public void Attach()
    {
        if (_attached || _disposed)
        {
            Trace.TraceWarning("CrashHandler.Attach: attach skipped because the handler is already attached or disposed.");
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        _attached = true;
        Trace.TraceInformation("CrashHandler.Attach: exception handlers attached.");
    }

    /// <summary>
    /// Captures an exception, saves it locally and tries to upload it within the upload timeout.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="isFatal">Whether the process is terminating because of it.</param>
    /// <param name="source">The hook that caught it, for example "WinUI".</param>
    /// <returns>What happened to the report.</returns>
    public CrashReportOutcome Report(Exception exception, bool isFatal = false, string source = "Manual")
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_disposed || s_isReporting || Interlocked.Increment(ref _reportCount) > MaxReportsPerSession)
        {
            Trace.TraceWarning($"CrashHandler.Report: report skipped for source '{source}'.");
            return CrashReportOutcome.Skipped;
        }

        s_isReporting = true;

        try
        {
            Trace.TraceInformation($"CrashHandler.Report: building report for source '{source}', fatal={isFatal}.");
            CrashReport report = CrashReportBuilder.Build(exception, isFatal, source, UserId);
            byte[] xml = CrashXmlSerializer.Serialize(report);

            // Save first: the process may die before an upload finishes.
            string? path = TrySave(report, xml);

            if (TryUpload(report.Id, xml))
            {
                if (path is not null)
                {
                    _store.Delete(path);
                }

                Trace.TraceInformation($"CrashHandler.Report: report {report.Id} uploaded successfully.");
                return CrashReportOutcome.Uploaded;
            }

            Trace.TraceWarning($"CrashHandler.Report: report {report.Id} was not uploaded; localPathPresent={path is not null}.");
            return path is null ? CrashReportOutcome.Failed : CrashReportOutcome.SavedLocally;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"CrashHandler.Report: could not report {exception.GetType().Name}: {ex.Message}");
            return CrashReportOutcome.Failed;
        }
        finally
        {
            s_isReporting = false;
        }
    }

    /// <summary>
    /// Starts uploading reports left over from earlier runs without blocking the caller.
    /// </summary>
    public void FlushPendingInBackground()
    {
        Trace.TraceInformation("CrashHandler.FlushPendingInBackground: starting a background flush.");
        _ = Task.Run(FlushQuietlyAsync);
    }

    /// <summary>
    /// Unsubscribes from the process hooks. Reports after this are skipped.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            Trace.TraceWarning("CrashHandler.Dispose: dispose skipped because the handler is already disposed.");
            return;
        }

        _disposed = true;

        if (_attached)
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            _attached = false;
        }

        lock (s_installLock)
        {
            if (ReferenceEquals(s_current, this))
            {
                s_current = null;
            }
        }

        Trace.TraceInformation("CrashHandler.Dispose: exception handlers detached.");
    }

    internal void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        Trace.TraceError($"CrashHandler.OnUnhandledException: unhandled exception received; terminating={e.IsTerminating}.");
        Exception exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException("A non-exception object was thrown: " + e.ExceptionObject?.GetType().FullName);

        Report(exception, e.IsTerminating, "AppDomain");
    }

    internal void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Trace.TraceError("CrashHandler.OnUnobservedTaskException: unobserved task exception received.");
        Report(e.Exception, false, "TaskScheduler");
    }

    private async Task FlushQuietlyAsync()
    {
        try
        {
            Trace.TraceInformation("CrashHandler.FlushQuietlyAsync: flushing pending reports.");
            await PendingUploader.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"CrashHandler.FlushQuietlyAsync: flushing pending reports failed: {ex.Message}");
        }
    }

    private string? TrySave(CrashReport report, byte[] xml)
    {
        try
        {
            Trace.TraceInformation($"CrashHandler.TrySave: saving report {report.Id} locally.");
            return _store.Save(report, xml);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"CrashHandler.TrySave: could not save report locally: {ex.Message}");
            return null;
        }
    }

    private bool TryUpload(Guid reportId, byte[] xml)
    {
        try
        {
            Trace.TraceInformation($"CrashHandler.TryUpload: uploading report {reportId} with a {_uploadTimeout} timeout.");
            CancellationTokenSource timeout = new(_uploadTimeout);

            // Task.Run keeps the upload off the crashing thread's synchronization context, so waiting here cannot deadlock.
            Task<bool> work = Task.Run(() => _uploader.UploadAsync(reportId, xml, timeout.Token));

            // Observe a late failure so it does not surface as an unobserved task exception.
            _ = work.ContinueWith(
                static task => { _ = task.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            return work.Wait(_uploadTimeout) && work.Result;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"CrashHandler.TryUpload: upload failed: {ex.Message}");
            return false;
        }
    }
}
