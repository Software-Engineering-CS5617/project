// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Incident_module;
using Incident_module.Implementations;
using Filesync;
using Persistence;


namespace Incidents.Test;

[TestClass]
public sealed class IncidentTest
{
    [TestMethod]
    public async Task CreateIncidentController()
    {
        IStorage cloudStorageService = new IncidentCloudStorageService();
        IFileOperations fileOperationsService = new IncidentFileSynchroniser();

        try
        {
            IncidentController controller = await IncidentController.CreateAsync(cloudStorageService, fileOperationsService);

            Console.WriteLine("Successfully created Incident Controller");
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to create Incident Controller: ", ex);
        }
    }
}
