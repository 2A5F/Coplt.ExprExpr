using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Coplt.ExprExpr.Semantics;

namespace Coplt.ExprExpr.Typing;

internal abstract record Constraint
{
    public required Semantic Semantic { get; init; }
    public Type? ResultType { get; protected set; }
    /// <summary>
    /// When call <see cref="Resolve"/>, target.ResultType must be not null
    /// </summary>
    public abstract void Resolve(Type? target);

    public virtual ImmArr<Type> InferPossibleTypes() => ResultType is null ? [] : [ResultType];
}

internal record FixedConstraint(Type Type) : Constraint
{
    public override void Resolve(Type? target)
    {
        if (target is not null && !target.IsAssignableFrom(Type))
            throw new EvalException($"{Type} can not assignable to {target} at {Semantic.Offset}");
        ResultType = Type;
    }

    public override ImmArr<Type> InferPossibleTypes() => [Type];
}

/// <summary>
/// <see cref="Types"/> must include <see cref="Default"/>
/// </summary>
internal record FallbackConstraint(ImmArr<Type> Types, Type Default) : Constraint
{
    public override void Resolve(Type? target)
    {
        if (target is null) goto def;
        foreach (var type in Types)
        {
            if (target.IsAssignableFrom(type))
            {
                ResultType = type;
                return;
            }
        }
        if (!target.IsAssignableFrom(Default))
            throw new EvalException($"{Default} can not assignable to {target} at {Semantic.Offset}");
        def:
        ResultType = Default;
    }

    public override ImmArr<Type> InferPossibleTypes() => Types;
}

internal record BinOpConstraint(Constraint L, Constraint R, string Name, Type[] Primitives) : Constraint
{
    private static readonly Type[] Op_Addition_Primitives =
        [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)];

    public static BinOpConstraint Op_Addition(Constraint L, Constraint R, Semantic Semantic) =>
        new(L, R, "op_Addition", Op_Addition_Primitives) { Semantic = Semantic };

    public override void Resolve(Type? target)
    {
        var lt = L.InferPossibleTypes();
        var rt = R.InferPossibleTypes();
        if (lt.Length is 0)
        {
            L.Resolve(null);
            lt = L.InferPossibleTypes();
        }
        if (rt.Length is 0)
        {
            R.Resolve(null);
            rt = R.InferPossibleTypes();
        }
        if (lt.Length is 0 || rt.Length is 0) goto err;
        if (Resolve(target, lt, rt, false)) return;
        if (Resolve(target, rt, lt, true)) return;
        err:
        throw new EvalException($"Failed to infer type at {Semantic.Offset}");
    }

    private bool Resolve(Type? target, ImmArr<Type> lt, ImmArr<Type> rt, bool inv)
    {
        foreach (var l in lt)
        {
            if (l.IsPrimitive && Primitives.Contains(l))
            {
                foreach (var r in rt)
                {
                    if (!Utils.PrimitiveCanConversion(r, l)) continue;
                    if (target is null) goto ok;
                    if (target.IsAssignableFrom(l)) // todo check convert
                        goto ok;
                    continue;
                    ok:
                    L.Resolve(l);
                    R.Resolve(r);
                    ResultType = l;
                    return true;
                }
            }
            foreach (var op in l.GetMethods(BindingFlags.Public | BindingFlags.Static)
                         .Where(m => m.Name == Name))
            {
                var p = op.GetParameters();
                if (p is not [var p0, var p1]) continue;
                if (inv) (p0, p1) = (p1, p0);
                if (p0.ParameterType != l) continue;
                foreach (var r in rt)
                {
                    if (p1.ParameterType.IsAssignableFrom(r))
                    {
                        if (target is null) goto ok;
                        if (target.IsAssignableFrom(op.ReturnType)) // todo check convert
                            goto ok;
                        continue;
                        ok:
                        L.Resolve(l);
                        R.Resolve(r);
                        ResultType = op.ReturnType;
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

internal record AnyNullableConstraint : Constraint
{
    public override void Resolve(Type? target)
    {
        throw new NotImplementedException("todo");
    }
}
