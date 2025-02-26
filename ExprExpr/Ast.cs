﻿using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Coplt.ExprExpr.Semantics;

namespace Coplt.ExprExpr.Syntaxes;

public abstract record Syntax(int offset)
{
    internal abstract Semantic ToSemantic();
}

public sealed record IdSyntax(int offset, string id) : Syntax(offset)
{
    public override string ToString() => id;
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

public sealed record CallSyntax(Syntax left, TupleSyntax args) : Syntax(left.offset)
{
    public override string ToString() => $"{left}{args}";
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

public sealed record IndexSyntax(Syntax left, ArraySyntax args) : Syntax(left.offset)
{
    public override string ToString() => $"{left}{args}";
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

public sealed record CondSyntax(Syntax cond, Syntax t, Syntax e) : Syntax(cond.offset)
{
    public override string ToString() => $"({cond} ? {t} : {e})";
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

public abstract record LiteralSyntax(int offset) : Syntax(offset)
{
    internal override Semantic ToSemantic() => new LiteralSemantic(this);

    internal abstract Type GetValueType();

    internal abstract ImmArr<Type> GetPossibleType();

    internal abstract Type GetDefaultType();

    internal abstract Expression ToExpr(Type target);
}

public enum StringType
{
    Utf16,
    Utf8,
    Char,
}

public sealed record StringLiteralSyntax(int offset, string value, StringType type) : LiteralSyntax(offset)
{
    public override string ToString() => $"\"{value.Replace("\"", "\"\"")}\"";
    internal override Type GetValueType() => type switch
    {
        StringType.Utf16 => typeof(string),
        StringType.Utf8 => typeof(ReadOnlyMemory<byte>),
        StringType.Char => typeof(char),
        _ => throw new ArgumentOutOfRangeException()
    };
    internal override ImmArr<Type> GetPossibleType() => [GetDefaultType()];
    internal override Type GetDefaultType() => type switch
    {
        StringType.Utf16 => typeof(string),
        StringType.Utf8 => typeof(ReadOnlyMemory<byte>),
        StringType.Char => typeof(char),
        _ => throw new ArgumentOutOfRangeException()
    };
    internal override Expression ToExpr(Type target) => type switch
    {
        StringType.Utf16 => target == typeof(char)
            ? Expression.Constant(value[0], typeof(char))
            : Expression.Constant(value, typeof(string)),
        StringType.Utf8 => Expression.Constant((ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(value),
            typeof(ReadOnlyMemory<byte>)),
        StringType.Char => Expression.Constant(value[0], typeof(char)),
        _ => throw new ArgumentOutOfRangeException()
    };
}

public sealed record NullLiteralSyntax(int offset) : LiteralSyntax(offset)
{
    public override string ToString() => "null";
    internal override Type GetValueType() => typeof(object);
    internal override ImmArr<Type> GetPossibleType() => [];
    internal override Type GetDefaultType() => typeof(object);
    internal override Expression ToExpr(Type target) => Expression.Constant(null, target);
}

public sealed record BoolLiteralSyntax(int offset, bool value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
    internal override Type GetValueType() => typeof(bool);
    internal override ImmArr<Type> GetPossibleType() => [typeof(bool)];
    internal override Type GetDefaultType() => typeof(bool);
    internal override Expression ToExpr(Type target) => Expression.Constant(value, typeof(bool));
}

public sealed record IntLiteralSyntax(int offset, int value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
    internal override Type GetValueType() => typeof(int);
    internal override ImmArr<Type> GetPossibleType() => Utils.LiteralIntTypes;
    internal override Type GetDefaultType() => typeof(int);
    internal override Expression ToExpr(Type target)
    {
        if (target == typeof(int)) return Expression.Constant(value, typeof(int));
        if (target == typeof(uint)) return Expression.Constant((uint)value, typeof(uint));
        if (target == typeof(long)) return Expression.Constant((long)value, typeof(long));
        if (target == typeof(ulong)) return Expression.Constant((ulong)value, typeof(ulong));
        if (target == typeof(float)) return Expression.Constant((float)value, typeof(float));
        if (target == typeof(double)) return Expression.Constant((double)value, typeof(double));
        if (target == typeof(decimal)) return Expression.Constant((decimal)value, typeof(decimal));
        if (target == typeof(sbyte)) return Expression.Constant((sbyte)value, typeof(sbyte));
        if (target == typeof(byte)) return Expression.Constant((byte)value, typeof(byte));
        if (target == typeof(short)) return Expression.Constant((short)value, typeof(short));
        if (target == typeof(ushort)) return Expression.Constant((ushort)value, typeof(ushort));
        return Expression.Constant(value, typeof(int));
    }
}

public sealed record UIntLiteralSyntax(int offset, uint value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
    internal override Type GetValueType() => typeof(uint);
    internal override ImmArr<Type> GetPossibleType() => [typeof(uint)];
    internal override Type GetDefaultType() => typeof(uint);
    internal override Expression ToExpr(Type target) => Expression.Constant(value, typeof(uint));
}

public sealed record LongLiteralSyntax(int offset, long value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
    internal override Type GetValueType() => typeof(long);
    internal override ImmArr<Type> GetPossibleType() => Utils.LiteralLongTypes;
    internal override Type GetDefaultType() => typeof(long);
    internal override Expression ToExpr(Type target)
    {
        if (target == typeof(long)) return Expression.Constant(value, typeof(long));
        if (target == typeof(ulong)) return Expression.Constant((ulong)value, typeof(ulong));
        if (target == typeof(float)) return Expression.Constant((float)value, typeof(float));
        if (target == typeof(double)) return Expression.Constant((double)value, typeof(double));
        if (target == typeof(decimal)) return Expression.Constant((decimal)value, typeof(decimal));
        return Expression.Constant(value, typeof(int));
    }
}

public sealed record ULongLiteralSyntax(int offset, ulong value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
    internal override Type GetValueType() => typeof(ulong);
    internal override ImmArr<Type> GetPossibleType() => [typeof(ulong)];
    internal override Type GetDefaultType() => typeof(ulong);
    internal override Expression ToExpr(Type target) => Expression.Constant(value, typeof(ulong));
}

public sealed record DoubleLiteralSyntax(int offset, double value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
    internal override Type GetValueType() => typeof(double);
    internal override ImmArr<Type> GetPossibleType() => [typeof(double)];
    internal override Type GetDefaultType() => typeof(double);
    internal override Expression ToExpr(Type target) => Expression.Constant(value, typeof(double));
}

public sealed record SingleLiteralSyntax(int offset, float value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
    internal override Type GetValueType() => typeof(float);
    internal override ImmArr<Type> GetPossibleType() => [typeof(float)];
    internal override Type GetDefaultType() => typeof(float);
    internal override Expression ToExpr(Type target) => Expression.Constant(value, typeof(float));
}

public sealed record DecimalLiteralSyntax(int offset, decimal value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
    internal override Type GetValueType() => typeof(decimal);
    internal override ImmArr<Type> GetPossibleType() => [typeof(decimal)];
    internal override Type GetDefaultType() => typeof(decimal);
    internal override Expression ToExpr(Type target) => Expression.Constant(value, typeof(decimal));
}

public enum OpKind
{
    None,
    /// <summary>
    /// <c>.</c>
    /// </summary>
    Path,
    /// <summary>
    /// <c>?.</c>
    /// </summary>
    TryPath,
    /// <summary>
    /// <c>-></c>
    /// </summary>
    PtrPath,
    /// <summary>
    /// <c>++</c>
    /// </summary>
    Inc,
    /// <summary>
    /// <c>--</c>
    /// </summary>
    Dec,
    /// <summary>
    /// <c>!</c>
    /// </summary>
    BoolNot,
    /// <summary>
    /// <c>~</c>
    /// </summary>
    Not,
    /// <summary>
    /// <c>+</c>
    /// </summary>
    Add,
    /// <summary>
    /// <c>-</c>
    /// </summary>
    Sub,
    /// <summary>
    /// <c>*</c>
    /// </summary>
    Mul,
    /// <summary>
    /// <c>/</c>
    /// </summary>
    Div,
    /// <summary>
    /// <c>%</c>
    /// </summary>
    Rem,
    /// <summary>
    /// <c>**</c>
    /// </summary>
    Pow,
    /// <summary>
    /// <c>&lt;&lt;</c>
    /// </summary>
    Shl,
    /// <summary>
    /// <c>>></c>
    /// </summary>
    Shr,
    /// <summary>
    /// <c>&lt;&lt;&lt;</c>
    /// </summary>
    Rol,
    /// <summary>
    /// <c>>>></c>
    /// </summary>
    Ror,
    /// <summary>
    /// <c>&</c>
    /// </summary>
    And,
    /// <summary>
    /// <c>|</c>
    /// </summary>
    Or,
    /// <summary>
    /// <c>^</c>
    /// </summary>
    Xor,
    /// <summary>
    /// <c>&&</c>
    /// </summary>
    BoolAnd,
    /// <summary>
    /// <c>||</c>
    /// </summary>
    BoolOr,
    /// <summary>
    /// <c>==</c>
    /// </summary>
    Eq,
    /// <summary>
    /// <c>!=</c>
    /// </summary>
    Ne,
    /// <summary>
    /// <c>&lt;</c>
    /// </summary>
    Lt,
    /// <summary>
    /// <c>></c>
    /// </summary>
    Gt,
    /// <summary>
    /// <c>&lt;=</c>
    /// </summary>
    Le,
    /// <summary>
    /// <c>>=</c>
    /// </summary>
    Ge,
    /// <summary>
    /// <c>??</c>
    /// </summary>
    NullCoalescing,
    /// <summary>
    /// <c>?!</c>
    /// </summary>
    NotNullCoalescing,
    /// <summary>
    /// <c>..</c>
    /// </summary>
    Range,
    /// <summary>
    /// <c>?</c>
    /// </summary>
    Cond,
}

public static class BinOpEx
{
    public static int Precedence(this OpKind kind) => kind switch
    {
        OpKind.Path or OpKind.TryPath or OpKind.PtrPath => 0,
        OpKind.Inc or OpKind.Dec or OpKind.BoolNot or OpKind.Not => 1,
        OpKind.Range => 2,
        OpKind.Mul or OpKind.Div or OpKind.Rem or OpKind.Pow => 3,
        OpKind.Add or OpKind.Sub => 4,
        OpKind.Shl or OpKind.Shr or OpKind.Rol or OpKind.Ror => 5,
        OpKind.Lt or OpKind.Gt or OpKind.Le or OpKind.Ge => 6,
        OpKind.Eq or OpKind.Ne => 7,
        OpKind.And => 8,
        OpKind.Xor => 9,
        OpKind.Or => 10,
        OpKind.BoolAnd => 11,
        OpKind.BoolOr => 12,
        OpKind.NullCoalescing or OpKind.NotNullCoalescing => 13,
        OpKind.Cond => 14,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// true: ← ,  false: →
    /// </summary>
    public static bool Associativity(this OpKind kind) => kind switch
    {
        OpKind.Pow => true,
        _ => false,
    };
}

public sealed record BinOpSyntax(Syntax left, Syntax right, OpKind kind) : Syntax(left.offset)
{
    public Syntax left { get; init; } = left;
    public Syntax right { get; init; } = right;
    public OpKind kind { get; init; } = kind;

