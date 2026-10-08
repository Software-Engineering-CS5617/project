namespace FileSync;

public class MainForm : Form
{
    private readonly SyncService _sync;
    private readonly SyncServer _server;
    private readonly int _peerPort;

    private readonly TextBox _hostBox = new() { Text = "127.0.0.1", Left = 12, Top = 40, Width = 180 };
    private readonly Button _syncButton = new() { Text = "File Sync", Left = 210, Top = 38, Width = 160, Height = 28 };
    private readonly ListBox _log = new() { Left = 12, Top = 80, Width = 358, Height = 160 };

    public MainForm(string folder, int listenPort, int peerPort)
    {
        _peerPort = peerPort;
        _sync = new SyncService(folder);
        _server = new SyncServer(folder, listenPort);

        Text = $"FileSync - listening on {listenPort}";
        ClientSize = new Size(384, 256);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        Controls.Add(new Label { Text = $"Peer host (peer port {peerPort}):", Left = 12, Top = 14, Width = 358 });
        Controls.Add(_hostBox);
        Controls.Add(_syncButton);
        Controls.Add(_log);

        _syncButton.Click += OnSyncClick;
        Load += (_, _) =>
        {
            _server.Start();
            Log($"Folder: {Path.GetFullPath(folder)}");
        };
        FormClosing += (_, _) => _server.Dispose();
    }

    private async void OnSyncClick(object? sender, EventArgs e)
    {
        _syncButton.Enabled = false;
        try
        {
            int n = await _sync.SyncAsync(_hostBox.Text.Trim(), _peerPort);
            Log($"Sync done - {n} file(s) updated.");
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
        }
        finally
        {
            _syncButton.Enabled = true;
        }
    }

    private void Log(string msg) => _log.Items.Add($"{DateTime.Now:HH:mm:ss}  {msg}");
}
