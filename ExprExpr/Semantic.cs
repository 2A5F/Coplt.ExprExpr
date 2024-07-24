using System.Linq.Expressions;
using Coplt.ExprExpr.Syntaxes;

namespace Coplt.ExprExpr.Semantics;

internal struct InferCtx
{
    public Type? TargetType { get; set; }
}

internal abstract record Semantic
{
    /// <summary>
    /// The result type of the expression
    /// </summary>
    public Type? Type { get; protected set; }
    /// <summary>
    /// Type inference completed, <see cref="Type"/> must have a value
    /// </summary>
    public bool TypeFixed { get; protected set; }
    /// <summary>
    /// If type conversion is required
    /// </summary>
    public Func<Expression, Expression>? Conversion { get; protected set; }

    protected abstract void InferType(ref EvalBuildCtx bc, ref InferCtx ic);

    protected abstract Expression Build(ref EvalBuildCtx bc);

    public Expression ToExpr(ref EvalBuildCtx ctx, Type retType)
    {
        var root_ctx = new InferCtx { TargetType = retType };
        InferType(ref ctx, ref root_ctx);
        return Build(ref ctx);
    }
}

internal sealed record LiteralSemantic(LiteralSyntax Syntax) : Semantic
{
    protected override void InferType(ref EvalBuildCtx bc, ref InferCtx ic)
    {
        var t = Syntax.GetValueType();
        if (ic.TargetType is null || ic.TargetType == t)
        {
            Type = t;
            TypeFixed = true;
            return;
        }
        if (ic.TargetType == typeof(object))
        {
            Type = typeof(object);
            TypeFixed = true;
            Conversion = Utils.ConvertToObject;
            return;
        }
        if (ic.TargetType.IsPrimitive)
        {
            Conversion = Syntax.Conversion(ic.TargetType);
            if (Conversion == null) goto cannot_convert;
            Type = ic.TargetType;
            TypeFixed = true;
            return;
        }
        // todo custom convert
        cannot_convert:
        throw new EvalException($"Cannot implicitly convert from {t} to {ic.TargetType} at {Syntax.offset}");
    }

    protected override Expression Build(ref EvalBuildCtx bc)
    {
        var expr = Syntax.ToExpr(Type!);
        if (Conversion != null) expr = Conversion(expr);
        return expr;
    }
}
