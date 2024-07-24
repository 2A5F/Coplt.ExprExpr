using System.Linq.Expressions;
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
