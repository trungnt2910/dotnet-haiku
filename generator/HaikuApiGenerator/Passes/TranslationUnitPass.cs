using CppSharp.AST;

namespace HaikuApiGenerator.Passes;

public abstract class TranslationUnitPass : CppSharp.Passes.TranslationUnitPass
{
    protected readonly ASTContext _astContext;

    protected TranslationUnitPass(ASTContext astContext)
    {
        _astContext = astContext;
    }

    public override bool VisitASTContext(ASTContext context)
    {
        if (context != _astContext)
        {
            return false;
        }

        if (!base.VisitASTContext(context))
        {
            return false;
        }

        return true;
    }
}