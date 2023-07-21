using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using HaikuApiGenerator.Passes;
using System.Text.RegularExpressions;

namespace HaikuApiGenerator;

class HaikuApiLibrary : ILibrary
{
    private readonly string _headersDir;
    private readonly string _libsDir;
    private readonly string _outputDir;

    public HaikuApiLibrary(string headersDir, string libsDir, string outputDir)
    {
        _headersDir = headersDir;
        _libsDir = libsDir;
        _outputDir = outputDir;
    }

    public void Setup(Driver driver)
    {
        var options = driver.Options;
        options.GeneratorKind = GeneratorKind.CSharp;
        options.OutputDir = _outputDir;
        options.GenerateFinalizers = true;
        options.GenerateDefaultValuesForArguments = true;
        options.MarshalCharAsManagedChar = true;
        options.MarshalConstCharArrayAsString = false;

        var supportKit = CreateHaikuApiModule(driver, "Support");
        var interfaceKit = CreateHaikuApiModule(driver, "Interface");
        var kernelKit = CreateHaikuApiModule(driver, "Kernel");
        var storageKit = CreateHaikuApiModule(driver, "Storage");
        var appKit = CreateHaikuApiModule(driver, "App");
    }

    public void SetupPasses(Driver driver)
    {
    }

    public void Preprocess(Driver driver, ASTContext ctx)
    {
        ctx.IgnoreConversionToProperty(".*");

        // The original enum name was "tab_side". When converted to PascalCase, the name
        // clashes with that of one member function of BTabView.
        ctx.SetNameOfEnumWithMatchingItem("kLeftSide", "TabSides");

        new ProcessConstantsAndEnumerationsPass(ctx).VisitASTContext(ctx);
        new StripUnwantedSymbolsPass(ctx, _headersDir).VisitASTContext(ctx);
        new ProcessIncompleteTypesPass(ctx).VisitASTContext(ctx);

        // The library dependency system of CppSharp is entirely broken and
        // makes the following passes generate incorrect code.
        foreach (var module in driver.Options.Modules)
        {
            module.Dependencies.Clear();
        }
    }

    public void Postprocess(Driver driver, ASTContext ctx)
    {
        new StripUnwantedSymbolsPass(ctx, _headersDir).VisitASTContext(ctx);
        new RestoreNamePass(ctx).VisitASTContext(ctx);
        new HandleEnumItemNamesPass(ctx).VisitASTContext(ctx);
        new ProcessDefaultParametersPass(ctx).VisitASTContext(ctx);
        new EliminateFloatOverloadsPass(ctx).VisitASTContext(ctx);
    }

    private Module CreateHaikuApiModule(Driver driver, string name)
    {
        var module = driver.Options.AddModule(name);

        module.IncludeDirs.Add(Path.Combine(_headersDir, "os"));
        module.IncludeDirs.Add(Path.Combine(_headersDir, "os", name.ToLowerInvariant()));
        module.IncludeDirs.Add(Path.Combine(_headersDir, "posix"));
        module.IncludeDirs.Add(Path.Combine(_headersDir));

        module.LibraryDirs.Add(_libsDir);
        module.Libraries.Add("libbe.so");
        module.Libraries.Add("libtracker.so");

        module.Headers.AddRange(
            Regex
                .Matches(
                    File.ReadAllText(Path.Combine(_headersDir, "os", $"{name}Kit.h")),
                    @"\s*#include\s+<([\w\d.]+)>")
                .Select(m => m.Groups[1].Value));

        module.OutputNamespace = $"Haiku.{name}";

        return module;
    }
}
