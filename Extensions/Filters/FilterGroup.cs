namespace AutoGenCrudLib.Extensions.Filters;

public class FilterGroup : FilterExpression
{
    public enum LogicType { And, Or }
    public LogicType Logic { get; set; }
    public List<FilterExpression> Children { get; set; } = new();

    public override bool Check(object value)
    {
        return Logic == LogicType.And
            ? Children.All(f => f.Check(value))
            : Children.Any(f => f.Check(value));
    }
}
