using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace HaikuApiGenerator;

class CppCompiler
{
    public static string CppCompilerPath { get; private set; }

    static CppCompiler()
    {
        CppCompilerPath = null!;

        var dotnetRootFs = Environment.GetEnvironmentVariable("ROOTFS_DIR");
        if (dotnetRootFs != null)
        {
            CppCompilerPath = Path.Combine(dotnetRootFs, "cross-tools-x86_64/bin/x86_64-unknown-haiku-g++");
        }

        if (CppCompilerPath == null || !File.Exists(CppCompilerPath))
        {
            CppCompilerPath = Which("x86_64-unknown-haiku-g++");
        }

        if (CppCompilerPath == null || !File.Exists(CppCompilerPath))
        {
            CppCompilerPath = Which("g++");
        }

        var psi = new ProcessStartInfo(CppCompilerPath, "-dumpmachine");
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        var p = Process.Start(psi)!;
        p.WaitForExit();
        var output = p.StandardOutput.ReadToEnd();
        if (output.Trim() != "x86_64-unknown-haiku")
        {
            throw new Exception("Could not find a valid C++ compiler for haiku-x64");
        }
    }

    public static void BuildSharedLibrary(string output, IEnumerable<string> sources, IEnumerable<string> libraries, string rootDir)
    {
        var psi = new ProcessStartInfo(CppCompilerPath,
            $"-shared -o {output} " +
            $"{string.Join(" ", sources)} " +
            $"{string.Join(" ", libraries.Select(l => $"-l{l}"))} " +
            $"--sysroot={rootDir} " +
            "-Wl,--no-undefined ");
        psi.RedirectStandardOutput = false;
        psi.RedirectStandardError = false;
        psi.UseShellExecute = false;
        var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            throw new Exception($"Failed to build shared library {output}");
        }
    }

    public static void BuildObject(string output, IEnumerable<string> sources, IEnumerable<string> libraries, string rootDir)
    {
        var psi = new ProcessStartInfo(CppCompilerPath,
            $"-c -o {output} " +
            $"{string.Join(" ", sources)} " +
            $"{string.Join(" ", libraries.Select(l => $"-l{l}"))} " +
            $"--sysroot={rootDir} " +
            "-Wl,--no-undefined ");
        psi.RedirectStandardOutput = false;
        psi.RedirectStandardError = false;
        psi.UseShellExecute = false;
        var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            throw new Exception($"Failed to build object {output}");
        }
    }

    public static void BuildStaticLibrary(string output, IEnumerable<string> sources, IEnumerable<string> libraries, string rootDir)
    {
        try
        {
            foreach (var source in sources)
            {
                var objectFile = Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(source) + ".o");
                BuildObject(objectFile, new[] { source }, libraries, rootDir);
            }

            var psi = new ProcessStartInfo("ar", $"rcs {output} {string.Join(" ", sources.Select(s => Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(s) + ".o")))}");
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;
            psi.UseShellExecute = false;
            var p = Process.Start(psi)!;
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                throw new Exception($"ar command failed");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to build static library {output}", e);
        }
        finally
        {
            foreach (var source in sources)
            {
                var objectFile = Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(source) + ".o");
                if (File.Exists(objectFile))
                {
                    File.Delete(objectFile);
                }
            }
        }
    }

    private static string Which(string command)
    {
        var psi = new ProcessStartInfo("which", command);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        var p = Process.Start(psi)!;
        p.WaitForExit();
        var output = p.StandardOutput.ReadToEnd();
        return output.Trim();
    }
}