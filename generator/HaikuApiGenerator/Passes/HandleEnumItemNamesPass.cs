using CppSharp.AST;

namespace HaikuApiGenerator.Passes;

public class HandleEnumItemNamesPass : TranslationUnitPass
{
    public HandleEnumItemNamesPass(ASTContext astContext)
        : base(astContext)
    {
    }

    public override bool VisitEnumItemDecl(Enumeration.Item decl)
    {
        if (!base.VisitEnumItemDecl(decl))
        {
            return false;
        }

        if (decl.OriginalName.IsHaikuValueName())
        {
            decl.Name = string.Join("",
                decl.OriginalName.Split('_').Skip(1)
                    .Select(token => token[0].ToString().ToUpperInvariant() + token[1..].ToLowerInvariant()));
        }

        return true;
    }
}
