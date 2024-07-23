using System.Collections.Immutable;
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

public enum BinOpKind
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
    public static int Precedence(this BinOpKind kind) => kind switch
    {
        BinOpKind.Path or BinOpKind.TryPath or BinOpKind.PtrPath => 0,
        BinOpKind.Inc or BinOpKind.Dec or BinOpKind.BoolNot or BinOpKind.Not => 1,
        BinOpKind.Range => 2,
        BinOpKind.Mul or BinOpKind.Div or BinOpKind.Rem or BinOpKind.Pow => 3,
        BinOpKind.Add or BinOpKind.Sub => 4,
        BinOpKind.Shl or BinOpKind.Shr or BinOpKind.Shar => 5,
        BinOpKind.Lt or BinOpKind.Gt or BinOpKind.Le or BinOpKind.Ge => 6,
        BinOpKind.Eq or BinOpKind.Ne => 7,
        BinOpKind.And => 8,
        BinOpKind.Xor => 9,
        BinOpKind.Or => 10,
        BinOpKind.BoolAnd => 11,
        BinOpKind.BoolOr => 12,
        BinOpKind.NullCoalescing or BinOpKind.NotNullCoalescing => 13,
        BinOpKind.Cond => 14,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// true: ← ,  false: →
    /// </summary>
    public static bool Associativity(this BinOpKind kind) => kind switch
    {
        BinOpKind.Pow => true,
        _ => false,
    };
}

public sealed record BinOpSyntax(Syntax left, Syntax right, BinOpKind kind) : Syntax(left.offset)
{
    public Syntax left { get; init; } = left;
    public Syntax right { get; init; } = right;
    public BinOpKind kind { get; init; } = kind;

    public override string ToString() => $"({left} {kind} {right})";
}

public sealed record PrefixOpSyntax(int offset, Syntax right, BinOpKind kind) : Syntax(offset)
{
    public Syntax right { get; init; } = right;
    public BinOpKind kind { get; init; } = kind;

    public override string ToString() => $"({kind} {right})";
}

public sealed record SuffixOpSyntax(Syntax left, BinOpKind kind) : Syntax(left.offset)
{
    public Syntax left { get; init; } = left;
    public BinOpKind kind { get; init; } = kind;

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
