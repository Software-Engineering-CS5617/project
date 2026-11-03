## To Do: Migrating Files to WinUI

### Create a WinUI Class Library

First, copy your existing class library files into the `Models` folder of the WinUI Class Library.

### Make the WinUI Console App Buildable and Testable in CI/CD

Open the `.csproj` files and add the following tags inside `<PropertyGroup>`. Make sure that only one instance of each tag exists.

```xml
<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
<RuntimeIdentifiers>win-x64;win-x86;win-arm64</RuntimeIdentifiers>
<SelfContained>false</SelfContained>
<PublishReadyToRun>false</PublishReadyToRun>
```

### Link Your WinUI Class Library Models to the Existing MSTest Project

First, go to the `.Test` (MSTest) project and add a project reference to the WinUI Class Library where you copied your code into the `Models` folder.

If you have any `internal` classes that you do not want to expose to other projects but still need to access in your tests, add the following to the `.csproj` file of the WinUI Class Library (optional, only if applicable):

```xml
<ItemGroup>
    <InternalsVisibleTo Include="<YourProject>.Test" />
</ItemGroup>
```

Inside the MSTest project's `.csproj` file, add the following tags to ensure that the project can be built and the tests can run:

```xml
<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
<UseWinUI>false</UseWinUI>
```

Finally, update your `using` statements from:

```csharp
using <Old_Class_Library>;
```

to:

```csharp
using <WinUI_Class_Library>.Models;
```

Finally, build the solution and run **ALL tests**.