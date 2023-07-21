using CppSharp.AST;

namespace HaikuApiGenerator.Passes;

public class RestoreNamePass : TranslationUnitPass
{
    public RestoreNamePass(ASTContext astContext)
        : base(astContext)
    {
    }

    public override bool VisitClassDecl(Class decl)
    {
        if (!base.VisitClassDecl(decl))
        {
            return false;
        }

        if (decl.Name == "Symbols")
        {
            foreach (var variable in decl.Variables)
            {
                variable.Name = variable.OriginalName;
            }
        }

        return true;
    }
}
