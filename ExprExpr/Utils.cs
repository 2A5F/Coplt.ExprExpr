using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using InlineIL;
using static InlineIL.IL.Emit;

namespace Coplt.ExprExpr;

internal static class Utils
{
    // ReSharper disable once EntityNameCapturedOnly.Global
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T UnboxUnsafe<T>(object obj)
    {
        Ldarg(nameof(obj));
        Unbox<T>();
        return ref IL.ReturnRef<T>();
    }

    public static bool HasInterface(Type type, Type i)
    {
        return type.GetInterfaces()
            .Any(a => a.IsGenericType && a.GetGenericTypeDefinition() == i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression MakePowerExpr(Expression left, Expression right, Type type)
    {
        if (type == typeof(double)) return Expression.Power(left, right);
        if (type == typeof(float)) return Expression.Call(PowMethod(type), left, right);
        throw new NotSupportedException($"{type} does not support power");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo PowMethod(Type type)
    {
        if (type == typeof(float))
        {
            Ldtoken(new MethodRef(typeof(MathF), nameof(MathF.Pow)));
            return IL.Return<MethodInfo>();
        }
        if (type == typeof(double))
        {
            Ldtoken(new MethodRef(typeof(Math), nameof(Math.Pow)));
            return IL.Return<MethodInfo>();
        }
        throw new NotSupportedException($"{type} does not support power");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo RotateLeftMethod(Type type)
    {
        if (type == typeof(uint))
        {
            Ldtoken(new MethodRef(typeof(BitOperations), nameof(BitOperations.RotateLeft), typeof(uint), typeof(int)));
            return IL.Return<MethodInfo>();
        }
        if (type == typeof(ulong))
        {
            Ldtoken(new MethodRef(typeof(BitOperations), nameof(BitOperations.RotateLeft), typeof(ulong), typeof(int)));
            return IL.Return<MethodInfo>();
        }
        if (type == typeof(nuint))
        {
            Ldtoken(new MethodRef(typeof(BitOperations), nameof(BitOperations.RotateLeft), typeof(nuint), typeof(int)));
            return IL.Return<MethodInfo>();
        }
        throw new NotSupportedException($"{type} does not support rotate shift");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo RotateRightMethod(Type type)
    {
        if (type == typeof(uint))
        {
            Ldtoken(new MethodRef(typeof(BitOperations), nameof(BitOperations.RotateRight), typeof(uint), typeof(int)));
            return IL.Return<MethodInfo>();
        }
        if (type == typeof(ulong))
        {
            Ldtoken(new MethodRef(typeof(BitOperations), nameof(BitOperations.RotateRight), typeof(ulong),
                typeof(int)));
            return IL.Return<MethodInfo>();
        }
        if (type == typeof(nuint))
        {
            Ldtoken(new MethodRef(typeof(BitOperations), nameof(BitOperations.RotateRight), typeof(nuint),
                typeof(int)));
            return IL.Return<MethodInfo>();
        }
        throw new NotSupportedException($"{type} does not support rotate shift");
    }

    public static ImmArr<Type> LiteralIntTypes { get; } =
    [
        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
    ];
    public static ImmArr<Type> LiteralLongTypes { get; } =
    [
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
    ];

    public static readonly Type[] Int32ConversionTarget =
        [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)];

    public static readonly Type[] UInt32ConversionTarget =
        [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)];

    public static readonly Type[] Int64ConversionTarget =
        [typeof(long), typeof(float), typeof(double), typeof(decimal)];

    public static readonly Type[] UInt64ConversionTarget =
        [typeof(ulong), typeof(float), typeof(double), typeof(decimal)];

    public static readonly Type[] SingleConversionTarget =
        [typeof(float), typeof(double), typeof(decimal)];

    public static readonly Type[] CharConversionTarget =
    [
        typeof(char), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float),
        typeof(double), typeof(decimal)
    ];

    public static bool PrimitiveCanConversion(Type src, Type dst)
    {
        if (src == dst) return true;
        if (src == typeof(int)) return Int32ConversionTarget.Contains(dst);
        if (src == typeof(uint)) return UInt32ConversionTarget.Contains(dst);
        if (src == typeof(long)) return Int64ConversionTarget.Contains(dst);
        if (src == typeof(ulong)) return UInt64ConversionTarget.Contains(dst);
        if (src == typeof(float)) return SingleConversionTarget.Contains(dst);
        if (src == typeof(char)) return CharConversionTarget.Contains(dst);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<Expression, Expression>? Int32Conversion(Type target)
    {
        if (target == typeof(int)) return ConvertIdentity;
        if (target == typeof(long)) return ConvertToInt64;
        if (target == typeof(float)) return ConvertToSingle;
        if (target == typeof(double)) return ConvertToDouble;
        if (target == typeof(decimal)) return ConvertToDecimal;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<Expression, Expression>? UInt32Conversion(Type target)
    {
        if (target == typeof(uint)) return ConvertIdentity;
        if (target == typeof(long)) return ConvertToInt64;
        if (target == typeof(ulong)) return ConvertToUInt64;
        if (target == typeof(float)) return ConvertToSingle;
        if (target == typeof(double)) return ConvertToDouble;
        if (target == typeof(decimal)) return ConvertToDecimal;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<Expression, Expression>? Int64Conversion(Type target)
    {
        if (target == typeof(long)) return ConvertIdentity;
        if (target == typeof(float)) return ConvertToSingle;
        if (target == typeof(double)) return ConvertToDouble;
        if (target == typeof(decimal)) return ConvertToDecimal;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<Expression, Expression>? UInt64Conversion(Type target)
    {
        if (target == typeof(ulong)) return ConvertIdentity;
        if (target == typeof(float)) return ConvertToSingle;
        if (target == typeof(double)) return ConvertToDouble;
        if (target == typeof(decimal)) return ConvertToDecimal;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<Expression, Expression>? SingleConversion(Type target)
    {
        if (target == typeof(float)) return ConvertIdentity;
        if (target == typeof(double)) return ConvertToDouble;
        if (target == typeof(decimal)) return ConvertToDecimal;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<Expression, Expression>? CharConversion(Type target)
    {
        if (target == typeof(char)) return ConvertIdentity;
        if (target == typeof(ushort)) return ConvertToUInt16;
        if (target == typeof(int)) return ConvertToInt32;
        if (target == typeof(uint)) return ConvertToUInt32;
        if (target == typeof(long)) return ConvertToInt64;
        if (target == typeof(ulong)) return ConvertToUInt64;
        if (target == typeof(float)) return ConvertToSingle;
        if (target == typeof(double)) return ConvertToDouble;
        if (target == typeof(decimal)) return ConvertToDecimal;
        return null;
    }

    public static Expression ConvertIdentity(Expression expr) => expr;
    public static Expression ConvertToObject(Expression expr) => Expression.Convert(expr, typeof(object));

    public static Expression ConvertToByte(Expression expr) => Expression.Convert(expr, typeof(byte));
    public static Expression ConvertToSbyte(Expression expr) => Expression.Convert(expr, typeof(sbyte));
    public static Expression ConvertToInt16(Expression expr) => Expression.Convert(expr, typeof(short));
    public static Expression ConvertToUInt16(Expression expr) => Expression.Convert(expr, typeof(ushort));
    public static Expression ConvertToInt32(Expression expr) => Expression.Convert(expr, typeof(int));
    public static Expression ConvertToUInt32(Expression expr) => Expression.Convert(expr, typeof(uint));
    public static Expression ConvertToInt64(Expression expr) => Expression.Convert(expr, typeof(long));
    public static Expression ConvertToUInt64(Expression expr) => Expression.Convert(expr, typeof(ulong));
    public static Expression ConvertToSingle(Expression expr) => Expression.Convert(expr, typeof(float));
    public static Expression ConvertToDouble(Expression expr) => Expression.Convert(expr, typeof(double));
    public static Expression ConvertToDecimal(Expression expr) => Expression.Convert(expr, typeof(decimal));
}
