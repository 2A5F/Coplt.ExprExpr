using Coplt.ExprExpr.Parsers;
using Coplt.ExprExpr.Syntaxes;

namespace Tests;

public class ParserTests
{
    [SetUp]
    public void Setup() { }

    #region TestInt

    [Test]
    public void TestInt1()
    {
        var s = Parser.Parse("123");
        Assert.That(s, Is.TypeOf<IntLiteralSyntax>());
        Assert.That(((IntLiteralSyntax)s).value, Is.EqualTo(123));
    }

    [Test]
    public void TestInt2()
    {
        var s = Parser.Parse("123_456");
        Assert.That(s, Is.TypeOf<IntLiteralSyntax>());
        Assert.That(((IntLiteralSyntax)s).value, Is.EqualTo(123456));
    }

    [Test]
    public void TestInt3()
    {
        var s = Parser.Parse("123_456UL");
        Assert.That(s, Is.TypeOf<ULongLiteralSyntax>());
        Assert.That(((ULongLiteralSyntax)s).value, Is.EqualTo(123456UL));
    }

    [Test]
    public void TestInt4()
    {
        var s = Parser.Parse("0x123af");
        Assert.That(s, Is.TypeOf<IntLiteralSyntax>());
        Assert.That(((IntLiteralSyntax)s).value, Is.EqualTo(0x123af));
    }

    [Test]
    public void TestInt5()
    {
        var s = Parser.Parse("0b0010_1001");
        Assert.That(s, Is.TypeOf<IntLiteralSyntax>());
        Assert.That(((IntLiteralSyntax)s).value, Is.EqualTo(0b0010_1001));
    }

    [Test]
    public void TestInt6()
    {
        var s = Parser.Parse("123_456u");
        Assert.That(s, Is.TypeOf<UIntLiteralSyntax>());
        Assert.That(((UIntLiteralSyntax)s).value, Is.EqualTo(123456u));
    }

    [Test]
    public void TestInt7()
    {
        var s = Parser.Parse("123_456l");
        Assert.That(s, Is.TypeOf<LongLiteralSyntax>());
        Assert.That(((LongLiteralSyntax)s).value, Is.EqualTo(123456L));
    }

    #endregion

    #region TestFloat

    [Test]
    public void TestFloat1()
    {
        var s = Parser.Parse("123.456");
        Assert.That(s, Is.TypeOf<DoubleLiteralSyntax>());
        Assert.That(((DoubleLiteralSyntax)s).value, Is.EqualTo(123.456));
    }

    [Test]
    public void TestFloat2()
    {
        var s = Parser.Parse("123.456f");
        Assert.That(s, Is.TypeOf<SingleLiteralSyntax>());
        Assert.That(((SingleLiteralSyntax)s).value, Is.EqualTo(123.456f));
    }

    [Test]
    public void TestFloat3()
    {
        var s = Parser.Parse("123.456m");
        Assert.That(s, Is.TypeOf<DecimalLiteralSyntax>());
        Assert.That(((DecimalLiteralSyntax)s).value, Is.EqualTo(123.456m));
    }

    [Test]
    public void TestFloat4()
    {
        var s = Parser.Parse("2_345e-2_0");
        Assert.That(s, Is.TypeOf<DoubleLiteralSyntax>());
        Assert.That(((DoubleLiteralSyntax)s).value, Is.EqualTo(2_345e-2_0));
    }

    [Test]
    public void TestFloat5()
    {
        var s = Parser.Parse(".123");
        Assert.That(s, Is.TypeOf<DoubleLiteralSyntax>());
        Assert.That(((DoubleLiteralSyntax)s).value, Is.EqualTo(.123));
    }

    [Test]
    public void TestFloat6()
    {
        var s = Parser.Parse("123.");
        Assert.That(s, Is.EqualTo(new SuffixOpSyntax(
            new IntLiteralSyntax(0, 123),
            OpKind.Path
        )));
    }

    #endregion

    #region TestNull

    [Test]
    public void TestNull1()
    {
        var s = Parser.Parse("null");
        Assert.That(s, Is.TypeOf<NullLiteralSyntax>());
    }

    #endregion

    #region TestBool

    [Test]
    public void TestBool1()
    {
        var s = Parser.Parse("true");
        Assert.That(s, Is.TypeOf<BoolLiteralSyntax>());
        Assert.That(((BoolLiteralSyntax)s).value, Is.True);
    }

    [Test]
    public void TestBool2()
    {
        var s = Parser.Parse("false");
        Assert.That(s, Is.TypeOf<BoolLiteralSyntax>());
        Assert.That(((BoolLiteralSyntax)s).value, Is.False);
    }

    #endregion

    #region TestOp

