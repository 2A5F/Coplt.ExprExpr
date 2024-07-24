using System.Linq.Expressions;
using Coplt.ExprExpr.Parsers;
using Coplt.ExprExpr.Semantics;

namespace Coplt.ExprExpr;

public static partial class Expr
{
    public static T Eval<T>(Str code) => Create<T>(code)();
    public static T Eval<T>(Str code, EvalOptions options) => Create<T>(code, options)();

    public static Func<T> Create<T>(Str code) => Create<T>(code, EvalOptions.Default);
    public static Func<T> Create<T>(Str code, EvalOptions options)
    {
        var syn = Parser.Parse(code);
        var sem = syn.ToSemantic();
        var ctx = new EvalBuildCtx() { Options = options };
        var expr = sem.ToExpr(ref ctx, typeof(T));
        var lambda = Expression.Lambda<ExprFunc<T>>(expr, [ctx.CtxParamExpr]);
        var f = lambda.Compile();
        var eval_ctx_ = new EvalCtx { CtxParams = ctx.CtxParamCtx.CtxParams };
        return () =>
        {
            var eval_ctx = eval_ctx_;
            return f(ref eval_ctx);
        };
    }
}
