// Author : Murali

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using FileSynchroniser.Models;
using FileSynchroniser.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Utils;
using static FileSynchroniser.ViewModels.UiDispatcher;

namespace Filesync.Test.ViewModels;

[TestClass]
public sealed class FileSyncViewModelTest
{
    private Synchroniser _sync = null!;
    private FileSyncViewModel _viewModel = null!;
    private string _testDir = null!;

    /// <summary>
    /// Path.GetRelativePath returns OS-native separators (backslashes on Windows),
    /// while the tests use forward slashes, so normalise before comparing.
    /// </summary>
    private static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Builds the one-entry dictionary that AddFile / SaveFile expect for a single file.
    /// </summary>
    private static Dictionary<string, byte[]> OneFile(string path, byte[] content)
    {
        return new Dictionary<string, byte[]> { [path] = content };
    }

    /// <summary>
    /// Initializing Synchroniser and FileSyncViewModel for testing
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _sync = new Synchroniser();
        _testDir = $"Tests/{Guid.NewGuid():N}";

        _viewModel = new FileSyncViewModel(_sync, new InlineUiDispatcher());
    }

    /// <summary>
    /// Clean up once test is completed
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _viewModel.Dispose();

        string fullPath = Path.Combine(_sync.GetDirectory(), _testDir);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
    }

    /// <summary>
    /// 1. Constructor throws ArgumentNullException when synchroniser is null
    /// </summary>
    [TestMethod]
    public void Constructor_NullSynchroniser_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new FileSyncViewModel(null!));
    }

    /// <summary>
    /// 2. Constructor initializes the root directory
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesRootDirectory()
    {
        Assert.AreEqual(_sync.GetDirectory(), _viewModel.RootDirectory);
    }

    /// <summary>
    /// 3. Constructor initializes the status message
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesStatusMessage()
    {
        Assert.AreEqual("Ready", _viewModel.StatusMessage);
    }

    /// <summary>
    /// 4. Constructor initializes HasError as false
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesHasErrorAsFalse()
    {
        Assert.IsFalse(_viewModel.HasError);
    }

    /// <summary>
    /// 5. Constructor initializes an empty file collection
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesEmptyFilesCollection()
    {
        Assert.IsNotNull(_viewModel.Files);
        Assert.IsEmpty(_viewModel.Files);
    }

    /// <summary>
    /// 6. AddFile saves a file and adds it to the collection
    /// </summary>
    [TestMethod]
    public void AddFile_ValidFile_AddsFileToCollection()
    {
        string path = $"{_testDir}/test.txt";
        byte[] content = Encoding.UTF8.GetBytes("hello world");

        _viewModel.AddFile(OneFile(path, content));

        Assert.HasCount(1, _viewModel.Files);
        Assert.AreEqual(path, Normalize(_viewModel.Files[0].RelativePath));
    }

    /// <summary>
    /// 7. AddFile updates the status message after successful save
    /// </summary>
    [TestMethod]
    public void AddFile_ValidFile_UpdatesStatusMessage()
    {
        string path = $"{_testDir}/test.txt";

        _viewModel.AddFile(OneFile(path, Encoding.UTF8.GetBytes("content")));

        Assert.AreEqual($"Saved '{path}'.", _viewModel.StatusMessage);
    }

    /// <summary>
    /// 8. AddFile clears HasError before saving
    /// </summary>
    [TestMethod]
    public void AddFile_ValidFile_ClearsHasError()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/test.txt", Encoding.UTF8.GetBytes("content")));

        Assert.IsFalse(_viewModel.HasError);
    }

    /// <summary>
    /// 9. AddFile handles an invalid file path without throwing
    /// </summary>
    [TestMethod]
    public void AddFile_InvalidPath_DoesNotThrow()
    {
        _viewModel.AddFile(OneFile("../invalid.txt", Encoding.UTF8.GetBytes("content")));

        Assert.IsTrue(_viewModel.HasError);
        StringAssert.Contains(_viewModel.StatusMessage, "invalid.txt");
    }

    /// <summary>
    /// 10. RefreshCommand reloads the file collection
    /// </summary>
    [TestMethod]
    public void RefreshCommand_WithFiles_LoadsFiles()
    {
        _sync.SaveFile(OneFile($"{_testDir}/test.txt", Encoding.UTF8.GetBytes("content")));

        _viewModel.RefreshCommand.Execute(null);

        Assert.HasCount(1, _viewModel.Files);
        Assert.AreEqual($"{_testDir}/test.txt", Normalize(_viewModel.Files[0].RelativePath));
    }

    /// <summary>
    /// 11. RefreshCommand clears HasError
    /// </summary>
    [TestMethod]
    public void RefreshCommand_ClearsHasError()
    {
        _viewModel.RefreshCommand.Execute(null);

        Assert.IsFalse(_viewModel.HasError);
    }

    /// <summary>
    /// 12. RefreshCommand can be executed multiple times
    /// </summary>
    [TestMethod]
    public void RefreshCommand_CalledMultipleTimes_DoesNotThrow()
    {
        _viewModel.RefreshCommand.Execute(null);
        _viewModel.RefreshCommand.Execute(null);

        Assert.IsFalse(_viewModel.HasError);
    }

    /// <summary>
    /// 13. DeleteSelectedCommand cannot execute when no file is selected
    /// </summary>
    [TestMethod]
    public void DeleteSelectedCommand_NoSelection_ReturnsFalse()
    {
        Assert.IsNull(_viewModel.SelectedFile);

        Assert.IsFalse(_viewModel.DeleteSelectedCommand.CanExecute(null));
    }

    /// <summary>
    /// 14. DeleteSelectedCommand can execute when a file is selected
    /// </summary>
    [TestMethod]
    public void DeleteSelectedCommand_WithSelection_ReturnsTrue()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/test.txt", Encoding.UTF8.GetBytes("content")));

        _viewModel.SelectedFile = _viewModel.Files[0];

        Assert.IsTrue(_viewModel.DeleteSelectedCommand.CanExecute(null));
    }

    /// <summary>
    /// 15. DeleteSelectedCommand deletes the selected file
    /// </summary>
    [TestMethod]
    public void DeleteSelectedCommand_WithSelection_DeletesFile()
    {
        string path = $"{_testDir}/test.txt";

        _viewModel.AddFile(OneFile(path, Encoding.UTF8.GetBytes("content")));

        _viewModel.SelectedFile = _viewModel.Files[0];

        _viewModel.DeleteSelectedCommand.Execute(null);

        Assert.IsEmpty(_viewModel.Files);
        Assert.IsFalse(File.Exists(Path.Combine(_sync.GetDirectory(), path)));
    }

    /// <summary>
    /// 16. DeleteSelectedCommand updates the status message after deletion
    /// </summary>
    [TestMethod]
    public void DeleteSelectedCommand_WithSelection_UpdatesStatusMessage()
    {
        string path = $"{_testDir}/test.txt";

        _viewModel.AddFile(OneFile(path, Encoding.UTF8.GetBytes("content")));

        _viewModel.SelectedFile = _viewModel.Files[0];

        // Capture before deleting: SelectedFile is reset to null once the list reloads.
        string selectedPath = _viewModel.SelectedFile.RelativePath;

        _viewModel.DeleteSelectedCommand.Execute(null);

        Assert.AreEqual($"Deleted '{selectedPath}'.", _viewModel.StatusMessage);
        Assert.AreEqual(path, Normalize(selectedPath));
    }

    /// <summary>
    /// 17. DeleteSelectedCommand does nothing when no file is selected
    /// </summary>
    [TestMethod]
    public void DeleteSelectedCommand_NoSelection_DoesNothing()
    {
        _viewModel.DeleteSelectedCommand.Execute(null);

        Assert.IsEmpty(_viewModel.Files);
    }

    /// <summary>
    /// 18. SelectedFile raises PropertyChanged when its value changes
    /// </summary>
    [TestMethod]
    public void SelectedFile_Changed_RaisesPropertyChanged()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/test.txt", Encoding.UTF8.GetBytes("content")));

        string? propertyName = null;

        _viewModel.PropertyChanged += (sender, args) => propertyName = args.PropertyName;

        _viewModel.SelectedFile = _viewModel.Files[0];

        Assert.AreEqual(nameof(_viewModel.SelectedFile), propertyName);
    }

    /// <summary>
    /// 19. SelectedFile raises CanExecuteChanged when its value changes
    /// </summary>
    [TestMethod]
    public void SelectedFile_Changed_RaisesCanExecuteChanged()
    {
        bool eventRaised = false;

        _viewModel.DeleteSelectedCommand.CanExecuteChanged += (sender, args) => eventRaised = true;

        _viewModel.AddFile(OneFile($"{_testDir}/test.txt", Encoding.UTF8.GetBytes("content")));

        _viewModel.SelectedFile = _viewModel.Files[0];

        Assert.IsTrue(eventRaised);
    }

    /// <summary>
    /// 20. SelectedFile does not raise PropertyChanged when assigned the same value
    /// </summary>
    [TestMethod]
    public void SelectedFile_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/test.txt", Encoding.UTF8.GetBytes("content")));

        SyncFileItem file = _viewModel.Files[0];

        _viewModel.SelectedFile = file;

        bool eventRaised = false;

        _viewModel.PropertyChanged += (sender, args) => eventRaised = true;

        _viewModel.SelectedFile = file;

        Assert.IsFalse(eventRaised);
    }

    /// <summary>
    /// 21. Dispose unsubscribes synchronisation events
    /// </summary>
    [TestMethod]
    public void Dispose_UnsubscribesSynchronisationEvents()
    {
        _viewModel.Dispose();

        int propertyChangedCount = 0;

        _viewModel.PropertyChanged += (sender, args) => propertyChangedCount++;

        _sync.OnSyncComplete += () => { };

        _sync.Synchronise();

        Assert.AreEqual(0, propertyChangedCount);
    }

    /// <summary>
    /// 22. LoadFiles displays file size information
    /// </summary>
    [TestMethod]
    public void AddFile_ValidFile_SetsSizeText()
    {
        byte[] content = Encoding.UTF8.GetBytes("hello");

        _viewModel.AddFile(OneFile($"{_testDir}/test.txt", content));

        Assert.AreEqual("5 B", _viewModel.Files[0].SizeText);
    }

    /// <summary>
    /// 23. LoadFiles displays last modified information
    /// </summary>
    [TestMethod]
    public void AddFile_ValidFile_SetsLastModifiedText()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/test.txt", Encoding.UTF8.GetBytes("content")));

        Assert.IsFalse(string.IsNullOrWhiteSpace(_viewModel.Files[0].LastModifiedText));
    }

    /// <summary>
    /// 24. LoadFiles shows an error when the folder listing throws an IOException
    /// </summary>
    [TestMethod]
    public void Constructor_EnumerationThrowsIOException_SetsError()
    {
        using FileSyncViewModel viewModel = new(_sync, new InlineUiDispatcher(), _ => throw new IOException("disk failure"));

        Assert.IsTrue(viewModel.HasError);
        Assert.AreEqual("disk failure", viewModel.StatusMessage);
        Assert.IsEmpty(viewModel.Files);
    }

    /// <summary>
    /// 25. LoadFiles shows an error when access to the folder is denied
    /// </summary>
    [TestMethod]
    public void Constructor_EnumerationThrowsUnauthorizedAccess_SetsError()
    {
        using FileSyncViewModel viewModel = new(_sync, new InlineUiDispatcher(), _ => throw new UnauthorizedAccessException("access denied"));

        Assert.IsTrue(viewModel.HasError);
        Assert.AreEqual("access denied", viewModel.StatusMessage);
        Assert.IsEmpty(viewModel.Files);
    }

    /// <summary>
    /// 26. LoadFiles does not swallow unexpected exceptions
    /// </summary>
    [TestMethod]
    public void Constructor_EnumerationThrowsUnexpectedException_Propagates()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => new FileSyncViewModel(_sync, new InlineUiDispatcher(), _ => throw new InvalidOperationException("unexpected")));
    }

    /// <summary>
    /// 27. Add File of 1 KB and use SetSize in Text
    /// </summary>
    [TestMethod]
    public void AddFile_OneKilobyte_SetsSizeTextInKb()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/kb.bin", new byte[1024]));

        Assert.AreEqual("1 KB", _viewModel.Files[0].SizeText);
    }

    /// <summary>
    /// 28. Add File of fraction KB and use SetSize to round to whole number size
    /// </summary>
    [TestMethod]
    public void AddFile_FractionalKilobytes_SetsRoundedSizeText()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/frac.bin", new byte[1536]));

        Assert.AreEqual("1.5 KB", _viewModel.Files[0].SizeText);
    }

    /// <summary>
    /// 29. Add File of 1 MB and use SetSize in Text
    /// </summary>
    [TestMethod]
    public void AddFile_OneMegabyte_SetsSizeTextInMb()
    {
        _viewModel.AddFile(OneFile($"{_testDir}/mb.bin", new byte[1024 * 1024]));

        Assert.AreEqual("1 MB", _viewModel.Files[0].SizeText);
    }

    /// <summary>
    /// 30. Sync events are marshalled through the UI dispatcher
    /// </summary>
    [TestMethod]
    public void RefreshCommand_RoutesSyncEventsThroughDispatcher()
    {
        Mock<IUiDispatcher> dispatcher = new();
        dispatcher.Setup(d => d.Run(It.IsAny<Action>())).Callback<Action>(a => a());

        using FileSyncViewModel viewModel = new(_sync, dispatcher.Object);

        viewModel.RefreshCommand.Execute(null);

        dispatcher.Verify(d => d.Run(It.IsAny<Action>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// 31. AddFile with several files adds every file to the collection
    /// </summary>
    [TestMethod]
    public void AddFile_MultipleFiles_AddsAllToCollection()
    {
        var files = new Dictionary<string, byte[]> {
            [$"{_testDir}/a.txt"] = Encoding.UTF8.GetBytes("A"),
            [$"{_testDir}/b.txt"] = Encoding.UTF8.GetBytes("B"),
            [$"{_testDir}/c.txt"] = Encoding.UTF8.GetBytes("C")
        };

        _viewModel.AddFile(files);

        Assert.HasCount(3, _viewModel.Files);
        Assert.IsFalse(_viewModel.HasError);
    }

    /// <summary>
    /// 32. AddFile with several files reports how many were saved
    /// </summary>
    [TestMethod]
    public void AddFile_MultipleFiles_UpdatesStatusMessageWithCount()
    {
        var files = new Dictionary<string, byte[]> {
            [$"{_testDir}/a.txt"] = Encoding.UTF8.GetBytes("A"),
            [$"{_testDir}/b.txt"] = Encoding.UTF8.GetBytes("B")
        };

        _viewModel.AddFile(files);

        Assert.AreEqual("Saved 2 files.", _viewModel.StatusMessage);
    }

    /// <summary>
    /// 33. AddFile with a mix of valid and invalid files saves the valid ones,
    /// reloads the list and shows the error
    /// </summary>
    [TestMethod]
    public void AddFile_PartialFailure_ReloadsListAndShowsError()
    {
        var files = new Dictionary<string, byte[]> {
            [$"{_testDir}/good.txt"] = Encoding.UTF8.GetBytes("good"),
            ["../bad.txt"] = Encoding.UTF8.GetBytes("bad")
        };

        _viewModel.AddFile(files);

        Assert.HasCount(1, _viewModel.Files);
        Assert.AreEqual($"{_testDir}/good.txt", Normalize(_viewModel.Files[0].RelativePath));
        Assert.IsTrue(_viewModel.HasError);
        StringAssert.Contains(_viewModel.StatusMessage, "bad.txt");
    }

    /// <summary>
    /// 34. AddFile for a file that already exists shows an error and keeps the list unchanged
    /// </summary>
    [TestMethod]
    public void AddFile_ExistingFile_SetsErrorAndDoesNotReload()
    {
        string path = $"{_testDir}/dup.txt";

        _viewModel.AddFile(OneFile(path, Encoding.UTF8.GetBytes("first")));
        _viewModel.SelectedFile = _viewModel.Files[0];

        _viewModel.AddFile(OneFile(path, Encoding.UTF8.GetBytes("second")));

        Assert.IsTrue(_viewModel.HasError);
        Assert.HasCount(1, _viewModel.Files);
        // The list was not reloaded, so the selection is kept.
        Assert.IsNotNull(_viewModel.SelectedFile);
    }

    /// <summary>
    /// 35. AddFile throws ArgumentNullException when the collection is null
    /// </summary>
    [TestMethod]
    public void AddFile_NullCollection_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _viewModel.AddFile(null!));
    }

    /// <summary>
    /// 36. AddFile with an empty collection does nothing
    /// </summary>
    [TestMethod]
    public void AddFile_EmptyCollection_DoesNothing()
    {
        _viewModel.AddFile(new Dictionary<string, byte[]>());

        Assert.AreEqual("Ready", _viewModel.StatusMessage);
        Assert.IsFalse(_viewModel.HasError);
        Assert.IsEmpty(_viewModel.Files);
    }

    /// <summary>
    /// 37. DeleteSelectedCommand shows an error when the file can no longer be deleted
    /// </summary>
    [TestMethod]
    public void DeleteSelectedCommand_FileAlreadyGone_SetsError()
    {
        string path = $"{_testDir}/vanishing.txt";

        _viewModel.AddFile(OneFile(path, Encoding.UTF8.GetBytes("content")));
        _viewModel.SelectedFile = _viewModel.Files[0];

        // Remove the file behind the view model's back.
        _sync.DeleteFile(new[] { path });

        _viewModel.DeleteSelectedCommand.Execute(null);

        Assert.IsTrue(_viewModel.HasError);
        StringAssert.Contains(_viewModel.StatusMessage, "vanishing.txt");
    }

    /// <summary>
    /// Creates a peer with a unique key (assumes UserKey is derived from the email).
    /// </summary>
    private static ControllerUser MakeUser(string name, int port, string ip = "192.168.1.50", bool isHost = false)
    {
        return new ControllerUser(name, $"{name.ToLower()}@test.com", null, ip, port, isHost);
    }

    /// <summary>
    /// 38. SetAvailableUsers populates AvailablePeers collection and selects first
    /// </summary>
    [TestMethod]
    public void SetAvailableUsers_PopulatesPeersAndSelectsFirst()
    {
        var users = new List<ControllerUser>
        {
            MakeUser("Alice", 8001, "192.168.1.10", isHost: true),
            MakeUser("Bob", 8002, "192.168.1.11")
        };

        _viewModel.SetAvailableUsers(users);

        Assert.HasCount(2, _viewModel.AvailablePeers);
        Assert.AreEqual("Alice", _viewModel.SelectedPeer?.Username);
        Assert.AreEqual("192.168.1.10", _viewModel.SelectedPeer?.Ip);
        Assert.AreEqual(8001, _viewModel.SelectedPeer?.Port);
    }

    /// <summary>
    /// 39. OnUserJoined adds a new user to AvailablePeers
    /// </summary>
    [TestMethod]
    public void OnUserJoined_AddsNewPeer()
    {
        ControllerUser user = MakeUser("Charlie", 8003);

        _viewModel.OnUserJoined(user);

        Assert.HasCount(1, _viewModel.AvailablePeers);
        Assert.AreEqual("Charlie", _viewModel.AvailablePeers[0].Username);
        Assert.AreSame(user, _viewModel.SelectedPeer);
    }

    /// <summary>
    /// 40. OnUserJoined avoids adding duplicate user with same key
    /// </summary>
    [TestMethod]
    public void OnUserJoined_DuplicateUser_IsNotAddedTwice()
    {
        ControllerUser user1 = MakeUser("Dan", 8004);
        ControllerUser user2 = MakeUser("Dan", 8004);

        _viewModel.OnUserJoined(user1);
        _viewModel.OnUserJoined(user2);

        Assert.HasCount(1, _viewModel.AvailablePeers);
    }

    /// <summary>
    /// 41. OnUserLeft removes peer and updates SelectedPeer
    /// </summary>
    [TestMethod]
    public void OnUserLeft_RemovesPeerAndReselectsNext()
    {
        ControllerUser userA = MakeUser("Eve", 8005);
        ControllerUser userB = MakeUser("Frank", 8006);

        _viewModel.SetAvailableUsers(new[] { userA, userB });
        Assert.AreEqual("Eve", _viewModel.SelectedPeer?.Username);

        _viewModel.OnUserLeft(userA);

        Assert.HasCount(1, _viewModel.AvailablePeers);
        Assert.AreEqual("Frank", _viewModel.AvailablePeers[0].Username);
        Assert.AreEqual("Frank", _viewModel.SelectedPeer?.Username);
    }

    /// <summary>
    /// 42. Changing SelectedPeer fires PropertyChanged event
    /// </summary>
    [TestMethod]
    public void SelectedPeer_ChangingFiresPropertyChanged()
    {
        string? changedProperty = null;
        _viewModel.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        ControllerUser user = MakeUser("Grace", 8007);
        _viewModel.SelectedPeer = user;

        Assert.AreEqual(nameof(FileSyncViewModel.SelectedPeer), changedProperty);
        Assert.AreSame(user, _viewModel.SelectedPeer);
    }

    /// <summary>
    /// 43. Assigning the same SelectedPeer does not raise PropertyChanged
    /// </summary>
    [TestMethod]
    public void SelectedPeer_SameValue_DoesNotRaisePropertyChanged()
    {
        ControllerUser user = MakeUser("Heidi", 8008);
        _viewModel.SelectedPeer = user;

        bool eventRaised = false;
        _viewModel.PropertyChanged += (_, _) => eventRaised = true;

        _viewModel.SelectedPeer = user;

        Assert.IsFalse(eventRaised);
    }

    /// <summary>
    /// 44. Setting SelectedPeer to null clears the selection and raises PropertyChanged
    /// </summary>
    [TestMethod]
    public void SelectedPeer_SetToNull_ClearsSelectionAndRaisesPropertyChanged()
    {
        _viewModel.SelectedPeer = MakeUser("Ivan", 8009);

        string? changedProperty = null;
        _viewModel.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        _viewModel.SelectedPeer = null;

        Assert.IsNull(_viewModel.SelectedPeer);
        Assert.AreEqual(nameof(FileSyncViewModel.SelectedPeer), changedProperty);
    }

    /// <summary>
    /// 45. SetAvailableUsers with an empty list clears peers and selection
    /// </summary>
    [TestMethod]
    public void SetAvailableUsers_EmptyList_ClearsPeersAndSelection()
    {
        _viewModel.SetAvailableUsers(new[] { MakeUser("Judy", 8010) });
        Assert.IsNotNull(_viewModel.SelectedPeer);

        _viewModel.SetAvailableUsers(new List<ControllerUser>());

        Assert.IsEmpty(_viewModel.AvailablePeers);
        Assert.IsNull(_viewModel.SelectedPeer);
    }

    /// <summary>
    /// 46. SetAvailableUsers replaces the previous peers instead of appending
    /// </summary>
    [TestMethod]
    public void SetAvailableUsers_CalledTwice_ReplacesPreviousPeers()
    {
        _viewModel.SetAvailableUsers(new[] { MakeUser("Karl", 8011), MakeUser("Liam", 8012) });

        ControllerUser mia = MakeUser("Mia", 8013);
        _viewModel.SetAvailableUsers(new[] { mia });

        Assert.HasCount(1, _viewModel.AvailablePeers);
        Assert.AreSame(mia, _viewModel.AvailablePeers[0]);
        Assert.AreSame(mia, _viewModel.SelectedPeer);
    }

    /// <summary>
    /// 47. OnUserJoined with a second distinct user adds it and keeps a peer selected
    /// </summary>
    [TestMethod]
    public void OnUserJoined_SecondDistinctUser_AddsPeerAndKeepsSelection()
    {
        ControllerUser first = MakeUser("Nina", 8014);
        ControllerUser second = MakeUser("Oscar", 8015);

        _viewModel.OnUserJoined(first);
        _viewModel.OnUserJoined(second);

        Assert.HasCount(2, _viewModel.AvailablePeers);
        Assert.IsNotNull(_viewModel.SelectedPeer);
    }

    /// <summary>
    /// 48. OnUserJoined with the same key but a different instance keeps the original peer
    /// </summary>
    [TestMethod]
    public void OnUserJoined_SameKeyDifferentInstance_KeepsOriginalPeer()
    {
        ControllerUser original = MakeUser("Pam", 8016);
        ControllerUser duplicate = MakeUser("Pam", 8016);

        _viewModel.OnUserJoined(original);
        _viewModel.OnUserJoined(duplicate);

        Assert.HasCount(1, _viewModel.AvailablePeers);
        Assert.AreSame(original, _viewModel.AvailablePeers[0]);
    }

    /// <summary>
    /// 49. OnUserLeft matches by key, so a different instance with the same key is removed
    /// </summary>
    [TestMethod]
    public void OnUserLeft_SameKeyDifferentInstance_RemovesPeer()
    {
        _viewModel.OnUserJoined(MakeUser("Quinn", 8017));

        _viewModel.OnUserLeft(MakeUser("Quinn", 8017));

        Assert.IsEmpty(_viewModel.AvailablePeers);
    }

    /// <summary>
    /// 50. OnUserLeft for a user that is not in the list changes nothing
    /// </summary>
    [TestMethod]
    public void OnUserLeft_UnknownUser_DoesNothing()
    {
        ControllerUser known = MakeUser("Rita", 8018);
        _viewModel.OnUserJoined(known);

        _viewModel.OnUserLeft(MakeUser("Stranger", 9999));

        Assert.HasCount(1, _viewModel.AvailablePeers);
        Assert.AreSame(known, _viewModel.SelectedPeer);
    }

    /// <summary>
    /// 51. OnUserLeft for a non-selected user keeps the current selection
    /// </summary>
    [TestMethod]
    public void OnUserLeft_NonSelectedUser_KeepsSelection()
    {
        ControllerUser selected = MakeUser("Sam", 8019);
        ControllerUser other = MakeUser("Tina", 8020);

        _viewModel.SetAvailableUsers(new[] { selected, other });
        Assert.AreSame(selected, _viewModel.SelectedPeer);

        _viewModel.OnUserLeft(other);

        Assert.HasCount(1, _viewModel.AvailablePeers);
        Assert.AreSame(selected, _viewModel.SelectedPeer);
    }

    /// <summary>
    /// 52. OnUserLeft for the last remaining peer clears the selection
    /// </summary>
    [TestMethod]
    public void OnUserLeft_LastPeer_ClearsSelection()
    {
        ControllerUser only = MakeUser("Uma", 8021);
        _viewModel.OnUserJoined(only);

        _viewModel.OnUserLeft(only);

        Assert.IsEmpty(_viewModel.AvailablePeers);
        Assert.IsNull(_viewModel.SelectedPeer);
    }

    /// <summary>
    /// 53. OnUserJoined with null does nothing (remove if your code throws on null instead)
    /// </summary>
    [TestMethod]
    public void OnUserJoined_NullUser_DoesNothing()
    {
        _viewModel.OnUserJoined(null!);

        Assert.IsEmpty(_viewModel.AvailablePeers);
    }

    /// <summary>
    /// 54. OnUserLeft with null does nothing (remove if your code throws on null instead)
    /// </summary>
    [TestMethod]
    public void OnUserLeft_NullUser_DoesNothing()
    {
        ControllerUser known = MakeUser("Vera", 8022);
        _viewModel.OnUserJoined(known);

        _viewModel.OnUserLeft(null!);

        Assert.HasCount(1, _viewModel.AvailablePeers);
    }
}