    [Test]
    public void TestOp1()
    {
        var s = Parser.Parse("(1 + 2!) * ~3");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<BinOpSyntax>());
        Assert.That(s, Is.EqualTo(
            new BinOpSyntax(
                new TupleSyntax(0, [
                    new BinOpSyntax(
                        new IntLiteralSyntax(1, 1),
                        new SuffixOpSyntax(new IntLiteralSyntax(5, 2), OpKind.BoolNot),
                        OpKind.Add
                    )
                ]),
                new PrefixOpSyntax(11, new IntLiteralSyntax(12, 3), OpKind.Not),
                OpKind.Mul
            )
        ));
    }

    [Test]
    public void TestCondOp1()
    {
        var s = Parser.Parse("1 ? 2 : 3");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<CondSyntax>());
        Assert.That(s, Is.EqualTo(
            new CondSyntax(
                new IntLiteralSyntax(0, 1),
                new IntLiteralSyntax(4, 2),
                new IntLiteralSyntax(8, 3)
            )
        ));
    }

    [Test]
    public void TestCondOp2()
    {
        var s = Parser.Parse("1 - 1 ? 2 + 2 : 3 * 3");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<CondSyntax>());
        Assert.That(s, Is.EqualTo(
            new CondSyntax(
                new BinOpSyntax(
                    new IntLiteralSyntax(0, 1),
                    new IntLiteralSyntax(4, 1),
                    OpKind.Sub
                ),
                new BinOpSyntax(
                    new IntLiteralSyntax(8, 2),
                    new IntLiteralSyntax(12, 2),
                    OpKind.Add
                ),
                new BinOpSyntax(
                    new IntLiteralSyntax(16, 3),
                    new IntLiteralSyntax(20, 3),
                    OpKind.Mul
                )
            )
        ));
    }

    #endregion

    #region TestId

    [Test]
    public void TestId1()
    {
        var s = Parser.Parse("asd");
        Assert.That(s, Is.TypeOf<IdSyntax>());
        Assert.That(((IdSyntax)s).id, Is.EqualTo("asd"));
    }

    [Test]
    public void TestId2()
    {
        var s = Parser.Parse("阿斯顿");
        Assert.That(s, Is.TypeOf<IdSyntax>());
        Assert.That(((IdSyntax)s).id, Is.EqualTo("阿斯顿"));
    }

    [Test]
    public void TestId3()
    {
        var s = Parser.Parse("_阿斯顿_123_asd_");
        Assert.That(s, Is.TypeOf<IdSyntax>());
        Assert.That(((IdSyntax)s).id, Is.EqualTo("_阿斯顿_123_asd_"));
    }

    #endregion

    #region TestCall

    [Test]
    public void TestCall1()
    {
        var s = Parser.Parse("sin(1)");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<CallSyntax>());
        Assert.That(s, Is.EqualTo(
            new CallSyntax(
                new IdSyntax(0, "sin"),
                new TupleSyntax(3, [new IntLiteralSyntax(4, 1)])
            )
        ));
    }

    #endregion

    #region TestIndex

    [Test]
    public void TestIndex1()
    {
        var s = Parser.Parse("arr[1]");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<IndexSyntax>());
        Assert.That(s, Is.EqualTo(
            new IndexSyntax(
                new IdSyntax(0, "arr"),
                new ArraySyntax(3, [new IntLiteralSyntax(4, 1)])
            )
        ));
    }

    #endregion

    #region TestString

    [Test]
    public void TestString1()
    {
        var s = Parser.Parse(@"""asd""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("asd"));
    }

    [Test]
    public void TestString2()
    {
        var s = Parser.Parse(@"""asd\n""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("asd\n"));
    }

    [Test]
    public void TestString3()
    {
        var s = Parser.Parse(@"""\nasd""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("\nasd"));
    }

    [Test]
    public void TestString4()
    {
        var s = Parser.Parse(@"""123\nasd""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("123\nasd"));
    }

    [Test]
    public void TestString5()
    {
        var s = Parser.Parse(@"""""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo(""));
    }

    [Test]
    public void TestString6()
    {
        var s = Parser.Parse(@"'\'\""\\\n\r\a\b\e\f\t\v\0'");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("\'\"\\\n\r\a\b\u001B\f\t\v\0"));
    }

    [Test]
    public void TestString7()
    {
        var s = Parser.Parse(@"""\u2A5F""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("\u2A5F"));
    }

    [Test]
    public void TestString8()
    {
        var s = Parser.Parse(@"""\U0001F602""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("😂"));
    }

    [Test]
    public void TestString9()
    {
        var s = Parser.Parse(@"""\uD83D\uDE02""");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("😂"));
    }

    [Test]
    public void TestString10()
    {
        var s = Parser.Parse(@"'a'c");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("a"));
        Assert.That(((StringLiteralSyntax)s).type, Is.EqualTo(StringType.Char));
    }

    [Test]
    public void TestString11()
    {
        var s = Parser.Parse(@"'asd'u8");
        Console.WriteLine(s);
        Assert.That(s, Is.TypeOf<StringLiteralSyntax>());
        Assert.That(((StringLiteralSyntax)s).value, Is.EqualTo("asd"));
        Assert.That(((StringLiteralSyntax)s).type, Is.EqualTo(StringType.Utf8));
    }

    #endregion
}
