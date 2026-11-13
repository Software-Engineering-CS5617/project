using System.Text;
using System.Windows;
using System.Windows.Controls;
using Cloud.Persistence;

namespace Cloud.Demo;

/// <summary>
/// Demo window that exercises every <see cref="IStorage"/> method.
/// </summary>
public partial class MainWindow : Window
{
    private readonly Dictionary<ModuleType, IStorage> _storages = [];
    private Dictionary<string, string> _savedEtags = [];

    /// <summary>
    /// Initializes the window and fills the module list.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        ModuleBox.ItemsSource = Enum.GetValues<ModuleType>();
        ModuleBox.SelectedItem = ModuleType.Chat;
    }

    private static byte[] ToBytes(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }

    private static string ToText(byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    private async Task<IStorage> GetStorageAsync()
    {
        ModuleType module = (ModuleType)ModuleBox.SelectedItem;

        if (!_storages.TryGetValue(module, out IStorage? storage))
        {
            storage = await StorageFactory.CreateAsync(module);
            _storages[module] = storage;
        }

        return storage;
    }

    private Dictionary<string, string> ParseBulkLines()
    {
        Dictionary<string, string> items = [];

        foreach (string line in BulkBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int index = line.IndexOf('=');
            if (index > 0)
            {
                items[line[..index].Trim()] = line[(index + 1)..].Trim();
            }
        }

        return items;
    }

    private void Log(string operation, bool success, string details)
    {
        string entry =
            $"{DateTime.Now:HH:mm:ss}  {operation}  ->  {(success ? "SUCCESS" : "FALSE / ERROR")}\n{details}\n\n";
        ResultBox.Text = entry + ResultBox.Text;
    }

    private void ClearClick(object sender, RoutedEventArgs e)
    {
        ResultBox.Clear();
    }

    private async void OpClick(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        string operation = (string)button.Tag;
        button.IsEnabled = false;

        try
        {
            IStorage storage = await GetStorageAsync();
            Dictionary<string, string> lines = ParseBulkLines();
            string[] keys = [.. lines.Keys];
            Dictionary<string, byte[]> bytes = lines.ToDictionary(p => p.Key, p => ToBytes(p.Value));

            switch (operation)
            {
                case "create":
                    bool created = await storage.Create(KeyBox.Text, ToBytes(ValueBox.Text));
                    Log("Create", created, created ? "Item created." : "Key already exists or call failed.");
                    break;

                case "read":
                    (bool readOk, byte[] data) = await storage.Read(KeyBox.Text);
                    if (readOk)
                    {
                        ValueBox.Text = ToText(data);
                    }

                    Log("Read", readOk, readOk ? ToText(data) : "Key not found.");
                    break;

                case "readEtag":
                    (bool etagOk, string etag) = await storage.ReadEtag(KeyBox.Text);
                    if (etagOk)
                    {
                        EtagBox.Text = etag;
                    }

                    Log("ReadEtag", etagOk, etagOk ? etag : "Key not found.");
                    break;

                case "update":
                    bool updated = await storage.Update(KeyBox.Text, ToBytes(ValueBox.Text), EtagBox.Text);
                    Log("Update", updated, updated ? "Item updated." : "Failed: stale ETag or missing key.");
                    break;

                case "delete":
                    bool deleted = await storage.Delete(KeyBox.Text);
                    Log("Delete", deleted, deleted ? "Item deleted." : "Key not found.");
                    break;

                case "createAll":
                    bool createdAll = await storage.CreateAll(bytes);
                    Log("CreateAll", createdAll, $"{bytes.Count} item(s) requested.");
                    break;

                case "readAll":
                    (bool readAllOk, Dictionary<string, byte[]> found) = await storage.ReadAll(keys);
                    Log(
                        "ReadAll",
                        readAllOk,
                        readAllOk
                            ? string.Join("\n", found.Select(p => $"{p.Key} = {ToText(p.Value)}"))
                            : "At least one key is missing.");
                    break;

                case "readAllEtags":
                    (bool etagsOk, Dictionary<string, string> etags) = await storage.ReadAllEtags();
                    if (etagsOk)
                    {
                        _savedEtags = etags;
                    }

                    Log(
                        "ReadAllEtags",
                        etagsOk,
                        string.Join("\n", etags.Select(p => $"{p.Key}  {p.Value}")));
                    break;

                case "getAllKeys":
                    (bool keysOk, List<string> allKeys) = await storage.GetAllKeys();
                    Log("GetAllKeys", keysOk, string.Join("\n", allKeys));
                    break;

                case "updateAll":
                    bool updatedAll = await storage.UpdateAll(bytes, _savedEtags);
                    Log(
                        "UpdateAll",
                        updatedAll,
                        updatedAll ? "All items updated." : "Failed: stale or missing ETag. Run ReadAllEtags first.");
                    break;

                case "deleteAll":
                    bool deletedAll = await storage.DeleteAll(keys);
                    Log("DeleteAll", deletedAll, deletedAll ? "All items deleted." : "At least one key was missing.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log(operation, false, ex.InnerException?.Message ?? ex.Message);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}