using System.Diagnostics.CodeAnalysis;
using Cloud.Crash;

namespace Cloud.UI.Views;

/// <summary>
/// Navigation parameter of <see cref="CrashDashboardPage"/>.
/// </summary>
/// <param name="Service">The service that reads crash data.</param>
[ExcludeFromCodeCoverage]
public sealed record CrashPageContext(ICrashReportService Service);

/// <summary>
/// Navigation parameter of <see cref="CrashDetailsPage"/>.
/// </summary>
/// <param name="Service">The service that reads crash data.</param>
/// <param name="Group">The group to show.</param>
[ExcludeFromCodeCoverage]
public sealed record CrashDetailsPageParameters(ICrashReportService Service, CrashGroup Group);
