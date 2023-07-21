# .NET 8.0 workload for Haiku

An attempt to create a .NET SDK workload that provides the `net8.0-haiku` TFM.

## Building instructions

### Clone this repository

Make sure to do a recursive clone as this repository uses `git` submodules.

```sh
git clone --recurse-submodules https://github.com/trungnt2910/dotnet-haiku
```

### Prerequisites

- A platform that uses a Haiku-compatible Itanium ABI (tested on Linux only).
- Git, CMake, and Ninja (used to build [CppSharp](https://github.com/mono/CppSharp/blob/main/docs/LLVM.md#compiling-using-the-build-script)). On Ubuntu:
```sh
sudo apt install -y git cmake ninja-build
```
- A `dotnet` installation.
- On non-Haiku platforms, a Haiku cross-compilation rootfs:
```sh
git clone --depth=1 https://github.com/dotnet/arcade
export ROOTFS_DIR=/path/to/rootfs/dir
arcade/eng/common/cross/build-rootfs.sh x64 haiku
```

### Build and install workload

From the repository root directory:

```sh
dotnet tool restore
dotnet cake build.cake --target=InstallWorkload --configuration=Release
```

This should install the `haiku` workload to the default .NET SDK installation.

The first build will take a long time as it builds a portion of the LLVM project.

## Usage

See the sample projects in the [`sample`](sample) folder for more examples.

### `net8.0-haiku` TFM

To use the Haiku API bindings, the project must target `net8.0-haiku` (replace `8.0` with a later version when applicable).
This is similar to the usage of `net8.0-windows` or any other OS-specific TFMs.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-haiku</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

### Kits and symbols

Each kit is located in a separate namespace prefixed with `Haiku.`.

Non-member constants/variables starting with `B_` or `be_` are located in a class called `Symbols` in their kits' namespaces. The name `Symbols` is not final and is subject to change.

Members of named `enum`s have the `B_` prefix removed. Their names are also converted to `PascalCase` to match C#'s convention.

For example, to create a custom [`BWindow`](https://www.haiku-os.org/docs/api/classBWindow.html):

```csharp
using Haiku.App;
using Haiku.Interface;
using static Haiku.App.Symbols;
using static Haiku.Interface.Symbols;

namespace EmptyWindow;

public class MainWindow: BWindow
{
    public MainWindow()
        : base(new BRect(), "Main Window", WindowType.TitledWindow, B_QUIT_ON_WINDOW_CLOSE)
    {
        MoveTo(100, 100);
        ResizeTo(200, 200);
    }

    public override bool QuitRequested()
    {
        be_app.PostMessage(B_QUIT_REQUESTED);
        return true;
    }
}
```

## Limitations

### Compilation

Only building on Linux is supported. At the time of writing, the .NET SDK on Haiku has not been stable yet.

### Packaging

Only `x86_64` is supported. Throughout the various build scripts `x64` and `x86_64` are hard coded. Some other parts of the scripts might also assume `x86_64`.

Reference assemblies are currently the same as the `x86_64` version.

It is still too early to add support for other platforms, since neither .NET on Haiku nor CppSharp is compatibile with anything other than `x86_64`.

### Generation

- Only a few basic kits are generated. Currently, Application, Interface, Kernel, Storage, and Support kits are included.
- Macros are not generated.
- Some classes, especially C++ template classes, might be missing.
- Documentation is not included.
- The generated API is considered experimental and may change any time as the generators improve.

### Usage

The usage of this workload is only supported on [custom](https://github.com/trungnt2910/dotnet-builds) .NET builds for Haiku.

Attempting to use this workload on mainstream .NET builds will result in an error:

```
/home/trung/sdk/.dotnet/sdk/8.0.100-preview.4.23260.5/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.FrameworkReferenceResolution.targets(90,5): error NETSDK1083: The specified RuntimeIdentifier 'haiku-x64' is not recognized. [/home/trung/DotnetHaiku/sample/EmptyWindow/EmptyWindow.csproj]
```

The `haiku-x64` RID does not exist yet, at least before [this](https://github.com/dotnet/runtime/pull/86391) pull request gets merged.
