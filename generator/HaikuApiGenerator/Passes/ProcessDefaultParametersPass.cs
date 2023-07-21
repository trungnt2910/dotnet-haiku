using CppSharp.AST;

namespace HaikuApiGenerator.Passes;

class ProcessDefaultParametersPass : TranslationUnitPass
{
    public ProcessDefaultParametersPass(ASTContext astContext)
        : base(astContext)
    {
    }

    public override bool VisitParameterDecl(Parameter decl)
    {
        if (!base.VisitParameterDecl(decl))
        {
            return false;
        }

        if (!decl.HasDefaultValue || decl.DefaultArgument == null)
        {
            return true;
        }

        ProcessDeclarations(decl.DefaultArgument);

        return true;
    }

    public override bool VisitFunctionDecl(Function decl)
    {
        if (!base.VisitFunctionDecl(decl))
        {
            return false;
        }

        foreach (var param in decl.Parameters)
        {
            if (!param.HasDefaultValue || param.DefaultArgument == null)
            {
                continue;
            }
            ProcessDeclarations(param.DefaultArgument);
        }

        return true;
    }

    public override bool VisitMethodDecl(Method decl)
    {
        if (!base.VisitMethodDecl(decl))
        {
            return false;
        }

        foreach (var param in decl.Parameters)
        {
            if (!param.HasDefaultValue || param.DefaultArgument == null)
            {
                continue;
            }
            ProcessDeclarations(param.DefaultArgument);
        }

        return true;
    }

    private void ProcessDeclarations(ExpressionObsolete expression)
    {
        switch (expression.Class)
        {
            case StatementClass.DeclarationReference:
            {
                if (expression.Declaration is not Variable variable)
                {
                    return;
                }

                if (!variable.OriginalName.IsHaikuValueName())
                {
                    return;
                }

                var symbolsClass = variable.TranslationUnit.Module.Units
                    .SelectMany(unit => unit.Classes)
                    .Single(cls => cls.Name == "Symbols");

                var symbolVariable = symbolsClass.Variables
                    .SingleOrDefault(var => var.Name == variable.OriginalName);

                if (symbolVariable != null)
                {
                    expression.Declaration = symbolVariable;
                }
            }
            break;
            case StatementClass.BinaryOperator:
            {
                var binaryExpression = (BinaryOperatorObsolete)expression;
                ProcessDeclarations(binaryExpression.LHS);
                ProcessDeclarations(binaryExpression.RHS);
            }
            break;
            case StatementClass.ConstructorReference:
            {
                var declarationReference = (CXXConstructExprObsolete)expression;
                foreach (var argument in declarationReference.Arguments)
                {
                    ProcessDeclarations(argument);
                }
            }
            break;
            case StatementClass.Any:
            break;
            default:
                throw new NotImplementedException();
        }
    }
}