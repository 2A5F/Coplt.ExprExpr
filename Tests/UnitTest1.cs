using Coplt.ExprExpr;
using Coplt.ExprExpr.Parsers;

namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup() { }

    #region TestFindNe

    [Test]
    [Parallelizable]
    public void TestFindNe1([Range(0, 1024)] int len)
    {
        var str = new char[len];
        var r = Parser.FindNe(str, '\0');
        Assert.That(r, Is.EqualTo(-1));
    }

    [Test]
    [Parallelizable]
    public void TestFindNe2([Range(1000, 1002)] int len, [Range(0, 130)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[at] = 'b';
        var r = Parser.FindNe(str, 'a');
        Assert.That(r, Is.EqualTo(at));
    }

    [Test]
    [Parallelizable]
    public void TestFindNe3([Range(1000, 1005)] int len, [Range(1, 10)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[len - at] = 'b';
        var r = Parser.FindNe(str, 'a');
        Assert.That(r, Is.EqualTo(len - at));
    }

    [Test]
    [Parallelizable]
    public void TestFindNe4([Range(0, 1024)] int len)
    {
        var str = new char[len];
        var r = Parser.FindNeIgnoreCase(str, '\0');
        Assert.That(r, Is.EqualTo(-1));
    }

    [Test]
    [Parallelizable]
    public void TestFindNe5([Range(1000, 1002)] int len, [Range(0, 130)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[at] = 'b';
        var r = Parser.FindNeIgnoreCase(str, 'A');
        Assert.That(r, Is.EqualTo(at));
    }

    [Test]
    [Parallelizable]
    public void TestFindNe6([Range(1000, 1005)] int len, [Range(1, 10)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[len - at] = 'b';
        var r = Parser.FindNeIgnoreCase(str, 'A');
        Assert.That(r, Is.EqualTo(len - at));
    }

    #endregion

    #region TestFindNotInRange

    [Test]
    [Parallelizable]
    public void TestFindNotInRange1([Range(0, 1024)] int len)
    {
        var str = new char[len];
        str.AsSpan().Fill('1');
        var r = Parser.FindNotInRange(str, '0', '9');
        Assert.That(r, Is.EqualTo(-1));
    }

    [Test]
    [Parallelizable]
    public void TestFindNotInRange2([Range(1000, 1002)] int len, [Range(0, 130)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[at] = '0';
        var r = Parser.FindNotInRange(str, 'a', 'z');
        Assert.That(r, Is.EqualTo(at));
    }

    [Test]
    [Parallelizable]
    public void TestFindNotInRange3([Range(1000, 1005)] int len, [Range(1, 10)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[len - at] = '0';
        var r = Parser.FindNotInRange(str, 'a', 'z');
        Assert.That(r, Is.EqualTo(len - at));
    }

    #endregion

    #region TestFindNotInRanges

    [Test]
    [Parallelizable]
    public void TestFindNotInRanges1([Range(0, 1024)] int len)
    {
        var str = new char[len];
        str.AsSpan().Fill('1');
        var r = Parser.FindNotInRanges(str, [new('0', '9'), new('a', 'f')]);
        Assert.That(r, Is.EqualTo(-1));
    }

    [Test]
    [Parallelizable]
    public void TestFindNotInRanges2([Range(1000, 1002)] int len, [Range(0, 130)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[at] = 'z';
        var r = Parser.FindNotInRanges(str, [new('0', '9'), new('a', 'f')]);
        Assert.That(r, Is.EqualTo(at));
    }

    [Test]
    [Parallelizable]
    public void TestFindNotInRanges3([Range(1000, 1005)] int len, [Range(1, 10)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[len - at] = 'z';
        var r = Parser.FindNotInRanges(str, [new('0', '9'), new('a', 'f')]);
        Assert.That(r, Is.EqualTo(len - at));
    }

    #endregion

    #region TestMathSubstr

    [Test]
    [Parallelizable]
    public void TestMathSubstr1([Range(0, 1024)] int len)
    {
        var a = new char[len];
        var b = new char[len];
        a.AsSpan().Fill('1');
        b.AsSpan().Fill('1');
        var r = Parser.HasSubStrAtStart(a, b);
        Assert.That(r, Is.True);
    }

    [Test]
    [Parallelizable]
    public void TestMathSubstr2([Range(128, 512)] int len, [Range(1, 3)] int offset)
    {
        var a = new char[len];
        var b = new char[len + offset];
        a.AsSpan().Fill('1');
        b.AsSpan().Fill('1');
        var r = Parser.HasSubStrAtStart(a, b);
        Assert.That(r, Is.False);
    }

    [Test]
    [Parallelizable]
    public void TestMathSubstr3([Range(128, 512)] int len, [Range(-1, -3)] int offset)
    {
        var a = new char[len];
        var b = new char[len + offset];
        a.AsSpan().Fill('1');
        b.AsSpan().Fill('1');
        var r = Parser.HasSubStrAtStart(a, b);
        Assert.That(r, Is.True);
    }

    [Test]
    [Parallelizable]
    public void TestMathSubstr4([Range(512, 1024)] int len)
    {
        var a = new char[len];
        var b = new char[len];
        a.AsSpan().Fill('1');
        b.AsSpan().Fill('2');
        var r = Parser.HasSubStrAtStart(a, b);
        Assert.That(r, Is.False);
    }

    [Test]
    [Parallelizable]
    public void TestMathSubstr5()
    {
        var a = Array.Empty<char>();
        var b = Array.Empty<char>();
        var r = Parser.HasSubStrAtStart(a, b);
        Assert.That(r, Is.True);
    }

    #endregion

    #region TestFindNeAny

    [Test]
    [Parallelizable]
    public void TestFindNeAny1([Range(0, 1024)] int len)
    {
        var str = new char[len];
        str.AsSpan().Fill('1');
        var r = Parser.FindNeAny(str, "12");
        Assert.That(r, Is.EqualTo(-1));
    }

    [Test]
    [Parallelizable]
    public void TestFindNeAny2([Range(1000, 1002)] int len, [Range(0, 130)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[at] = 'z';
        var r = Parser.FindNeAny(str, "abc");
        Assert.That(r, Is.EqualTo(at));
    }

    [Test]
    [Parallelizable]
    public void TestFindNeAny3([Range(1000, 1005)] int len, [Range(1, 10)] int at)
    {
        var str = new char[len];
        str.AsSpan().Fill('a');
        str[len - at] = 'z';
        var r = Parser.FindNeAny(str, "abc");
        Assert.That(r, Is.EqualTo(len - at));
    }

    #endregion

    #region FastBinaryToInt

    [Test]
    public static void TestFastBinaryToInt1()
    {
        var r = Parser.FastBinaryToInt("10001000001000100001000000010010", true);
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b10001000001000100001000000010010));
    }

    [Test]
    public static void TestFastBinaryToInt2()
    {
        var r = Parser.FastBinaryToInt("1000100000100010000100000001001010001000001000100001000000010010", true);
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b1000100000100010000100000001001010001000001000100001000000010010));
    }

    [Test]
    public static void TestFastBinaryToInt3()
    {
        var r = Parser.FastBinaryToInt(
            "100010000010001000010000000100101000100000100010000100000001001010001000001000100001000000010010", true);
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b1000100000100010000100000001001010001000001000100001000000010010));
    }

    [Test]
    public static void TestFastBinaryToInt4()
    {
        var r = Parser.FastBinaryToInt("01000100000100010000100", true);
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b01000100000100010000100));
    }

    [Test]
    public static void TestFastBinaryToInt5()
    {
        var str = new char[64];
        str.AsSpan().Fill('0');
        const string t = "1000_1000_1000_1000_1000_1000_1000_1000";
        t.AsSpan().CopyTo(str.AsSpan()[^t.Length..]);
        var r = Parser.FastBinaryToInt(str);
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b1000_1000_1000_1000_1000_1000_1000_1000));
    }

    [Test]
    public static void TestFastBinaryToInt6()
    {
        var str = new char[32 + 16];
        str.AsSpan().Fill('0');
        const string t = "1000_1000_1000_1000_1000_1000_1000_1000";
        t.AsSpan().CopyTo(str.AsSpan()[^t.Length..]);
        var r = Parser.FastBinaryToInt(str);
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b1000_1000_1000_1000_1000_1000_1000_1000));
    }

    [Test]
    public static void TestFastBinaryToInt7()
    {
        var str = new char[32 + 8];
        str.AsSpan().Fill('0');
        const string t = "1000_1000_1000_1000_1000_1000_1000_1000";
        t.AsSpan().CopyTo(str.AsSpan()[^t.Length..]);
        var r = Parser.FastBinaryToInt(str);
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b1000_1000_1000_1000_1000_1000_1000_1000));
    }

    [Test]
    public static void TestFastBinaryToInt8()
    {
        var r = Parser.FastBinaryToInt("1000_1000_1000_1000_1000_1000_1000_1000");
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b1000_1000_1000_1000_1000_1000_1000_1000));
    }

    [Test]
    public static void TestFastBinaryToInt9()
    {
        var r = Parser.FastBinaryToInt("1000________1000");
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b1000________1000));
    }

    [Test]
    public static void TestFastBinaryToInt10()
    {
        var r = Parser.FastBinaryToInt("____1000___1000_1111");
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0b____1000___1000_1111));
    }

    [Test]
    public static void TestFastBinaryToInt11()
    {
        var r = Parser.FastBinaryToInt("_");
        Console.WriteLine(r.ToString("B"));
        Assert.That(r, Is.EqualTo(0));
    }

    #endregion
}
