using CppSharp.AST;
using System.Numerics;

namespace HaikuApiGenerator;

public static class Helpers
{
    public static BuiltinTypeExpressionObsolete ToExpression(this Enumeration.Item item)
    {
        return item.Value.ToExpression(((BuiltinType)((Enumeration)item.Namespace).Type).Type);
    }

    public static BuiltinTypeExpressionObsolete ToExpression<T>(this T number, PrimitiveType type)
        where T : IBinaryInteger<T>
    {
        var expression = new BuiltinTypeExpressionObsolete()
        {
            Type = new BuiltinType()
            {
                Type = type,
            },
            String = null
        };

        var arr = new byte[number.GetByteCount()];
        number.WriteLittleEndian(arr);
        var bigNumber = new BigInteger(arr, T.Sign(number) != -1, false);
        var longNumber = (ulong)bigNumber;

        switch (expression.Type.Type)
        {
            case PrimitiveType.Char:
            case PrimitiveType.SChar:
                expression.Value = unchecked((sbyte)longNumber);
                break;
            case PrimitiveType.UChar:
                expression.Value = unchecked((byte)longNumber);
                break;
            case PrimitiveType.Short:
                expression.Value = unchecked((short)longNumber);
                break;
            case PrimitiveType.UShort:
                expression.Value = unchecked((ushort)longNumber);
                break;
            case PrimitiveType.Int:
                expression.Value = unchecked((int)longNumber);
                break;
            case PrimitiveType.UInt:
                expression.Value = unchecked((uint)longNumber);
                break;
            case PrimitiveType.Long:
                expression.Value = unchecked((long)longNumber);
                break;
            case PrimitiveType.ULong:
                expression.Value = unchecked((long)longNumber);
                expression.String = unchecked((ulong)longNumber).ToString();
                break;
            default:
                throw new Exception($"Unsupported enum type {expression.Type.Type}");
        }

        if (expression.String == null)
        {
            expression.String = expression.Value.ToString();
        }

        return expression;
    }

    public static bool IsHaikuValueName(this string name)
    {
        return name.StartsWith("B_") || name.StartsWith("be_");
    }
}
