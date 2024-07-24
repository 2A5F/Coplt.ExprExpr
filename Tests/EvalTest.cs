﻿using Coplt.ExprExpr;

namespace Tests;

public class EvalTest
{
    
    [SetUp]
    public void Setup() { }

    [Test]
    public void Test1()
    {
        var r = Expr.Eval<int>("123");
        Assert.That(r, Is.EqualTo(123));
    }
}