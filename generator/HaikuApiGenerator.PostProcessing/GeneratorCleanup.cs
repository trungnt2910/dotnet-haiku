using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Collections.Generic;
using BuildTask = Microsoft.Build.Utilities.Task;

namespace HaikuApiGenerator.PostProcessing;

public class GeneratorCleanup : BuildTask
{
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    public string SnkPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            using var asmDef = AssemblyDefinition.ReadAssembly(AssemblyPath, new ReaderParameters()
            {
                ReadSymbols = true,
                ThrowIfSymbolsAreNotMatching = true,
                InMemory = true,
            });

            foreach (var type in asmDef.MainModule.Types.Where(t => t.Namespace.StartsWith("CppSharp")))
            {
                // Hide away all CppSharp types.
                type.IsPublic = false;

                // Redirect libdl P/Invoke to libroot.
                foreach (var method in type.Methods.Where(m => m.PInvokeInfo?.Module.Name == "dl"))
                {
                    method.PInvokeInfo.Module.Name = "root";
                }
            }

            asmDef.Write(AssemblyPath, new WriterParameters()
            {
                WriteSymbols = true,
                StrongNameKeyBlob = File.Exists(SnkPath) ? File.ReadAllBytes(SnkPath) : null
            });
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e);
            return false;
        }

        return true;
    }
}