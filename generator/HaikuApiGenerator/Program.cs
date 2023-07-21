using CppSharp;
using HaikuApiGenerator;
using System.Globalization;
using System.Text.RegularExpressions;

var rootDir = Environment.GetEnvironmentVariable("HAIKU_API_GENERATOR_ROOT_DIR");
if (rootDir == null)
{
    // .NET cross compiling machines
    rootDir = Environment.GetEnvironmentVariable("ROOTFS_DIR");
}
if (rootDir == null)
{
    // Machines with HyClone
    rootDir = Environment.GetEnvironmentVariable("HPREFIX");
}
if (rootDir == null)
{
    rootDir = "/";
}

var originalHeadersDir = Environment.GetEnvironmentVariable("HAIKU_API_GENERATOR_HEADERS_DIR");
if (originalHeadersDir == null)
{
    originalHeadersDir = Path.Combine(rootDir, "boot", "system", "develop", "headers");
}

var libsDir = Environment.GetEnvironmentVariable("HAIKU_API_GENERATOR_LIBS_DIR");
if (libsDir == null)
{
    libsDir = Path.Combine(rootDir, "boot", "system", "develop", "lib");
}

var outputDir = Environment.GetEnvironmentVariable("HAIKU_API_GENERATOR_OUTPUT_DIR");
if (outputDir == null)
{
    outputDir = "generated";
}

if (Directory.Exists(outputDir))
{
    Directory.Delete(outputDir, true);
}
Directory.CreateDirectory(outputDir);

var modifiedHeadersDir = Path.Combine(outputDir, "headers");
Directory.CreateDirectory(modifiedHeadersDir);

foreach (var file in Directory.EnumerateFiles(originalHeadersDir, "*.*", new EnumerationOptions() { RecurseSubdirectories = true }))
{
    var relativePath = Path.GetRelativePath(originalHeadersDir, file);
    var modifiedPath = Path.Combine(modifiedHeadersDir, relativePath);

    Directory.CreateDirectory(Path.GetDirectoryName(modifiedPath)!);

    var contents = File.ReadAllText(file);

    // Currently, there are no modifications yet.

    File.WriteAllText(modifiedPath, contents);
}

ConsoleDriver.Run(new HaikuApiLibrary(modifiedHeadersDir, libsDir, outputDir));

foreach (var file in Directory.EnumerateFiles(outputDir, "*.cs"))
{
    var contents = File.ReadAllText(file);

    // Silence some warnings
    contents = "#pragma warning disable CS0108" + Environment.NewLine + contents;
    contents = "#pragma warning disable CS0660" + Environment.NewLine + contents;
    contents = "#pragma warning disable CS0661" + Environment.NewLine + contents;
    contents = "#pragma warning disable CS1591" + Environment.NewLine + contents;
    contents = "#pragma warning disable CS8981" + Environment.NewLine + contents;

    // Prevents the __Internal struct from being exposed to the public.
    contents = contents.Replace("public partial struct __Internal", "internal partial struct __Internal");

    // vtable-related hacks. TODO: Upstream to CppSharp?
    contents = contents.Replace("SetupVTables(GetType().FullName", "SetupVTables(((object)this).GetType().FullName");
    contents = Regex.Replace(contents, @"(bool skipVTables = false\)\s+?: base\(\(void\*\)\s+?native)\)", "$1, skipVTables)");

    // Unnecessary attributes, since we are building every file into a single library. The attributes also break the build for strongly-named assemblies.
    contents = Regex.Replace(contents, @"\[assembly:InternalsVisibleTo\(\""[A-Z][a-z]*?\""\)\]", "");

    // We are also building all native glue into a single "HaikuGlue" library.
    contents = Regex.Replace(contents, @"DllImport\(\""[A-Z][a-z]*?\""", "DllImport(\"HaikuGlue\"");

    // Injects code that disposes the managed instance of BLooper when it has quitted.
    contents = Regex.Replace(contents, @"(_QuitDelegateHook\([^\)]*?\)[^}]*?\{[^}]*?(^\s*)[^}]*?BLooper[^}]*)(^\s+\})",
        "$1$2__target.__ownsNativeInstance = false;\n$2__target.Dispose(false, callNativeDtor: false );\n$3", RegexOptions.Multiline);

    File.WriteAllText(file, contents);
}

foreach (var file in Directory.EnumerateFiles(outputDir, "*.cpp"))
{
    var contents = File.ReadAllText(file);

    // Allows the glue code to access members that are officially protected.
    // Currently this does not break anything.
    contents = "#define protected public" + Environment.NewLine + contents;

    // Otherwise this would clash with the POSIX function `stat`.
    contents = contents.Replace("stat*", "struct stat*");
    contents = contents.Replace("stat&", "struct stat&");

    // Put all contents into an unique namespace. Otherwise, the names of different glue files would clash when linked into a single library.
    var lastInclude = contents.LastIndexOf("#");
    var firstBlankLineAfterLastInclude = contents.IndexOf(Environment.NewLine, lastInclude);
    contents = contents.Insert(firstBlankLineAfterLastInclude + Environment.NewLine.Length + 1, $"namespace {Path.GetFileNameWithoutExtension(file).Replace(".", "_").Replace("-", "_")}" + Environment.NewLine + "{" + Environment.NewLine + Environment.NewLine);
    contents = contents + Environment.NewLine + "}" + Environment.NewLine;

    File.WriteAllText(file, contents);
}

File.WriteAllText(Path.Combine(outputDir, "Std.cs"),
@"namespace Std;
using System;
using System.Runtime.InteropServices;

internal class Vector
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct __Internal
    {
        IntPtr fBegin;
        IntPtr fEnd;
        IntPtr fCapacityEnd;

    }
}
");

CppCompiler.BuildSharedLibrary(
    Path.Combine(outputDir, "libHaikuGlue.so"),
    Directory.EnumerateFiles(outputDir, "*.cpp"),
    new [] { "be", "tracker" },
    rootDir
);

CppCompiler.BuildStaticLibrary(
    Path.Combine(outputDir, "libHaikuGlue.a"),
    Directory.EnumerateFiles(outputDir, "*.cpp"),
    new [] { "be", "tracker" },
    rootDir
);
