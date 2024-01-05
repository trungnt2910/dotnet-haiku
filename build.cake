#load cake\TargetEnvironment.cake
#load cake\Bash.cake
#addin "Cake.Incubator&version=8.0.0"

using D = System.IO.Directory;
using F = System.IO.File;
using P = System.IO.Path;

// VARS
var version = Argument("build-version", "0.1.0");
var packageVersion = Argument("package-version", "0.1.0");
var configuration = Argument("configuration", "Release");
var target = Argument("target", "Default");
var runtimeVersion = Argument("runtime-version", "8.0");

var msbuildsettings = new DotNetMSBuildSettings();
var supportedVersionBands = new List<string>() { "8.0.100-preview.4", "8.0.100-preview.5", "8.0.100-preview.6", "8.0.100-preview.7", "8.0.100-rc.1", "8.0.100-rc.2", "8.0.100" };

const string manifestName = "Trungnt2910.NET.Sdk.Haiku";
var manifestPack = $"{manifestName}.Manifest-{TargetEnvironment.DotNetCliFeatureBand}.{packageVersion}.nupkg";
var manifestPackPath = $"out/nuget/{manifestPack}";

var packNames = new List<string>()
{
    "Haiku.Ref",
    "Haiku.Runtime",
    "Haiku.Sdk"
};

var coreLibraryNames = new List<string>()
{
    "Haiku"
};

// TASKS

Task("Init")
    .Does(() =>
{
    Console.WriteLine("Version: " + version);
    Console.WriteLine("Package Version: " + packageVersion);

    // Assign some common properties
    msbuildsettings = msbuildsettings.WithProperty("Version", version);
    msbuildsettings = msbuildsettings.WithProperty("PackageVersion", packageVersion);
    msbuildsettings = msbuildsettings.WithProperty("Authors", "Trung Nguyen");
    msbuildsettings = msbuildsettings.WithProperty("_HaikuVersion", version);
    msbuildsettings = msbuildsettings.WithProperty("_HaikuNetVersion", runtimeVersion);
});

Task("BuildCppSharp")
    .Does(() =>
{
    var workingDirectory = "external/CppSharp/build";

    try
    {
        Bash.RunCommand($"./build.sh generate -configuration {configuration} -platform x64", workingDirectory);
    }
    catch
    {
        Console.WriteLine("Prebuilt LLVM not available for this platform. Building from source...");
        Bash.RunCommand("./build.sh clone_llvm", workingDirectory);
        Bash.RunCommand("./build.sh build_llvm", workingDirectory);
        Bash.RunCommand("./build.sh package_llvm", workingDirectory);
        Bash.RunCommand($"./build.sh generate -configuration {configuration} -platform x64", workingDirectory);
    }

    Bash.RunCommand($"./build.sh -configuration {configuration} -platform x64", workingDirectory);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var settings = new DotNetRestoreSettings
    {
        MSBuildSettings = msbuildsettings,
    };

    foreach (var name in coreLibraryNames)
    {
        DotNetRestore($"src/{name}/{name}.csproj", settings);
    }

    foreach (var name in packNames)
    {
        DotNetRestore($"workload/{name}/{name}.csproj", settings);
    }

    DotNetRestore("workload/Haiku.Templates/Haiku.Templates.csproj", settings);
    DotNetRestore("workload/Trungnt2910.NET.Sdk.Haiku/Trungnt2910.NET.Sdk.Haiku.csproj", settings);
    DotNetRestore("generator/HaikuApiGenerator/HaikuApiGenerator.csproj", settings);
    DotNetRestore("generator/HaikuApiGenerator.PostProcessing/HaikuApiGenerator.PostProcessing.csproj", settings);
});

Task("Clean")
    .IsDependentOn("Init")
    .Does(() =>
{

});

Task("FullClean")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DeleteDirectory("out", new DeleteDirectorySettings {
        Recursive = true,
        Force = true
    });
});

Task("GenerateBindings")
    .IsDependentOn("BuildCppSharp")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetBuildSettings
    {
        Configuration = configuration,
        MSBuildSettings = msbuildsettings,
        NoRestore = true
    };

    DotNetBuild($"generator/HaikuApiGenerator/HaikuApiGenerator.csproj", settings);

    var runSettings = new DotNetRunSettings
    {
        Configuration = configuration,
        NoBuild = true,
    };

    runSettings.EnvironmentVariables.Add("HAIKU_API_GENERATOR_OUTPUT_DIR", "out/generated");
    runSettings.EnvironmentVariables.Add("LD_LIBRARY_PATH",
        $"{Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")}:{P.Combine(D.GetCurrentDirectory(), $"external/CppSharp/bin/{configuration}_x64")}");

    DotNetRun($"generator/HaikuApiGenerator/HaikuApiGenerator.csproj", runSettings);
});

