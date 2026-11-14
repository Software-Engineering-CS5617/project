
using System;
using Incidents;
using Incidents.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Incidents.Test.ViewModels;

/// <summary>
/// Testing AddEvidenceDialogViewModel functionality.
/// </summary>
[TestClass]
public sealed class AddEvidenceDialogViewModelTest
{
    private AddEvidenceDialogViewModel _viewModel = null!;

    /// <summary>
    /// Initializing AddEvidenceDialogViewModel for testing.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _viewModel = new AddEvidenceDialogViewModel(IntPtr.Zero);
    }

    /// <summary>
    /// Default property values are initialized correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesDefaultValues()
    {
        Assert.AreEqual(string.Empty, _viewModel.FilePath);
        Assert.AreEqual(string.Empty, _viewModel.Description);
        Assert.IsFalse(_viewModel.IsValid);
    }

    /// <summary>
    /// Setting FilePath updates its value.
    /// </summary>
    [TestMethod]
    public void FilePath_SetValue_UpdatesProperty()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";

        Assert.AreEqual("C:/Evidence/image.png", _viewModel.FilePath);
    }

    /// <summary>
    /// Setting Description updates its value.
    /// </summary>
    [TestMethod]
    public void Description_SetValue_UpdatesProperty()
    {
        _viewModel.Description = "Evidence description";

        Assert.AreEqual("Evidence description", _viewModel.Description);
    }

    /// <summary>
    /// IsValid returns true when both fields are populated.
    /// </summary>
    [TestMethod]
    public void IsValid_BothFieldsPopulated_ReturnsTrue()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";
        _viewModel.Description = "Evidence description";

        Assert.IsTrue(_viewModel.IsValid);
    }

    /// <summary>
    /// IsValid returns false when FilePath is empty.
    /// </summary>
    [TestMethod]
    public void IsValid_EmptyFilePath_ReturnsFalse()
    {
        _viewModel.Description = "Evidence description";

        Assert.IsFalse(_viewModel.IsValid);
    }

    /// <summary>
    /// IsValid returns false when Description is empty.
    /// </summary>
    [TestMethod]
    public void IsValid_EmptyDescription_ReturnsFalse()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";

        Assert.IsFalse(_viewModel.IsValid);
    }

    /// <summary>
    /// FilePath setter raises PropertyChanged notifications.
    /// </summary>
    [TestMethod]
    public void FilePath_SetValue_RaisesPropertyChangedEvents()
    {
        int eventCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.FilePath) ||
                e.PropertyName == nameof(_viewModel.IsValid))
            {
                eventCount++;
            }
        };

        _viewModel.FilePath = "C:/Evidence/image.png";

        Assert.AreEqual(2, eventCount);
    }

    /// <summary>
    /// Description setter raises PropertyChanged notifications.
    /// </summary>
    [TestMethod]
    public void Description_SetValue_RaisesPropertyChangedEvents()
    {
        int eventCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.Description) ||
                e.PropertyName == nameof(_viewModel.IsValid))
            {
                eventCount++;
            }
        };

        _viewModel.Description = "Evidence description";

        Assert.AreEqual(2, eventCount);
    }

    /// <summary>
    /// Helper function to create a temporary file
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private string CreateTemporaryFile(byte[] content)
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.png");

        File.WriteAllBytes(filePath, content);

        return filePath;
    }

    /// <summary>
    /// BuildEvidence creates an evidence object with the supplied details.
    /// </summary>
    [TestMethod]
    public async Task BuildEvidence_CreatesEvidenceWithCorrectValues()
    {
        string uploadedBy = Guid.NewGuid().ToString();
        byte[] expectedContent = [1, 2, 3, 4];
        string filePath = CreateTemporaryFile(expectedContent);

        try
        {
            _viewModel.FilePath = filePath;
            _viewModel.Description = "  Accident photograph  ";

            (IncidentEvidence Evidence, byte[] Content) result = await _viewModel.BuildEvidenceAsync(uploadedBy);

            Assert.AreNotEqual(Guid.Empty, result.Evidence.Id);
            Assert.AreEqual("Accident photograph", result.Evidence.Description);
            Assert.AreEqual(filePath, result.Evidence.FilePath);
            Assert.AreEqual(uploadedBy, result.Evidence.UploadedBy);
            Assert.AreNotEqual(default, result.Evidence.UploadTime);
            CollectionAssert.AreEqual(expectedContent, result.Content);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// BuildEvidence generates a unique ID for each evidence object.
    /// </summary>
    [TestMethod]
    public async Task BuildEvidence_GeneratesUniqueIds()
    {
        string filePath = CreateTemporaryFile([1, 2, 3, 4]);

        try
        {
            _viewModel.FilePath = filePath;
            _viewModel.Description = "Evidence";

            (IncidentEvidence Evidence, byte[] Content) first = await _viewModel.BuildEvidenceAsync(Guid.NewGuid().ToString());
            (IncidentEvidence Evidence, byte[] Content) second = await _viewModel.BuildEvidenceAsync(Guid.NewGuid().ToString());

            Assert.AreNotEqual(first.Evidence.Id, second.Evidence.Id);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Setting FilePath to the same value does not raise PropertyChanged events.
    /// </summary>
    [TestMethod]
    public void FilePath_SetSameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.FilePath = "C:/Evidence/image.png",
            nameof(_viewModel.FilePath));
    }

    /// <summary>
    /// Setting Description to the same value does not raise PropertyChanged events.
    /// </summary>
    [TestMethod]
    public void Description_SetSameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.Description = "Evidence description";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.Description = "Evidence description",
            nameof(_viewModel.Description));
    }
}
