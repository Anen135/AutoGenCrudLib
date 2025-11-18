namespace AutoGenCrudLib.Extensions.Filters;

public class EnumFilter : FilterExpression
{
    public List<object> Selected { get; set; } = new();
    public override bool Check(object value) => Selected.Contains(value);
}
