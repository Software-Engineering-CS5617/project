// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Incidents;
using Incidents.ViewModels;

namespace Incidents.Test.ViewModels;

/// <summary>
/// Contains common helper methods used across ViewModel tests.
/// </summary>
internal static class VMTestHelpers
{
    /// <summary>
    /// Creates an incident through the real IncidentController.
    /// </summary>
    public static async Task<Incident> CreateIncidentAsync(
        IIncidentController controller,
        string currentUserId)
    {
        CreateIncidentRequest request = new(
            "Vehicle accident",
            "Test incident description",
            currentUserId,
            IncidentCategory.Accident,
            IncidentSeverity.High,
            new IncidentLocation(10.0, 76.0, "Test location"),
            []);

        return await controller.CreateIncident(request);
    }

    /// <summary>
    /// Creates an incident and loads it into the IncidentDetailsPageViewModel.
    /// </summary>
    public static async Task<Incident> LoadCreatedIncidentAsync(
        IIncidentController controller,
        IncidentDetailsPageViewModel viewModel,
        string currentUserId)
    {
        Incident incident = await CreateIncidentAsync(controller, currentUserId);
        await viewModel.LoadIncidentAsync(incident.Id);

        Assert.IsNotNull(viewModel.Incident);

        return viewModel.Incident;
    }

    /// <summary>
    /// Asserts that changing a property raises PropertyChanged.
    /// </summary>
    public static void AssertPropertyChanged(
        INotifyPropertyChanged viewModel,
        Action propertySetter,
        string propertyName)
    {
        bool notified = false;

        viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == propertyName)
            {
                notified = true;
            }
        };

        propertySetter();

        Assert.IsTrue(notified, $"{propertyName} did not raise PropertyChanged.");
    }

    /// <summary>
    /// Asserts that assigning the same value does not raise PropertyChanged.
    /// </summary>
    public static void AssertNoPropertyChanged(
        INotifyPropertyChanged viewModel,
        Action propertySetter,
        string propertyName)
    {
        int notificationCount = 0;

        viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == propertyName)
            {
                notificationCount++;
            }
        };

        propertySetter();

        Assert.AreEqual(0, notificationCount,
            $"{propertyName} unexpectedly raised PropertyChanged.");
    }

    /// <summary>
    /// Assigns valid values to all required incident fields.
    /// </summary>
    public static void SetValidIncidentDetails(CreateIncidentPageViewModel viewModel)
    {
        viewModel.Title = "Vehicle accident";
        viewModel.Description = "Test incident description";

        viewModel.Latitude = 10.0;
        viewModel.Longitude = 76.0;
        viewModel.Address = "Test location";

        viewModel.SelectedCategory = IncidentCategory.Accident;
        viewModel.SelectedSeverity = IncidentSeverity.High;
    }

    /// <summary>
    /// Executes SubmitCommand and waits for the IncidentCreated event.
    /// </summary>
    public static async Task SubmitAndWaitForIncidentCreatedAsync(
        CreateIncidentPageViewModel viewModel)
    {
        TaskCompletionSource<bool> completionSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        viewModel.IncidentCreated += (_, _) =>
            completionSource.TrySetResult(true);

        viewModel.SubmitCommand.Execute(null);

        await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Waits until the asynchronous submission has completed.
    /// </summary>
    public static async Task WaitUntilSubmissionCompletesAsync(
        CreateIncidentPageViewModel viewModel)
    {
        DateTime timeout = DateTime.UtcNow.AddSeconds(10);

        while (viewModel.IsSubmitting)
        {
            if (DateTime.UtcNow >= timeout)
            {
                Assert.Fail("Submission did not complete within the expected time.");
            }
            await Task.Delay(10);
        }
    }
}