Task("BuildAndPackCoreLibraries")
    .IsDependentOn("GenerateBindings")
    .Does(() =>
{
    var buildSettings = new DotNetBuildSettings
    {
        Configuration = configuration,
        MSBuildSettings = msbuildsettings,
        NoRestore = true
    };

    var packSettings = new DotNetPackSettings
    {
        Configuration = configuration,
        MSBuildSettings = msbuildsettings,
        OutputDirectory = "out/nuget",
        NoRestore = true,
        NoBuild = true,
        NoDependencies = true
    };

    foreach (var name in coreLibraryNames)
    {
        DotNetBuild($"src/{name}/{name}.csproj", buildSettings);
        DotNetPack($"src/{name}/{name}.csproj", packSettings);
    }
});

Task("BuildAndPackageWorkload")
    .IsDependentOn("BuildAndPackCoreLibraries")
    .Does(() =>
{
    var buildSettings = new DotNetBuildSettings
    {
        MSBuildSettings = msbuildsettings,
        Configuration = configuration,
        NoRestore = true,
        // Don't build the core libraries
        NoDependencies = true,
        // Always re-generate files.
        NoIncremental = true
    };

    var packSettings = new DotNetPackSettings
    {
        MSBuildSettings = msbuildsettings,
        Configuration = configuration,
        OutputDirectory = "out/nuget",
        NoRestore = true,
        NoBuild = true,
        NoDependencies = true
    };

    foreach (var name in packNames)
    {
        DotNetBuild($"workload/{name}/{name}.csproj", buildSettings);
        DotNetPack($"workload/{name}/{name}.csproj", packSettings);
    }

    DotNetBuild($"workload/Haiku.Templates/Haiku.Templates.csproj", buildSettings);
    DotNetPack($"workload/Haiku.Templates/Haiku.Templates.csproj", packSettings);

    foreach (var band in supportedVersionBands)
    {
        if (!band.StartsWith($"{runtimeVersion}."))
        {
            continue;
        }

        buildSettings.MSBuildSettings.Properties.Remove("_HaikuManifestVersionBand");
        buildSettings.MSBuildSettings.Properties.Add("_HaikuManifestVersionBand", new [] { band });
        DotNetBuild("workload/Trungnt2910.NET.Sdk.Haiku/Trungnt2910.NET.Sdk.Haiku.csproj", buildSettings);

        packSettings.MSBuildSettings.Properties.Remove("_HaikuManifestVersionBand");
        packSettings.MSBuildSettings.Properties.Add("_HaikuManifestVersionBand", new [] { band });
        DotNetPack("workload/Trungnt2910.NET.Sdk.Haiku/Trungnt2910.NET.Sdk.Haiku.csproj", packSettings);
    }
});

Task("InstallWorkload")
    .IsDependentOn("BuildAndPackageWorkload")
    .Does(() =>
{
    Console.WriteLine($"Installing workload for SDK version {TargetEnvironment.DotNetCliFeatureBand}, at {TargetEnvironment.DotNetInstallPath}");
    Console.WriteLine($"Installing manifests to {TargetEnvironment.DotNetManifestPath}");
    TargetEnvironment.InstallManifests(manifestName, manifestPackPath);
    Console.WriteLine($"Installing packs to {TargetEnvironment.DotNetPacksPath}");
    foreach (var name in packNames)
    {
        var realName = name;
        // TODO: Remove Haiku.Runtime from the general package names list
        // and properly handle different RIDs when we add support for other architectures!!!!
        if (name == "Haiku.Runtime")
        {
            realName = "Haiku.Runtime.haiku-x64";
        }
        Console.WriteLine($"Installing {realName}");
        var pack = $"{realName}.{packageVersion}.nupkg";
        var packPath = $"out/nuget/{pack}";
        TargetEnvironment.InstallPack(realName, packageVersion, packPath);
    }
    Console.WriteLine("Installing templates");
    TargetEnvironment.InstallTemplatePack($"Haiku.Templates.{packageVersion}.nupkg", $"out/nuget/Haiku.Templates.{packageVersion}.nupkg");
    Console.WriteLine($"Registering \"haiku\" installed workload...");
    TargetEnvironment.RegisterInstalledWorkload("haiku");
});

Task("UninstallWorkload")
    .Does(() =>
{
    Console.WriteLine($"Uninstalling workload for SDK version {TargetEnvironment.DotNetCliFeatureBand}, at {TargetEnvironment.DotNetInstallPath}");
    Console.WriteLine($"Removing manifests from {TargetEnvironment.DotNetManifestPath}");
    TargetEnvironment.UninstallManifests(manifestName);
    Console.WriteLine($"Removing packs from {TargetEnvironment.DotNetPacksPath}");
    foreach (var name in packNames)
    {
        var realName = name;
        // TODO: Same as above.
        if (name == "Haiku.Runtime")
        {
            realName = "Haiku.Runtime.haiku-x64";
        }
        Console.WriteLine($"Removing {realName}");
        TargetEnvironment.UninstallPack(realName, version);
    }
    Console.WriteLine("Removing templates");
    TargetEnvironment.UninstallTemplatePack($"Haiku.Templates");
    Console.WriteLine($"Unregistering \"haiku\" installed workload...");
    TargetEnvironment.UnregisterInstalledWorkload("haiku");
});

// TASK TARGETS

Task("Default")
    .IsDependentOn("BuildAndPackageWorkload");

// EXECUTION

RunTarget(target);
