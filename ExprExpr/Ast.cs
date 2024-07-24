using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;

namespace Coplt.ExprExpr.Syntaxes;

public abstract record Syntax(int offset);

public sealed record IdSyntax(int offset, string id) : Syntax(offset)
{
    public override string ToString() => id;
}

public sealed record CallSyntax(Syntax left, TupleSyntax args) : Syntax(left.offset)
{
    public override string ToString() => $"{left}{args}";
}

public sealed record IndexSyntax(Syntax left, ArraySyntax args) : Syntax(left.offset)
{
    public override string ToString() => $"{left}{args}";
}

public sealed record CondSyntax(Syntax cond, Syntax t, Syntax e) : Syntax(cond.offset)
{
    public override string ToString() => $"({cond} ? {t} : {e})";
}

public abstract record LiteralSyntax(int offset) : Syntax(offset);

public enum StringType
{
    Utf16,
    Utf8,
    Char,
}

public sealed record StringLiteralSyntax(int offset, string value, StringType type) : LiteralSyntax(offset)
{
    public override string ToString() => $"\"{value.Replace("\"", "\"\"")}\"";
}

public sealed record NullLiteralSyntax(int offset) : LiteralSyntax(offset)
{
    public override string ToString() => "null";
}

public sealed record BoolLiteralSyntax(int offset, bool value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
}

public sealed record IntLiteralSyntax(int offset, int value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
}

public sealed record UIntLiteralSyntax(int offset, uint value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
}

public sealed record LongLiteralSyntax(int offset, long value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
}

public sealed record ULongLiteralSyntax(int offset, ulong value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString();
}

public sealed record DoubleLiteralSyntax(int offset, double value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
}

public sealed record SingleLiteralSyntax(int offset, float value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
}

public sealed record DecimalLiteralSyntax(int offset, decimal value) : LiteralSyntax(offset)
{
    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
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
    /// <c>>>></c>
    /// </summary>
    Shar,
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
        OpKind.Shl or OpKind.Shr or OpKind.Shar => 5,
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
}

public sealed record PrefixOpSyntax(int offset, Syntax right, OpKind kind) : Syntax(offset)
{
    public Syntax right { get; init; } = right;
    public OpKind kind { get; init; } = kind;

    public override string ToString() => $"({kind} {right})";
}

public sealed record SuffixOpSyntax(Syntax left, OpKind kind) : Syntax(left.offset)
{
    public Syntax left { get; init; } = left;
    public OpKind kind { get; init; } = kind;

    public override string ToString() => $"({left} {kind})";
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
