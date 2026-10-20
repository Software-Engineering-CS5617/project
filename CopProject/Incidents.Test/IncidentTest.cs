// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Incidents;
using Incidents.Implementations;
using Filesync;
using Persistence;


namespace Incidents.Test;

[TestClass]
public sealed class IncidentTest
{
    [TestMethod]
    public async Task CreateIncidentArgumentNullException()
    {
        IStorage cloudStorageService = new IncidentCloudStorageService();
        IFileOperations fileOperationsService = new IncidentFileSynchroniser();
        IncidentController controller = new IncidentController(cloudStorageService, fileOperationsService);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateIncident(null!));
    }
}
