using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using Coplt.ExprExpr.Syntaxes;
using InlineIL;

namespace Coplt.ExprExpr.Semantics;

internal struct InferCtx()
{
    public Typ TargetType { get; set; } = Typ.TheHole;
    public Constraint? TargetConstraints { get; set; }
}

internal abstract record Semantic
{
    /// <summary>
    /// The result type of the expression
    /// </summary>
    public Typ Type { get; protected set; } = Typ.TheHole;

    public abstract int Offset { get; }

    /// <summary> Recursively initialize the leaf node type </summary>
    public abstract void PreInferType(ref EvalBuildCtx bc);

    public abstract void InferType(ref EvalBuildCtx bc, ref InferCtx ic);

    public abstract Expression Build(ref EvalBuildCtx bc);

    public Expression ToExpr(ref EvalBuildCtx ctx, Typ retType)
    {
        var root_ctx = new InferCtx { TargetType = retType };
        PreInferType(ref ctx);
        InferType(ref ctx, ref root_ctx);
        if (Type is not Typ.One) throw new EvalException("Failed to infer type");
        return Build(ref ctx);
    }
}

internal sealed record LiteralSemantic(LiteralSyntax Syntax) : Semantic
{
    public override int Offset => Syntax.offset;

    public override void PreInferType(ref EvalBuildCtx bc)
    {
        Type = Syntax.GetPossibleType();
    }
    public override void InferType(ref EvalBuildCtx bc, ref InferCtx ic)
    {
        if (Type is Typ.One) return;
        Type &= ic.TargetType;
    }

    public override Expression Build(ref EvalBuildCtx bc)
    {
        if (Type is not Typ.One(var type)) throw new UnreachableException("Internal logic error");
        var expr = Syntax.ToExpr(type);
        return expr;
    }
}

internal sealed record BinOpSemantic(Semantic left, Semantic right, OpKind OpKind, BinOpSyntax RawSyntax) : Semantic
{
    public override int Offset => RawSyntax.offset;
    public override void PreInferType(ref EvalBuildCtx bc)
    {
        left.PreInferType(ref bc);
        right.PreInferType(ref bc);
    }

    #region ConstraintCache

    private static readonly Dictionary<OpKind, ConditionalWeakTable<Typ, Constraint>> ConstraintCache = new();

    private static Constraint GetConstraint(OpKind op, Typ TargetType, Func<Typ, Constraint> create)
    {
        if (!ConstraintCache.TryGetValue(op, out var cache))
        {
            cache = new();
            ConstraintCache[op] = cache;
        }
        if (cache.TryGetValue(TargetType, out var c)) return c;
        c = create(TargetType);
        cache.Add(TargetType, c);
        return c;
    }

    #endregion

    #region Constraints

    private static Constraint AddConstraint(Typ TargetType) =>
        new InterfaceConstraint(new TRef.Open(typeof(IAdditionOperators<,,>), [Typ.TheHole, Typ.TheHole, TargetType]));

    #endregion


    public override void InferType(ref EvalBuildCtx bc, ref InferCtx ic)
    {
        switch (OpKind)
        {
            case OpKind.Add:
            {
                var sic = new InferCtx
                {
                    TargetConstraints = GetConstraint(OpKind.Add, ic.TargetType, AddConstraint)
                };
                left.InferType(ref bc, ref ic);

                // todo
                break;
            }
            case OpKind.Add or OpKind.Sub or OpKind.Mul or OpKind.Div or OpKind.Rem:
            {
                // todo
                break;
            }
            case OpKind.Pow:
            {
                // todo
                break;
            }
            default: throw new EvalException($"Unsupported infix operator {OpKind} at {Offset}");
        }
    }
    public override Expression Build(ref EvalBuildCtx bc)
    {
        var l = left.Build(ref bc);
        var r = right.Build(ref bc);
        return OpKind switch
        {
            OpKind.Add => Expression.Add(l, r),
            OpKind.Sub => Expression.Subtract(l, r),
            OpKind.Mul => Expression.Multiply(l, r),
            OpKind.Div => Expression.Divide(l, r),
            OpKind.Rem => Expression.Modulo(l, r),
            OpKind.Pow => Utils.MakePowerExpr(l, r, l.Type!),
            OpKind.Shl => Expression.LeftShift(l, r),
            OpKind.Shr => Expression.RightShift(l, r),
            OpKind.Rol => Expression.Call(Utils.RotateLeftMethod(l.Type!), l, r),
            OpKind.Ror => Expression.Call(Utils.RotateRightMethod(l.Type!), l, r),
            OpKind.And => Expression.And(l, r),
            OpKind.Or => Expression.Or(l, r),
            OpKind.Xor => Expression.ExclusiveOr(l, r),
            OpKind.BoolAnd => Expression.AndAlso(l, r),
            OpKind.BoolOr => Expression.OrElse(l, r),
            OpKind.Eq => Expression.Equal(l, r),
            OpKind.Ne => Expression.NotEqual(l, r),
            OpKind.Lt => Expression.LessThan(l, r),
            OpKind.Gt => Expression.GreaterThan(l, r),
            OpKind.Le => Expression.LessThanOrEqual(l, r),
            OpKind.Ge => Expression.GreaterThanOrEqual(l, r),
            _ => throw new EvalException($"Unsupported infix operator {OpKind} at {Offset}")
        };
    }
}