    public override string ToString() => $"({left} {kind} {right})";
    internal override Semantic ToSemantic()
    {
        switch (kind)
        {
            case OpKind.Add or OpKind.Sub or OpKind.Mul or OpKind.Div or OpKind.Rem or OpKind.Pow
                or OpKind.Shl or OpKind.Shr or OpKind.Rol or OpKind.Ror or OpKind.And or OpKind.Or or OpKind.Xor
                or OpKind.Eq or OpKind.Ne or OpKind.Le or OpKind.Ge or OpKind.Lt or OpKind.Gt:
                return new BinOpSemantic(left.ToSemantic(), right.ToSemantic(), kind, this);
            // todo other
            default:
                throw new EvalException($"{kind} is not supported as a binary operator at {offset}");
        }
    }
}

public sealed record PrefixOpSyntax(int offset, Syntax right, OpKind kind) : Syntax(offset)
{
    public Syntax right { get; init; } = right;
    public OpKind kind { get; init; } = kind;

    public override string ToString() => $"({kind} {right})";
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

public sealed record SuffixOpSyntax(Syntax left, OpKind kind) : Syntax(left.offset)
{
    public Syntax left { get; init; } = left;
    public OpKind kind { get; init; } = kind;

    public override string ToString() => $"({left} {kind})";
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

public sealed record TupleSyntax(int offset, List<Syntax> items) : Syntax(offset)
{
    public override string ToString() => $"({string.Join(", ", items)})";

