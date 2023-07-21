using CppSharp.AST;

namespace HaikuApiGenerator.Passes;

public class ProcessIncompleteTypesPass : TranslationUnitPass
{
    public ProcessIncompleteTypesPass(ASTContext astContext)
        : base(astContext)
    {
    }

    public override bool VisitClassDecl(Class decl)
    {
        if (!base.VisitClassDecl(decl))
        {
            return false;
        }

        if (decl.Ignore)
        {
            return true;
        }

        if (decl.IsIncomplete)
        {
            if (_astContext.FindCompleteClass(decl.Name) == null)
            {
                Console.WriteLine($"Incomplete class {decl.Name} has no complete definition");
            }
        }

        return true;
    }

    public override bool VisitTagType(TagType tag, TypeQualifiers quals)
    {
        if (!base.VisitTagType(tag, quals))
        {
            return false;
        }

        HandleType(tag);
        return true;
    }

    public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
    {
        if (!base.VisitArrayType(array, quals))
        {
            return false;
        }

        HandleType(array.Type);
        return true;
    }

    public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
    {
        if (!base.VisitPointerType(pointer, quals))
        {
            return false;
        }

        HandleType(pointer.Pointee);
        return true;
    }

    public override bool VisitQualifiedType(QualifiedType qualType)
    {
        if (!base.VisitQualifiedType(qualType))
        {
            return false;
        }

        HandleType(qualType.Type);
        return true;
    }

    public override bool VisitFunctionDecl(Function function)
    {
        if (!base.VisitFunctionDecl(function))
        {
            return false;
        }

        HandleType(function.ReturnType.Type);
        foreach (var param in function.Parameters)
        {
            HandleType(param.Type);
        }

        return true;
    }

    private void HandleType(TagType type)
    {
        if (type.Declaration is not Class classDecl)
        {
            return;
        }

        if (!classDecl.IsIncomplete)
        {
            return;
        }

        var newDeclaration = _astContext.FindCompleteClass(classDecl.Name) ?? type.Declaration;

        if (newDeclaration != type.Declaration)
        {
            type.Declaration = newDeclaration;
        }
    }

    private void HandleType(CppSharp.AST.Type type)
    {
        switch (type)
        {
            case TagType tagType:
                HandleType(tagType);
                break;
            case PointerType pointerType:
                HandleType(pointerType.Pointee);
                break;
            case TypedefType typedefType:
                HandleType(typedefType.Declaration.Type);
                break;
            case ArrayType arrayType:
                HandleType(arrayType.Type);
                break;
            case AttributedType attributedType:
                HandleType(attributedType.Modified.Type);
                break;
        }
    }
}
