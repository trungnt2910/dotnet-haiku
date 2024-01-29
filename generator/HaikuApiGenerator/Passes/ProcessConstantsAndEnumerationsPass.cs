using CppSharp.AST;

namespace HaikuApiGenerator.Passes;

public class ProcessConstantsAndEnumerationsPass : TranslationUnitPass
{
    private readonly Dictionary<Module, Class> _symbolsClasses = new();

    public ProcessConstantsAndEnumerationsPass(ASTContext astContext)
        : base(astContext)
    {
        // Pre-create the symbols class for each module.
        foreach (var module in _astContext.TranslationUnits.Select(m => m.Module).Where(m => m != null).ToHashSet())
        {
            var translationUnitName = $"{module.LibraryName}.Symbols.g.h";
            var translationUnit = _astContext.FindOrCreateTranslationUnit(translationUnitName);
            translationUnit.Name = "";
            translationUnit.Module = module;
            translationUnit.GenerationKind = GenerationKind.Generate;
            module.Units.Add(translationUnit);

            var symbolsClass = new Class()
            {
                Name = "Symbols",
                Namespace = translationUnit,
                IsIncomplete = false,
                IsStatic = true,
                GenerationKind = GenerationKind.Generate
            };

            translationUnit.Declarations.Add(symbolsClass);
            _symbolsClasses.Add(module, symbolsClass);
        }
    }

    public override bool VisitEnumItemDecl(Enumeration.Item decl)
    {
        if (!decl.Name.IsHaikuValueName())
        {
            goto skip;
        }

        if (decl.Namespace.Namespace is Class)
        {
            goto skip;
        }

        var symbolsClass = GetSymbolsClass(decl);
        if (decl.Namespace == symbolsClass)
        {
            goto skip;
        }

        var variable = new Variable
        {
            Name = decl.Name,
            OriginalName = decl.OriginalName,
            QualifiedType =
                new QualifiedType(((Enumeration)decl.Namespace).Type,
                    new TypeQualifiers() { IsConst = true } ),
            Access = AccessSpecifier.Public,
            Namespace = symbolsClass,
            IsConstExpr = true,
            Initializer = decl.ToExpression(),
            Mangled = "",
            GenerationKind = GenerationKind.Generate
        };

        symbolsClass.Declarations.Add(variable);

    skip:
        return base.VisitEnumItemDecl(decl);
    }

    public override bool VisitVariableDecl(Variable decl)
    {
        if (!decl.Name.IsHaikuValueName())
        {
            goto skip;
        }

        var symbolsClass = GetSymbolsClass(decl);
        if (decl.Namespace == symbolsClass)
        {
            goto skip;
        }

        var variable = new Variable
        {
            Name = decl.Name,
            OriginalName = decl.OriginalName,
            QualifiedType = decl.QualifiedType,
            Access = AccessSpecifier.Public,
            Namespace = symbolsClass,
            IsConstExpr = decl.IsConstExpr,
            Initializer = decl.Initializer,
            Mangled = decl.Mangled,
            GenerationKind = GenerationKind.Generate
        };

        symbolsClass.Declarations.Add(variable);

    skip:
        return base.VisitVariableDecl(decl);
    }

    private Class GetSymbolsClass(Declaration decl)
    {
        if (decl.TranslationUnit.Module == null)
        {
            throw new Exception($"Declaration {decl.DebugText} has no module");
        }

        return _symbolsClasses[decl.TranslationUnit.Module];
    }
}
