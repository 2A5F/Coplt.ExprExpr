﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using Coplt.ExprExpr.Syntaxes;

namespace Coplt.ExprExpr.Parsers;

public partial class Parser
{
    public Syntax Parse(Str str)
    {
        Code code = str;
        var syn = Root(code, int.MaxValue, out var result);
        if (result)
        {
            if (result.Last.IsNotEmpty) throw new ParserException($"Unexpected Character at {result.Last.Offset}");
            return syn!;
        }
        throw new ParserException($"Unexpected Character at 0");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Syntax? Root(Code code, int max_precedence, out Result result)
    {
        var syn = Op(code, max_precedence, out result);
        if (result) return syn;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Syntax? Op(Code code, int max_precedence, out Result result)
    {
        var left = NoOpSyntax(code, out result);
        if (!result) return null;
        for (;;)
        {
            var op = OpKind(result.Last, out var r);
            if (!r) break;
            var precedence = op.Precedence();
            var associativity = op.Associativity();
            if (precedence > max_precedence) break;
            var right = Root(r.Last, associativity ? precedence : precedence - 1, out var r2);
            if (!r2)
            {
                result &= r;
                left = new SuffixOpSyntax(left!, op);
                continue;
            }
            result &= r2;
            if (op is BinOpKind.Cond)
                left = Cond(left!, right!, precedence - 1, ref result);
            else left = new BinOpSyntax(left!, right!, op);
        }
        return left;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CondSyntax? Cond(Syntax left, Syntax right, int max_precedence, ref Result result)
    {
        SkipSpace(ref result);
        if (result.Last.IsEmpty || result.Last[0] != ':')
            throw new ParserException($"The conditional expression is missing the else part at {result.Last.Offset}");
        result &= Result.Ok(result.Last, 1);
        SkipSpace(ref result);
        var el = Root(result.Last, max_precedence, out var r);
        if (!r)
            throw new ParserException($"The conditional expression is missing the else part at {result.Last.Offset}");
        result &= r;
        return new CondSyntax(left, right, el!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BinOpKind OpKind(Code code, out Result result)
    {
        if (code.IsEmpty)
        {
            result = Result.Failed();
            return BinOpKind.None;
        }
        switch (code[0])
        {
            case '!':
                if (code.Length >= 2)
                {
                    switch (code[1])
                    {
                        case '=':
                            result = Result.Ok(code, 2);
                            return BinOpKind.Ne;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.BoolNot;
            case '~':
                result = Result.Ok(code, 1);
                return BinOpKind.Not;
            case '.':
                if (code.Length >= 2)
                {
                    if (code[1] == '.')
                    {
                        result = Result.Ok(code, 2);
                        return BinOpKind.Range;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Path;
            case '+':
                if (code.Length >= 2)
                {
                    if (code[1] == '+')
                    {
                        result = Result.Ok(code, 2);
                        return BinOpKind.Inc;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Add;
            case '-':
                if (code.Length >= 2)
                {
                    switch (code[1])
                    {
                        case '>':
                            result = Result.Ok(code, 2);
                            return BinOpKind.PtrPath;
                        case '-':
                            result = Result.Ok(code, 2);
                            return BinOpKind.Dec;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Sub;
            case '*':
                if (code.Length >= 2)
                {
                    if (code[1] == '*')
                    {
                        result = Result.Ok(code, 2);
                        return BinOpKind.Pow;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Mul;
            case '/':
                result = Result.Ok(code, 1);
                return BinOpKind.Div;
            case '%':
                result = Result.Ok(code, 1);
                return BinOpKind.Rem;
            case '^':
                result = Result.Ok(code, 1);
                return BinOpKind.Xor;
            case '|':
                if (code.Length >= 2)
                {
                    if (code[1] == '|')
                    {
                        result = Result.Ok(code, 2);
                        return BinOpKind.BoolOr;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Or;
            case '&':
                if (code.Length >= 2)
                {
                    if (code[1] == '&')
                    {
                        result = Result.Ok(code, 2);
                        return BinOpKind.BoolAnd;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.And;
            case '=':
                if (code.Length >= 2)
                {
                    if (code[1] == '=')
                    {
                        result = Result.Ok(code, 2);
                        return BinOpKind.Eq;
                    }
                }
                break;
            case '?':
                if (code.Length >= 2)
                {
                    switch (code[1])
                    {
                        case '?':
                            result = Result.Ok(code, 2);
                            return BinOpKind.NullCoalescing;
                        case '!':
                            result = Result.Ok(code, 2);
                            return BinOpKind.NotNullCoalescing;
                        case '.':
                            result = Result.Ok(code, 2);
                            return BinOpKind.TryPath;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Cond;
            case '<':
                if (code.Length >= 2)
                {
                    switch (code[1])
                    {
                        case '<':
                            result = Result.Ok(code, 2);
                            return BinOpKind.Shl;
                        case '=':
                            result = Result.Ok(code, 2);
                            return BinOpKind.Le;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Lt;
            case '>':
                if (code.Length >= 2)
                {
                    switch (code[1])
                    {
                        case '>':
                            if (code.Length >= 3)
                            {
                                if (code[2] == '>')
                                {
                                    result = Result.Ok(code, 3);
                                    return BinOpKind.Shar;
                                }
                            }
                            result = Result.Ok(code, 2);
                            return BinOpKind.Shr;
                        case '=':
                            result = Result.Ok(code, 2);
                            return BinOpKind.Ge;
                    }
                }
                result = Result.Ok(code, 1);
                return BinOpKind.Gt;
        }
        result = Result.Failed();
        return BinOpKind.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Syntax? NoOpSyntax(Code code, out Result result)
    {
        var r = new Result(false, default, code);
        SkipSpace(ref r);

        Syntax? syn = null;
        syn = Tuple(r.Last, out var r2);
        if (r2)
        {
            r &= r2;
            goto end;
        }
        syn = Array(r.Last, out r2);
        if (r2)
        {
            r &= r2;
            goto end;
        }
        syn = PrefixOpSyntax(r.Last, out r2);
        if (r2)
        {
            r &= r2;
            goto end;
        }
        syn = Literal(r.Last, out r2);
        if (r2)
        {
            r &= r2;
            goto end;
        }
        syn = Id(r.Last, out r2);
        if (r2)
        {
            r &= r2;
            goto end;
        }

        end:

        SkipSpace(ref r);

        if (syn != null)
        {
            re:
            var tuple = Tuple(r.Last, out r2);
            if (r2)
            {
                r &= r2;
                syn = new CallSyntax(syn, tuple!);
                SkipSpace(ref r);
                goto re;
            }
            var array = Array(r.Last, out r2);
            if (r2)
            {
                r &= r2;
                syn = new IndexSyntax(syn, array!);
                SkipSpace(ref r);
                goto re;
            }
        }

        result = r;
        return syn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Syntax? PrefixOpSyntax(Code code, out Result result)
    {
        var op = OpKind(code, out result);
        if (!result) return null;
        var right = NoOpSyntax(result.Last, out var r2);
        if (!r2) return null;
        var syn = new PrefixOpSyntax(result.Range.Offset, right!, op);
        result &= r2;
        return syn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TupleSyntax? Tuple(Code code, out Result result)
    {
        result = ParseOneChar(code, '(');
        if (!result) return null;
        var off = code.Offset;
        List<Syntax>? items = null;

        SkipSpace(ref result);

        re:
        var syn = Root(result.Last, int.MaxValue, out var r);
        if (r)
        {
            items ??= new();
            items.Add(syn!);
            result &= r;
        }

        SkipSpace(ref result);

        r = ParseOneChar(result.Last, ',');
        if (r)
        {
            result &= r;
            SkipSpace(ref result);
            goto re;
        }

        r = ParseOneChar(result.Last, ')');
        if (!r) throw new ParserException($"Tuple is not closed at {result.Last.Offset}");

        result &= r;
        return new TupleSyntax(off, items ?? new());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ArraySyntax? Array(Code code, out Result result)
    {
        result = ParseOneChar(code, '[');
        if (!result) return null;
        var off = code.Offset;
        List<Syntax>? items = null;

        SkipSpace(ref result);

        re:
        var syn = Root(result.Last, int.MaxValue, out var r);
        if (r)
        {
            items ??= new();
            items.Add(syn!);
            result &= r;
        }

        SkipSpace(ref result);

        r = ParseOneChar(result.Last, ',');
        if (r)
        {
            result &= r;
            SkipSpace(ref result);
            goto re;
        }

        r = ParseOneChar(result.Last, ']');
        if (!r) throw new ParserException($"Array is not closed at {result.Last.Offset}");

        result &= r;
        return new ArraySyntax(off, items ?? new());
    }

    private static readonly Regex IdRegex = GetIdRegex();
    private static readonly Regex IdBodyRegex = GetIdBodyRegex();

    [GeneratedRegex(@"[_\p{L}\p{Nl}][_\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*")]
    private static partial Regex GetIdRegex();

    [GeneratedRegex(@"[_\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]+")]
    private static partial Regex GetIdBodyRegex();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IdSyntax? Id(Code code, out Result result)
    {
        foreach (var match in IdRegex.EnumerateMatches(code.Str))
        {
            result = Result.Ok(code, match.Length);
            return new IdSyntax(code.Offset, code.Str[..match.Length].ToString());
        }
        result = Result.Failed();
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? Literal(Code code, out Result result)
    {
        var syn = StringLiteral(code, out result);
        if (result) return syn;
        syn = NullLiteral(code, out result);
        if (result) return syn;
        syn = BoolLiteral(code, out result);
        if (result) return syn;
        syn = IntLiteral(code, out result);
        if (result) return syn;
        // todo float
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? NullLiteral(Code code, out Result result)
    {
        result = ParseSubstr(code, "null");
        if (result) return new NullLiteralSyntax(code.Offset);
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? BoolLiteral(Code code, out Result result)
    {
        result = ParseSubstr(code, "true");
        if (result) return new BoolLiteralSyntax(code.Offset, true);
        result = ParseSubstr(code, "false");
        if (result) return new BoolLiteralSyntax(code.Offset, false);
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? IntLiteral(Code code, out Result result)
    {
        var syn = HexIntLiteral(code, out result);
        if (result) return syn;
        syn = BinaryIntLiteral(code, out result);
        if (result) return syn;
        syn = DecimalIntLiteral(code, out result);
        if (result) return syn;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? BinaryIntLiteral(Code code, out Result result)
    {
        if (code.IsEmpty || code.Length < 3 || code.Str is not ['0', 'b' or 'B', ..])
        {
            result = Result.Failed();
            return null;
        }
        var r = Result.Ok(code, 2);
        re:
        var r2 = DecoratedBinaryDigit(r.Last);
        if (r2)
        {
            r &= r2;
            goto re;
        }
        var intType = IntegerTypeSuffix(r.Last, out var r3);
        result = r & r3;
        return intType switch
        {
            IntType.Int => new IntLiteralSyntax(code.Offset, ParserIntLiteralValueBinary(code & r.Range)),
            IntType.UInt => new UIntLiteralSyntax(code.Offset, ParserUIntLiteralValueBinary(code & r.Range)),
            IntType.Long => new LongLiteralSyntax(code.Offset, ParserLongLiteralValueBinary(code & r.Range)),
            IntType.ULong => new ULongLiteralSyntax(code.Offset, ParserULongLiteralValueBinary(code & r.Range)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? HexIntLiteral(Code code, out Result result)
    {
        if (code.IsEmpty || code.Length < 3 || code.Str is not ['0', 'x' or 'X', ..])
        {
            result = Result.Failed();
            return null;
        }
        var r = Result.Ok(code, 2);
        re:
        var r2 = DecoratedHexDigit(r.Last);
        if (r2)
        {
            r &= r2;
            goto re;
        }
        var intType = IntegerTypeSuffix(r.Last, out var r3);
        result = r & r3;
        return intType switch
        {
            IntType.Int => new IntLiteralSyntax(code.Offset, ParserIntLiteralValueHex(code & r.Range)),
            IntType.UInt => new UIntLiteralSyntax(code.Offset, ParserUIntLiteralValueHex(code & r.Range)),
            IntType.Long => new LongLiteralSyntax(code.Offset, ParserLongLiteralValueHex(code & r.Range)),
            IntType.ULong => new ULongLiteralSyntax(code.Offset, ParserULongLiteralValueHex(code & r.Range)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? DecimalIntLiteral(Code code, out Result result)
    {
        var r = DecimalDigit(code);
        if (!r)
        {
            result = r;
            return null;
        }
        {
            re:
            var r2 = DecoratedDecimalDigit(r.Last);
            if (r2)
            {
                r &= r2;
                goto re;
            }
        }
        var intType = IntegerTypeSuffix(r.Last, out var r3);
        result = r & r3;
        return intType switch
        {
            IntType.Int => new IntLiteralSyntax(code.Offset, ParserIntLiteralValue(code & r.Range)),
            IntType.UInt => new UIntLiteralSyntax(code.Offset, ParserUIntLiteralValue(code & r.Range)),
            IntType.Long => new LongLiteralSyntax(code.Offset, ParserLongLiteralValue(code & r.Range)),
            IntType.ULong => new ULongLiteralSyntax(code.Offset, ParserULongLiteralValue(code & r.Range)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Result DecimalDigit(Code code) => ParserInRange(code, '0', '9');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Result DecoratedDecimalDigit(Code code)
    {
        var r = ParserChars(code, '_');
        return DecimalDigit(r ? r.Last : code);
    }

    private static readonly CharRange[] DecoratedHexDigitRanges = [new('0', '9'), new('a', 'f'), new('A', 'F')];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Result DecoratedHexDigit(Code code)
    {
        var r = ParserChars(code, '_');
        return ParserInRanges(r ? r.Last : code, DecoratedHexDigitRanges);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Result DecoratedBinaryDigit(Code code)
    {
        var r = ParserChars(code, '_');
        return ParserAnyChars(r ? r.Last : code, "01");
    }

    private enum IntType
    {
        Int,
        UInt,
        Long,
        ULong,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IntType IntegerTypeSuffix(Code code, out Result result)
    {
        if (code.Length >= 2)
        {
            if ((char.ToLower(code[0]) == 'u' && char.ToLower(code[1]) == 'l') ||
                (char.ToLower(code[0]) == 'l' && char.ToLower(code[1]) == 'u'))
            {
                result = Result.Ok(code, 2);
                return IntType.ULong;
            }
            goto len1;
        }
        else if (code.Length >= 1) goto len1;
        other:
        result = Result.Failed();
        return IntType.Int;
        len1:
        if (char.ToLower(code[0]) == 'u')
        {
            result = Result.Ok(code, 1);
            return IntType.UInt;
        }
        if (char.ToLower(code[0]) == 'l')
        {
            result = Result.Ok(code, 1);
            return IntType.Long;
        }
        goto other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Result Space(Code code) => ParserAnyChars(code, " \t\u00a0");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipSpace(ref Result result)
    {
        re:
        var r = Space(result.Last);
        if (r)
        {
            result &= r;
            goto re;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LiteralSyntax? StringLiteral(Code code, out Result result)
    {
        var syn = UnTypedStringLiteral(code, out result);
        if (!result) return null;
        var st = ParseStringType(result.Last, out var r);
        if (r)
        {
            result &= r;
            if (st is not StringType.Utf16) syn = syn! with { type = st };
        }
        return syn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StringType ParseStringType(Code code, out Result result)
    {
        if (code.IsEmpty) goto no;
        switch (code[0])
        {
            case 'c':
                if (code.Length >= 2)
                {
                    var s = code.Str[1..2];
                    if (IdBodyRegex.IsMatch(s)) goto no;
                }
                result = Result.Ok(code, 1);
                return StringType.Char;
            case 'u':
                if (code.Length >= 2)
                {
                    if (code[1] != '8') goto no;
                    if (code.Length >= 3)
                    {
                        var s = code.Str[2..3];
                        if (IdBodyRegex.IsMatch(s)) goto no;
                    }
                    result = Result.Ok(code, 2);
                    return StringType.Utf8;
                }
                break;
        }
        no:
        result = Result.Failed();
        return StringType.Utf16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StringLiteralSyntax? UnTypedStringLiteral(Code code, out Result result)
    {
        if (code.IsEmpty) goto failed;
        if (code[0] is not ('"' or '\'')) goto failed;
        if (code.Length < 2) throw new ParserException($"Unclosed string at {code.Offset}");
        var quote = code[0];
        if (code[1] == quote)
        {
            result = Result.Ok(code, 2);
            return new StringLiteralSyntax(code.Offset, string.Empty, StringType.Utf16);
        }
        result = Result.Ok(code, 1);
        Span<char> chars = stackalloc char[] { quote, '\\' };
        StringBuilder? sb = null;
        re:
        var i = FindEqAny(result.Last, chars);
        if (i < 0) throw new ParserException($"Unclosed string at {code.Offset}");
        var c = result.Last[i];
        if (c == quote)
        {
            var r = Result.Ok(result.Last, i + 1);
            sb?.Append(r.Range[..^1]);
            result &= r;
            return new StringLiteralSyntax(code.Offset, sb?.ToString() ?? result.Range[1..^1].ToString(),
                StringType.Utf16);
        }
        if (c == '\\')
        {
            var r = Result.Ok(result.Last, i);
            var last = r.Last;
            if (last.Length < 2) goto err_esc;
            sb ??= new();
            sb.Append(r.Range.ToString());
            var e = last[1];
            switch (e)
            {
                case '"' or '\'' or '\\':
                    sb.Append(e);
                    goto e1;
                case '0':
                    sb.Append('\0');
                    goto e1;
                case 'a':
                    sb.Append('\a');
                    goto e1;
                case 'b':
                    sb.Append('\b');
                    goto e1;
                case 'e':
                    sb.Append('\u001B');
                    goto e1;
                case 'f':
                    sb.Append('\f');
                    goto e1;
                case 'n':
                    sb.Append('\n');
                    goto e1;
                case 'r':
                    sb.Append('\r');
                    goto e1;
                case 't':
                    sb.Append('\t');
                    goto e1;
                case 'v':
                    sb.Append('\v');
                    goto e1;
                case 'u':
                    // if (last.Length < 3) goto err_esc;
                {
                    if (last.Length < 6) goto err_esc;
                    if (!ushort.TryParse(last.Str[2..6], NumberStyles.AllowHexSpecifier, null, out var ec))
                        goto err_esc;
                    sb.Append((char)ec);
                    result &= Result.Ok(r.Last, 6);
                    goto re;
                }
                case 'U':
                {
                    if (last.Length < 10) goto err_esc;
                    if (!uint.TryParse(last.Str[2..10], NumberStyles.AllowHexSpecifier, null, out var ec))
                        goto err_esc;
                    var rune = new Rune(ec);
                    Span<char> esc_chars = stackalloc char[2];
                    var esc_len = rune.EncodeToUtf16(esc_chars);
                    if (esc_len > 2) throw new UnreachableException();
                    sb.Append(esc_chars[..esc_len]);
                    result &= Result.Ok(r.Last, 10);
                    goto re;
                }
                default: goto err_esc;
            }
            e1:
            result &= Result.Ok(r.Last, 2);
            goto re;
            err_esc:
            throw new ParserException($"Illegal escape at {i}");
        }
        throw new UnreachableException();
        failed:
        result = Result.Failed();
        return null;
    }
}