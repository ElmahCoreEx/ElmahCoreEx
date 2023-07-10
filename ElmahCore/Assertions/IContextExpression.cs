namespace ElmahCore.Assertions
{
    public interface IContextExpression
    {
        object Evaluate(object context);
    }
}