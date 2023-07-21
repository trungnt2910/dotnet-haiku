using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace HaikuApiGenerator.Passes;

public class EliminateFloatOverloadsPass : TranslationUnitPass
{
    public EliminateFloatOverloadsPass(ASTContext astContext)
        : base(astContext)
    {
    }

    public override bool VisitClassDecl(Class decl)
    {
        if (!base.VisitClassDecl(decl))
        {
            return false;
        }

        var methodNames = decl.Methods
            .Where(m => m.IsSynthetized
                        && m.SynthKind == FunctionSynthKind.DefaultValueOverload
                        && !m.IsConstructor && !m.IsDestructor)
            .Select(m => m.Name)
            .ToHashSet();

        foreach (var name in methodNames)
        {
            foreach (var method in decl.Methods
                .Where(m => m.Name == name && m.IsSynthetized && m.SynthKind == FunctionSynthKind.DefaultValueOverload)
                .OrderBy(m => m.Parameters.FindIndex(p => p.GenerationKind == GenerationKind.None))
                .ToList())
            {
                var overloadIndex = method.Parameters.FindIndex(p => p.GenerationKind == GenerationKind.None);

                var isReferenceToConstantVariable = method.Parameters[overloadIndex].DefaultArgument.Class == StatementClass.DeclarationReference
                    && method.Parameters[overloadIndex].DefaultArgument.Declaration is Variable variable
                    && variable.Type is BuiltinType builtinType
                    && variable.QualifiedType.Qualifiers.IsConst;

                // Somehow CppSharp does not like UChar
                // https://github.com/mono/CppSharp/blob/1ce9cb7e7fff2dae3bdce05a6a2231b1919b4cab/src/Generator/Passes/ExpressionHelper.cs#L36
                var isByte = method.Parameters[overloadIndex].Type.Desugar().IsPrimitiveType(PrimitiveType.UChar);

                if (isReferenceToConstantVariable || isByte)
                {
                    var originalParameter = method.OriginalFunction.Parameters[overloadIndex];
                    originalParameter.DefaultArgument = originalParameter.OriginalDefaultArgument;
                    decl.Methods.Remove(method);
                }
                else
                {
                    break;
                }
            }
        }


        return true;
    }
}
