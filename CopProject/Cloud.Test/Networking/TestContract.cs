// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;

namespace Cloud.Test;

/// <summary>
/// Tests the Contract class
/// </summary>
[TestClass]
public class TestContract
{
    /// <summary>Tests that method names match their expected hardcoded values.</summary>
    [TestMethod]
    public void CheckIfContractMethodNamesAreCorrect()
    {
        // Ensures the contract method names remain unchanged.
        Assert.AreEqual("SendMessage", Contract.SendMessage);
        Assert.AreEqual("ReceiveMessage", Contract.ReceiveMessage);
    }
}