    public bool Equals(TupleSyntax? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && items.SequenceEqual(other.items);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var item in items)
        {
            hash.Add(item.GetHashCode());
        }
        return hash.ToHashCode();
    }
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

public sealed record ArraySyntax(int offset, List<Syntax> items) : Syntax(offset)
{
    public override string ToString() => $"[{string.Join(", ", items)}]";

    public bool Equals(ArraySyntax? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && items.SequenceEqual(other.items);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var item in items)
        {
            hash.Add(item.GetHashCode());
        }
        return hash.ToHashCode();
    }
    internal override Semantic ToSemantic()
    {
        throw new NotImplementedException();
    }
}

// // currently not supported
//
// public abstract record StringFormatPart(int offset)
// {
//     public sealed record TextPart(int offset, string value) : StringFormatPart(offset)
//     {
//         public override string ToString() => value.Replace("\"", "\"\"");
//     }
//
//     public sealed record ExprPart(int offset, Syntax expr, string? format) : StringFormatPart(offset)
//     {
//         public override string ToString() => $"{{{expr}{(format != null ? ":" : "")}{format?.Replace("\"", "\"\"")}}}";
//     }
// }
//
// public sealed record StringFormatSyntax(int offset, List<StringFormatPart> parts) : Syntax(offset)
// {
//     public override string ToString() => $"$\"{string.Join("", parts)}\"";
// }
