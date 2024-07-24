namespace Coplt.ExprExpr;

public class ParserException : Exception
{
    public ParserException() { }
    public ParserException(string message) : base(message) { }
    public ParserException(string message, Exception inner) : base(message, inner) { }
}

public class EvalException : Exception
{
    public EvalException() { }
    public EvalException(string message) : base(message) { }
    public EvalException(string message, Exception inner) : base(message, inner) { }
}
