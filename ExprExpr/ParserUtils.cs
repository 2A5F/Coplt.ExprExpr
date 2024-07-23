using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Coplt.ExprExpr.Parsers;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
internal readonly ref struct Code(Str str, int offset)
{
    public readonly Str Str = str;
    public readonly int Offset = offset;

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Str.Length == 0;
    }

    public bool IsNotEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Str.Length != 0;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Str.Length;
    }

    public ref readonly char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Str[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Code(string str) => new(str, 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Code(Str str) => new(str, 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Str(Code code) => code.Str;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => Str.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Code Slice(int start) => new(Str.Slice(start), Offset + start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Code Slice(int start, int length) => new(Str.Slice(start, length), Offset + start);

    /// <summary>
    /// Merge Code
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Code operator &(Code a, Code b)
    {
        if (a.Offset > b.Offset) return b & a;
        var s = new Str(Unsafe.AsPointer(ref Unsafe.AsRef(in a[0])), (b.Offset - a.Offset + b.Length));
        return new(s, a.Offset);
    }

    /// <summary>
    /// Get Latest Last
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Code operator |(Code a, Code b)
    {
        return a.Offset > b.Offset ? a : b;
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
internal readonly ref struct Result(bool success, Code range, Code last)
{
    public readonly bool Success = success;
    public readonly Code Range = range;
    public readonly Code Last = last;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Failed() => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Ok(Code str, int len) => new(true, str[..len], str[len..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(Result r) => r.Success;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(Result r) => !r.Success;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !(Result r) => !r.Success;

    /// <summary>
    /// Merge Result
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result operator &(Result a, Result b)
    {
        if (!a.Success) return b;
        if (!b.Success) return a;
        return new(true, a.Range & b.Range, a.Last | b.Last);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => $"Code {{ Success = {Success}, Range = {Range}, Last = {Last} }}";
}

public readonly record struct CharRange
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CharRange(char start, char end)
    {
        if (end < start) (start, end) = (end, start);
        this.start = start;
        this.end = end;
    }

    public char start
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        init;
    }
    public char end
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        init;
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out char start, out char end)
    {
        start = this.start;
        end = this.end;
    }
}

public partial class Parser
{
    #region Utils

    [MethodImpl(256 | 512)]
    private static ref ushort AsU16(Str str) => ref Unsafe.As<char, ushort>(ref Unsafe.AsRef(in str[0]));

    /// <summary>
    /// Find unequal character positions
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int FindNe(Str str, char c)
    {
        var last = str;
        var off = 0;
        re:
        if (Vector512.IsHardwareAccelerated)
        {
            if (last.Length >= 32)
            {
                var v = Vector512.LoadUnsafe(ref AsU16(last));
                var r = ~Vector512.Equals(v, Vector512.Create((ushort)c));
                var s = r.ExtractMostSignificantBits();
                var l = ulong.TrailingZeroCount(s);
                if (l < 32) return off + (int)l;
                off += 32;
                last = last[32..];
                goto re;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            if (last.Length >= 16)
            {
                var v = Vector256.LoadUnsafe(ref AsU16(last));
                var r = ~Vector256.Equals(v, Vector256.Create((ushort)c));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 16) return off + (int)l;
                off += 16;
                last = last[16..];
                goto re;
            }
        }
        if (Vector128.IsHardwareAccelerated)
        {
            if (last.Length >= 8)
            {
                var v = Vector128.LoadUnsafe(ref AsU16(last));
                var r = ~Vector128.Equals(v, Vector128.Create((ushort)c));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 8) return off + (int)l;
                off += 8;
                last = last[8..];
                goto re;
            }
        }
        if (Vector64.IsHardwareAccelerated)
        {
            if (last.Length >= 4)
            {
                var v = Vector64.LoadUnsafe(ref AsU16(last));
                var r = ~Vector64.Equals(v, Vector64.Create((ushort)c));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 4) return off + (int)l;
                off += 4;
                last = last[4..];
                goto re;
            }
        }
        for (var i = 0; i < last.Length; i++)
        {
            if (last[i] != c)
            {
                return off + i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Find unequal character positions
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int FindNeIgnoreCase(Str str, char c)
    {
        var last = str;
        var off = 0;
        var a = (ushort)char.ToLower(c);
        var b = (ushort)char.ToUpper(c);
        re:
        if (Vector512.IsHardwareAccelerated)
        {
            if (last.Length >= 32)
            {
                var v = Vector512.LoadUnsafe(ref AsU16(last));
                var r = ~(Vector512.Equals(v, Vector512.Create(a)) | Vector512.Equals(v, Vector512.Create(b)));
                var s = r.ExtractMostSignificantBits();
                var l = ulong.TrailingZeroCount(s);
                if (l < 32) return off + (int)l;
                off += 32;
                last = last[32..];
                goto re;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            if (last.Length >= 16)
            {
                var v = Vector256.LoadUnsafe(ref AsU16(last));
                var r = ~(Vector256.Equals(v, Vector256.Create(a)) | Vector256.Equals(v, Vector256.Create(b)));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 16) return off + (int)l;
                off += 16;
                last = last[16..];
                goto re;
            }
        }
        if (Vector128.IsHardwareAccelerated)
        {
            if (last.Length >= 8)
            {
                var v = Vector128.LoadUnsafe(ref AsU16(last));
                var r = ~(Vector128.Equals(v, Vector128.Create(a)) | Vector128.Equals(v, Vector128.Create(b)));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 8) return off + (int)l;
                off += 8;
                last = last[8..];
                goto re;
            }
        }
        if (Vector64.IsHardwareAccelerated)
        {
            if (last.Length >= 4)
            {
                var v = Vector64.LoadUnsafe(ref AsU16(last));
                var r = ~(Vector64.Equals(v, Vector64.Create(a)) | Vector64.Equals(v, Vector64.Create(b)));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 4) return off + (int)l;
                off += 4;
                last = last[4..];
                goto re;
            }
        }
        for (var i = 0; i < last.Length; i++)
        {
            var v = last[i];
            if (v != a && v != b)
            {
                return off + i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Find unequal character positions
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int FindNeAny(Str str, Str chars)
    {
        if (chars.Length == 0) return -1;
        var last = str;
        var off = 0;
        re:
        if (Vector512.IsHardwareAccelerated)
        {
            if (last.Length >= 32)
            {
                var v = Vector512.LoadUnsafe(ref AsU16(last));
                var t = Vector512.Equals(v, Vector512.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector512.Equals(v, Vector512.Create((ushort)chars[i]));
                }
                var r = ~t;
                var s = r.ExtractMostSignificantBits();
                var l = ulong.TrailingZeroCount(s);
                if (l < 32) return off + (int)l;
                off += 32;
                last = last[32..];
                goto re;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            if (last.Length >= 16)
            {
                var v = Vector256.LoadUnsafe(ref AsU16(last));
                var t = Vector256.Equals(v, Vector256.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector256.Equals(v, Vector256.Create((ushort)chars[i]));
                }
                var r = ~t;
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 16) return off + (int)l;
                off += 16;
                last = last[16..];
                goto re;
            }
        }
        if (Vector128.IsHardwareAccelerated)
        {
            if (last.Length >= 8)
            {
                var v = Vector128.LoadUnsafe(ref AsU16(last));
                var t = Vector128.Equals(v, Vector128.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector128.Equals(v, Vector128.Create((ushort)chars[i]));
                }
                var r = ~t;
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 8) return off + (int)l;
                off += 8;
                last = last[8..];
                goto re;
            }
        }
        if (Vector64.IsHardwareAccelerated)
        {
            if (last.Length >= 4)
            {
                var v = Vector64.LoadUnsafe(ref AsU16(last));
                var t = Vector64.Equals(v, Vector64.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector64.Equals(v, Vector64.Create((ushort)chars[i]));
                }
                var r = ~t;
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 4) return off + (int)l;
                off += 4;
                last = last[4..];
                goto re;
            }
        }
        for (var i = 0; i < last.Length; i++)
        {
            foreach (var t in chars)
            {
                if (last[i] == t) goto next;
            }
            return off + i;
            next: ;
        }
        return -1;
    }

    /// <summary>
    /// Find equal character positions
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int FindEqAny(Str str, Str chars)
    {
        if (chars.Length == 0) return -1;
        var last = str;
        var off = 0;
        re:
        if (Vector512.IsHardwareAccelerated)
        {
            if (last.Length >= 32)
            {
                var v = Vector512.LoadUnsafe(ref AsU16(last));
                var t = Vector512.Equals(v, Vector512.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector512.Equals(v, Vector512.Create((ushort)chars[i]));
                }
                var r = t;
                var s = r.ExtractMostSignificantBits();
                var l = ulong.TrailingZeroCount(s);
                if (l < 32) return off + (int)l;
                off += 32;
                last = last[32..];
                goto re;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            if (last.Length >= 16)
            {
                var v = Vector256.LoadUnsafe(ref AsU16(last));
                var t = Vector256.Equals(v, Vector256.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector256.Equals(v, Vector256.Create((ushort)chars[i]));
                }
                var r = t;
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 16) return off + (int)l;
                off += 16;
                last = last[16..];
                goto re;
            }
        }
        if (Vector128.IsHardwareAccelerated)
        {
            if (last.Length >= 8)
            {
                var v = Vector128.LoadUnsafe(ref AsU16(last));
                var t = Vector128.Equals(v, Vector128.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector128.Equals(v, Vector128.Create((ushort)chars[i]));
                }
                var r = t;
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 8) return off + (int)l;
                off += 8;
                last = last[8..];
                goto re;
            }
        }
        if (Vector64.IsHardwareAccelerated)
        {
            if (last.Length >= 4)
            {
                var v = Vector64.LoadUnsafe(ref AsU16(last));
                var t = Vector64.Equals(v, Vector64.Create((ushort)chars[0]));
                for (var i = 1; i < chars.Length; i++)
                {
                    t |= Vector64.Equals(v, Vector64.Create((ushort)chars[i]));
                }
                var r = t;
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 4) return off + (int)l;
                off += 4;
                last = last[4..];
                goto re;
            }
        }
        for (var i = 0; i < last.Length; i++)
        {
            foreach (var t in chars)
            {
                if (last[i] == t) return off + i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Find the position of a character that is not in the range
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int FindNotInRange(Str str, char start, char end)
    {
        if (end < start) (start, end) = (end, start);
        var last = str;
        var off = 0;
        re:
        if (Vector512.IsHardwareAccelerated)
        {
            if (last.Length >= 32)
            {
                var v = Vector512.LoadUnsafe(ref AsU16(last));
                var r = Vector512.LessThan(v, Vector512.Create((ushort)start)) |
                        Vector512.GreaterThan(v, Vector512.Create((ushort)end));
                var s = r.ExtractMostSignificantBits();
                var l = ulong.TrailingZeroCount(s);
                if (l < 32) return off + (int)l;
                off += 32;
                last = last[32..];
                goto re;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            if (last.Length >= 16)
            {
                var v = Vector256.LoadUnsafe(ref AsU16(last));
                var r = Vector256.LessThan(v, Vector256.Create((ushort)start)) |
                        Vector256.GreaterThan(v, Vector256.Create((ushort)end));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 16) return off + (int)l;
                off += 16;
                last = last[16..];
                goto re;
            }
        }
        if (Vector128.IsHardwareAccelerated)
        {
            if (last.Length >= 8)
            {
                var v = Vector128.LoadUnsafe(ref AsU16(last));
                var r = Vector128.LessThan(v, Vector128.Create((ushort)start)) |
                        Vector128.GreaterThan(v, Vector128.Create((ushort)end));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 8) return off + (int)l;
                off += 8;
                last = last[8..];
                goto re;
            }
        }
        if (Vector64.IsHardwareAccelerated)
        {
            if (last.Length >= 4)
            {
                var v = Vector64.LoadUnsafe(ref AsU16(last));
                var r = Vector64.LessThan(v, Vector64.Create((ushort)start)) |
                        Vector64.GreaterThan(v, Vector64.Create((ushort)end));
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 4) return off + (int)l;
                off += 4;
                last = last[4..];
                goto re;
            }
        }
        for (var i = 0; i < last.Length; i++)
        {
            var v = last[i];
            if (v < start || v > end)
            {
                return off + i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Find the position of a character that is not in the range
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int FindNotInRanges(Str str, ReadOnlySpan<CharRange> ranges)
    {
        if (ranges.Length == 0) return -1;
        var last = str;
        var off = 0;
        re:
        if (Vector512.IsHardwareAccelerated)
        {
            if (last.Length >= 32)
            {
                var v = Vector512.LoadUnsafe(ref AsU16(last));
                var r = Vector512.LessThan(v, Vector512.Create((ushort)ranges[0].start)) |
                        Vector512.GreaterThan(v, Vector512.Create((ushort)ranges[0].end));
                for (var i = 1; i < ranges.Length; i++)
                {
                    r &= Vector512.LessThan(v, Vector512.Create((ushort)ranges[i].start)) |
                         Vector512.GreaterThan(v, Vector512.Create((ushort)ranges[i].end));
                }
                var s = r.ExtractMostSignificantBits();
                var l = ulong.TrailingZeroCount(s);
                if (l < 32) return off + (int)l;
                off += 32;
                last = last[32..];
                goto re;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            if (last.Length >= 16)
            {
                var v = Vector256.LoadUnsafe(ref AsU16(last));
                var r = Vector256.LessThan(v, Vector256.Create((ushort)ranges[0].start)) |
                        Vector256.GreaterThan(v, Vector256.Create((ushort)ranges[0].end));
                for (var i = 1; i < ranges.Length; i++)
                {
                    r &= Vector256.LessThan(v, Vector256.Create((ushort)ranges[i].start)) |
                         Vector256.GreaterThan(v, Vector256.Create((ushort)ranges[i].end));
                }
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 16) return off + (int)l;
                off += 16;
                last = last[16..];
                goto re;
            }
        }
        if (Vector128.IsHardwareAccelerated)
        {
            if (last.Length >= 8)
            {
                var v = Vector128.LoadUnsafe(ref AsU16(last));
                var r = Vector128.LessThan(v, Vector128.Create((ushort)ranges[0].start)) |
                        Vector128.GreaterThan(v, Vector128.Create((ushort)ranges[0].end));
                for (var i = 1; i < ranges.Length; i++)
                {
                    r &= Vector128.LessThan(v, Vector128.Create((ushort)ranges[i].start)) |
                         Vector128.GreaterThan(v, Vector128.Create((ushort)ranges[i].end));
                }
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 8) return off + (int)l;
                off += 8;
                last = last[8..];
                goto re;
            }
        }
        if (Vector64.IsHardwareAccelerated)
        {
            if (last.Length >= 4)
            {
                var v = Vector64.LoadUnsafe(ref AsU16(last));
                var r = Vector64.LessThan(v, Vector64.Create((ushort)ranges[0].start)) |
                        Vector64.GreaterThan(v, Vector64.Create((ushort)ranges[0].end));
                for (var i = 1; i < ranges.Length; i++)
                {
                    r &= Vector64.LessThan(v, Vector64.Create((ushort)ranges[i].start)) |
                         Vector64.GreaterThan(v, Vector64.Create((ushort)ranges[i].end));
                }
                var s = r.ExtractMostSignificantBits();
                var l = uint.TrailingZeroCount(s);
                if (l < 4) return off + (int)l;
                off += 4;
                last = last[4..];
                goto re;
            }
        }
        for (var i = 0; i < last.Length; i++)
        {
            var v = last[i];
            foreach (var range in ranges)
            {
                if (v >= range.start && v <= range.end) goto next;
            }
            return off + i;
            next: ;
        }
        return -1;
    }

    /// <summary>
    /// Determines whether the string has the substring at the start
    /// </summary>
    [MethodImpl(256 | 512)]
    public static bool HasSubStrAtStart(Str str, Str sub)
    {
        var last_str = str;
        var last_sub = sub;
        re:
        if (last_str.Length < last_sub.Length) return false;
        if (Vector512.IsHardwareAccelerated)
        {
            if (last_sub.Length >= 32)
            {
                var a = Vector512.LoadUnsafe(ref AsU16(last_sub));
                var b = Vector512.LoadUnsafe(ref AsU16(last_str));
                if (a != b) return false;
                last_str = last_str[32..];
                last_sub = last_sub[32..];
                goto re;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            if (last_sub.Length >= 16)
            {
                var a = Vector256.LoadUnsafe(ref AsU16(last_sub));
                var b = Vector256.LoadUnsafe(ref AsU16(last_str));
                if (a != b) return false;
                last_str = last_str[16..];
                last_sub = last_sub[16..];
                goto re;
            }
        }
        if (Vector128.IsHardwareAccelerated)
        {
            if (last_sub.Length >= 8)
            {
                var a = Vector128.LoadUnsafe(ref AsU16(last_sub));
                var b = Vector128.LoadUnsafe(ref AsU16(last_str));
                if (a != b) return false;
                last_str = last_str[8..];
                last_sub = last_sub[8..];
                goto re;
            }
        }
        if (Vector64.IsHardwareAccelerated)
        {
            if (last_sub.Length >= 4)
            {
                var a = Vector64.LoadUnsafe(ref AsU16(last_sub));
                var b = Vector64.LoadUnsafe(ref AsU16(last_str));
                if (a != b) return false;
                last_str = last_str[4..];
                last_sub = last_sub[4..];
                goto re;
            }
        }
        for (var i = 0; i < last_sub.Length; i++)
        {
            if (last_str[i] != last_sub[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// <c>&lt;c&gt;+</c>
    /// </summary>
    [MethodImpl(256 | 512)]
    private static Result ParserChars(Code code, char c)
    {
        if (code.IsEmpty) return Result.Failed();
        var i = FindNe(code, c);
        if (i == 0) return Result.Failed();
        if (i < 0) return Result.Ok(code, code.Length);
        return Result.Ok(code, i);
    }

    /// <summary>
    /// <c>[&lt;chars&gt;]+</c>
    /// </summary>
    [MethodImpl(256 | 512)]
    private static Result ParserAnyChars(Code code, Str chars)
    {
        if (code.IsEmpty) return Result.Failed();
        var i = FindNeAny(code, chars);
        if (i == 0) return Result.Failed();
        if (i < 0) return Result.Ok(code, code.Length);
        return Result.Ok(code, i);
    }

    /// <summary>
    /// <c>[&lt;start&gt;-&lt;end&gt;]+</c>
    /// </summary>
    [MethodImpl(256 | 512)]
    private static Result ParserInRange(Code code, char start, char end)
    {
        if (code.IsEmpty) return Result.Failed();
        var i = FindNotInRange(code, start, end);
        if (i == 0) return Result.Failed();
        if (i < 0) return Result.Ok(code, code.Length);
        return Result.Ok(code, i);
    }

    /// <summary>
    /// <c>[&lt;start1&gt;-&lt;end1&gt;&lt;start2&gt;-&lt;end2&gt;...]+</c>
    /// </summary>
    [MethodImpl(256 | 512)]
    private static Result ParserInRanges(Code code, ReadOnlySpan<CharRange> ranges)
    {
        if (code.IsEmpty) return Result.Failed();
        var i = FindNotInRanges(code, ranges);
        if (i == 0) return Result.Failed();
        if (i < 0) return Result.Ok(code, code.Length);
        return Result.Ok(code, i);
    }

    /// <summary>
    /// Match the substr
    /// </summary>
    [MethodImpl(256 | 512)]
    private static Result ParseSubstr(Code code, Str sub)
    {
        if (sub.IsEmpty) return Result.Ok(code, 0);
        if (!HasSubStrAtStart(code, sub)) return Result.Failed();
        return Result.Ok(code, sub.Length);
    }

    /// <summary>
    /// Match the char
    /// </summary>
    [MethodImpl(256 | 512)]
    private static Result ParseOneChar(Code code, char c)
    {
        if (code.IsEmpty) return Result.Failed();
        if (code[0] == c) return Result.Ok(code, 1);
        return Result.Failed();
    }

    #endregion

    #region FastBinaryToInt

    /// <summary>
    /// Fast binary string to integer conversion accelerated by simd
    /// </summary>
    /// <param name="str">The input string must match <c>[01_]*</c></param>
    /// <param name="no_underline">No underscore is faster</param>
    [MethodImpl(256 | 512)]
    public static ulong FastBinaryToInt(Str str, bool no_underline = false)
    {
        if (str.Length == 0) return 0;
        ulong r = 0;
        var last = str;
        {
            var off_i = 0;
            re:
            if (Vector512.IsHardwareAccelerated)
            {
                if (last.Length >= 32)
                {
                    var a = Vector512.LoadUnsafe(ref AsU16(last[^32..]));
                    a = Vector512.Shuffle(a, Vector512.Create(
                        (ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
                        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0
                    ));
                    {
                        var ev = Vector512.Equals(a, Vector512.Create((ushort)'1'));
                        var sv = ev.ExtractMostSignificantBits();
                        var n = 0;

                        if (!no_underline)
                        {
                            var eu = Vector512.Equals(a, Vector512.Create((ushort)'_'));
                            var su = eu.ExtractMostSignificantBits();
                            for (; su > 0; su = ((su - 1) & su) >> 1)
                            {
                                n++;
                                var i = (int)ulong.TrailingZeroCount(su);
                                var mask = ulong.MaxValue << i;
                                sv = ((sv & mask) >> 1) | (sv & ~mask);
                            }
                        }

                        r |= sv << off_i;
                        off_i += 32 - n;
                    }
                    last = last[..^32];
                    goto re;
                }
            }
            if (Vector256.IsHardwareAccelerated)
            {
                if (last.Length >= 16)
                {
                    var a = Vector256.LoadUnsafe(ref AsU16(last[^16..]));
                    a = Vector256.Shuffle(a, Vector256.Create(
                        (ushort)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0
                    ));
                    {
                        var ev = Vector256.Equals(a, Vector256.Create((ushort)'1'));
                        var sv = (ulong)ev.ExtractMostSignificantBits();
                        var n = 0;

                        if (!no_underline)
                        {
                            var eu = Vector256.Equals(a, Vector256.Create((ushort)'_'));
                            var su = eu.ExtractMostSignificantBits();
                            for (; su > 0; su = ((su - 1) & su) >> 1)
                            {
                                n++;
                                var i = (int)ulong.TrailingZeroCount(su);
                                var mask = ulong.MaxValue << i;
                                sv = ((sv & mask) >> 1) | (sv & ~mask);
                            }
                        }

                        r |= sv << off_i;
                        off_i += 16 - n;
                    }
                    last = last[..^16];
                    goto re;
                }
            }
            if (Vector128.IsHardwareAccelerated)
            {
                if (last.Length >= 8)
                {
                    var a = Vector128.LoadUnsafe(ref AsU16(last[^8..]));
                    a = Vector128.Shuffle(a, Vector128.Create(
                        (ushort)7, 6, 5, 4, 3, 2, 1, 0
                    ));
                    {
                        var ev = Vector128.Equals(a, Vector128.Create((ushort)'1'));
                        var sv = (ulong)ev.ExtractMostSignificantBits();
                        var n = 0;

                        if (!no_underline)
                        {
                            var eu = Vector128.Equals(a, Vector128.Create((ushort)'_'));
                            var su = eu.ExtractMostSignificantBits();
                            for (; su > 0; su = ((su - 1) & su) >> 1)
                            {
                                n++;
                                var i = (int)ulong.TrailingZeroCount(su);
                                var mask = ulong.MaxValue << i;
                                sv = ((sv & mask) >> 1) | (sv & ~mask);
                            }
                        }

                        r |= sv << off_i;
                        off_i += 8 - n;
                    }
                    last = last[..^8];
                    goto re;
                }
            }
            if (Vector64.IsHardwareAccelerated)
            {
                if (last.Length >= 4)
                {
                    var a = Vector64.LoadUnsafe(ref AsU16(last[^4..]));
                    a = Vector64.Shuffle(a, Vector64.Create(
                        (ushort)3, 2, 1, 0
                    ));
                    {
                        var ev = Vector64.Equals(a, Vector64.Create((ushort)'1'));
                        var sv = (ulong)ev.ExtractMostSignificantBits();
                        var n = 0;

                        if (!no_underline)
                        {
                            var eu = Vector64.Equals(a, Vector64.Create((ushort)'_'));
                            var su = eu.ExtractMostSignificantBits();
                            for (; su > 0; su = ((su - 1) & su) >> 1)
                            {
                                n++;
                                var i = (int)ulong.TrailingZeroCount(su);
                                var mask = ulong.MaxValue << i;
                                sv = ((sv & mask) >> 1) | (sv & ~mask);
                            }
                        }

                        r |= sv << off_i;
                        off_i += 4 - n;
                    }
                    last = last[..^4];
                    goto re;
                }
            }
            var end = last.Length - 1;
            for (var c = end; c >= 0; c--)
            {
                var t = last[c];
                if (t == '_') continue;
                if (t == '1')
                {
                    r |= 1ul << off_i;
                }
                off_i++;
            }
        }
        return r;
    }

    #endregion

    #region ParserInt

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int ParserIntLiteralValue(Str str)
    {
        var v = 0;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is < '0' or > '9') goto err;
            var a = c - '0';
            v *= 10;
            v += a;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static uint ParserUIntLiteralValue(Str str)
    {
        var v = 0u;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is < '0' or > '9') goto err;
            var a = c - '0';
            v *= 10;
            v += (uint)a;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static long ParserLongLiteralValue(Str str)
    {
        var v = 0L;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is < '0' or > '9') goto err;
            var a = c - '0';
            v *= 10;
            v += a;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static ulong ParserULongLiteralValue(Str str)
    {
        var v = 0UL;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is < '0' or > '9') goto err;
            var a = c - '0';
            v *= 10;
            v += (ulong)a;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    #endregion

    #region ParserIntHex

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int ParserIntLiteralValueHex(Str str)
    {
        if (str is not ['0', 'x' or 'X', ..]) goto err;
        str = str[2..];
        var v = 0;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is >= '0' and <= '9')
            {
                var a = c - '0';
                v <<= 4;
                v |= a;
                continue;
            }
            if (c is >= 'a' and <= 'f')
            {
                var a = c - 'a' + 10;
                v <<= 4;
                v |= a;
                continue;
            }
            if (c is >= 'A' and <= 'F')
            {
                var a = c - 'A' + 10;
                v <<= 4;
                v |= a;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static uint ParserUIntLiteralValueHex(Str str)
    {
        if (str is not ['0', 'x' or 'X', ..]) goto err;
        str = str[2..];
        var v = 0u;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is >= '0' and <= '9')
            {
                var a = c - '0';
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            if (c is >= 'a' and <= 'f')
            {
                var a = c - 'a' + 10;
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            if (c is >= 'A' and <= 'F')
            {
                var a = c - 'A' + 10;
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static long ParserLongLiteralValueHex(Str str)
    {
        if (str is not ['0', 'x' or 'X', ..]) goto err;
        str = str[2..];
        var v = 0L;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is >= '0' and <= '9')
            {
                var a = c - '0';
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            if (c is >= 'a' and <= 'f')
            {
                var a = c - 'a' + 10;
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            if (c is >= 'A' and <= 'F')
            {
                var a = c - 'A' + 10;
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static ulong ParserULongLiteralValueHex(Str str)
    {
        if (str is not ['0', 'x' or 'X', ..]) goto err;
        str = str[2..];
        var v = 0UL;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is >= '0' and <= '9')
            {
                var a = c - '0';
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            if (c is >= 'a' and <= 'f')
            {
                var a = c - 'a' + 10;
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            if (c is >= 'A' and <= 'F')
            {
                var a = c - 'A' + 10;
                v <<= 4;
                v |= (uint)a;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    #endregion

    #region ParserIntBinary

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static int ParserIntLiteralValueBinary(Str str)
    {
        if (str is not ['0', 'b' or 'B', ..]) goto err;
        str = str[2..];
        var v = 0;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is '0')
            {
                v <<= 1;
                continue;
            }
            else if (c is '1')
            {
                v <<= 1;
                v |= 1;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static uint ParserUIntLiteralValueBinary(Str str)
    {
        if (str is not ['0', 'b' or 'B', ..]) goto err;
        str = str[2..];
        var v = 0u;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is '0')
            {
                v <<= 1;
                continue;
            }
            else if (c is '1')
            {
                v <<= 1;
                v |= 1;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static long ParserLongLiteralValueBinary(Str str)
    {
        if (str is not ['0', 'b' or 'B', ..]) goto err;
        str = str[2..];
        var v = 0L;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is '0')
            {
                v <<= 1;
                continue;
            }
            else if (c is '1')
            {
                v <<= 1;
                v |= 1;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    /// <summary>
    /// Parsing int literal value
    /// </summary>
    [MethodImpl(256 | 512)]
    public static ulong ParserULongLiteralValueBinary(Str str)
    {
        if (str is not ['0', 'b' or 'B', ..]) goto err;
        str = str[2..];
        var v = 0UL;
        foreach (var c in str)
        {
            if (c is '_') continue;
            if (c is '0')
            {
                v <<= 1;
                continue;
            }
            else if (c is '1')
            {
                v <<= 1;
                v |= 1;
                continue;
            }
            goto err;
        }
        return v;
        err:
        throw new ParserException($"Failed to parse integer literal: \"{str.ToString()}\"");
    }

    #endregion
}
