using CppSharp.AST;

namespace HaikuApiGenerator.Passes;

public class StripUnwantedSymbolsPass : TranslationUnitPass
{
    private readonly string _headersDir;

    public StripUnwantedSymbolsPass(ASTContext context, string headersDir)
        : base(context)
    {
        _headersDir = headersDir;
    }

    public override bool VisitTranslationUnit(TranslationUnit unit)
    {
        if (!base.VisitTranslationUnit(unit))
        {
            return false;
        }


        if (unit.FilePath.EndsWith(".g.h"))
        {
            return true;
        }

        var relativePath = Path.GetRelativePath(_headersDir, unit.FilePath);

        if (!relativePath.Contains("os"))
        {
            unit.ExplicitlyIgnore();
        }
        else
        {
            relativePath = Path.GetRelativePath("os", relativePath);

            if (!relativePath.StartsWith(unit.Module.LibraryName.ToLowerInvariant()))
            {
                unit.ExplicitlyIgnore();
            }
        }

        return true;
    }

    public override bool VisitClassDecl(Class decl)
    {
        if (!base.VisitClassDecl(decl))
        {
            return false;
        }

        if (decl.QualifiedOriginalName.Contains("BPrivate"))
        {
            Console.WriteLine("Ignoring private class: " + decl.QualifiedOriginalName);
            decl.ExplicitlyIgnore();
        }

        // These are often POD structs that are used as parameters to standalone functions.
        // They are also (somewhat) unstable and subject to change.
        if (Path.GetRelativePath(Path.Combine(_headersDir, "os"), decl.TranslationUnit.FilePath).StartsWith("kernel"))
        {
            Console.WriteLine("Ignoring kernel class: " + decl.QualifiedOriginalName);
            decl.ExplicitlyIgnore();
        }

        foreach (var field in decl.Fields.ToArray())
        {
            foreach (var method in decl.Methods)
            {
                if (field.Name.Equals(method.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Ignoring field with same name as method: " + field.Name);
                    field.ExplicitlyIgnore();
                }
            }
        }

        return true;
    }

    public override bool VisitFunctionDecl(Function decl)
    {
        if (!base.VisitFunctionDecl(decl))
        {
            return false;
        }

        if (decl.Namespace is not Class)
        {
            decl.ExplicitlyIgnore();
        }

        return true;
    }

    public override bool VisitFieldDecl(Field decl)
    {
        if (!base.VisitFieldDecl(decl))
        {
            return false;
        }

        if (decl.Namespace is not Class)
        {
            decl.ExplicitlyIgnore();
        }

        return true;
    }

    public override bool VisitVariableDecl(Variable decl)
    {
        if (decl.Namespace is not Class)
        {
            Console.WriteLine("Ignoring non-member variable: " + decl.Name);
            decl.ExplicitlyIgnore();
        }

        return base.VisitVariableDecl(decl);
    }

    public override bool VisitProperty(Property decl)
    {
        if (!base.VisitProperty(decl))
        {
            return false;
        }

        if (decl.Namespace is not Class)
        {
            decl.ExplicitlyIgnore();
        }

        return true;
    }

    public override bool VisitEnumDecl(Enumeration decl)
    {
        if (!base.VisitEnumDecl(decl))
        {
            return false;
        }

        // Ignore unamed enums for now. We do not know what to do with them.
        if (string.IsNullOrEmpty(decl.Name) || decl.Name.StartsWith("__"))
        {
            Console.WriteLine($"Ignored unamed enum at {decl.TranslationUnit.FilePath}:{decl.LineNumberStart}");
            decl.ExplicitlyIgnore();
        }

        return true;
    }
}
