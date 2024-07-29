using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using Coplt.ExprExpr.Syntaxes;
using Coplt.ExprExpr.Typing;
using InlineIL;

namespace Coplt.ExprExpr.Semantics;

internal abstract record Semantic
{
    public Constraint? Constraint { get; protected set; }

    public abstract int Offset { get; }

    public abstract void BuildConstraint(ref EvalBuildCtx bc, out Constraint constraint);
    public abstract Expression Build(ref EvalBuildCtx bc);

    public Expression ToExpr(ref EvalBuildCtx ctx, Type retType)
    {
        BuildConstraint(ref ctx, out var root_constraint);
        Constraint!.Resolve(retType);
        if (root_constraint.ResultType is null) throw new EvalException($"Failed to infer type at {Offset}");
        return Build(ref ctx);
    }
}

internal sealed record LiteralSemantic(LiteralSyntax Syntax) : Semantic
{
    public override int Offset => Syntax.offset;

    public override void BuildConstraint(ref EvalBuildCtx bc, out Constraint constraint)
    {
        if (Syntax is NullLiteralSyntax)
        {
            constraint = Constraint ??= new AnyNullableConstraint { Semantic = this };
            return;
        }
        var types = Syntax.GetPossibleType();
        if (types.Length == 1)
        {
            constraint = Constraint ??= new FixedConstraint(types[0]) { Semantic = this };
            return;
        }
        var defv = Syntax.GetDefaultType();
        constraint = Constraint ??= new FallbackConstraint(types, defv) { Semantic = this };
    }
    public override Expression Build(ref EvalBuildCtx bc)
    {
        if (Constraint!.ResultType is not { } type) throw new UnreachableException("Internal logic error");
        var expr = Syntax.ToExpr(type);
        return expr;
    }
}

internal sealed record BinOpSemantic(Semantic left, Semantic right, OpKind OpKind, BinOpSyntax RawSyntax) : Semantic
{
    public override int Offset => RawSyntax.offset;

    public override void BuildConstraint(ref EvalBuildCtx bc, out Constraint constraint)
    {
        left.BuildConstraint(ref bc, out var l);
        right.BuildConstraint(ref bc, out var r);
        constraint = Constraint ??= OpKind switch
        {
            OpKind.Add => BinOpConstraint.Op_Addition(l, r, this),
            // todo
            _ => throw new EvalException($"Unsupported infix operator {OpKind} at {Offset}")
        };
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
