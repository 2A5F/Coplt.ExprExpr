using System.Reflection;

namespace Coplt.ExprExpr;

public abstract class EvalOptions
{
    public static EvalOptions Default { get; } = new DefaultEvalOptions();

    public abstract IEnumerable<Type> ExposedTypes { get; }
    public abstract IEnumerable<Type> OpenedStatics { get; }
}

public class DefaultEvalOptions : EvalOptions
{
    public override IEnumerable<Type> ExposedTypes { get; } = [typeof(Console)];
    public override IEnumerable<Type> OpenedStatics { get; } = [typeof(Math), typeof(MathF)];
}
