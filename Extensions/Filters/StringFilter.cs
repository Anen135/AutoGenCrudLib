namespace AutoGenCrudLib.Extensions.Filters;

public class StringFilter : FilterExpression
{
    public enum Mode { Contains, Equals, StartsWith, EndsWith }
    public Mode Operation { get; set; }
    public string Value { get; set; }

    public override bool Check(object value)
    {
        var s = value?.ToString() ?? "";
        return Operation switch
        {
            Mode.Contains => s.Contains(Value, StringComparison.OrdinalIgnoreCase),
            Mode.Equals => s.Equals(Value, StringComparison.OrdinalIgnoreCase),
            Mode.StartsWith => s.StartsWith(Value, StringComparison.OrdinalIgnoreCase),
            Mode.EndsWith => s.EndsWith(Value, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
