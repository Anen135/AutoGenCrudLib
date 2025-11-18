namespace AutoGenCrudLib.Extensions;

public abstract class FilterExpression
{
    public string FieldName { get; set; }
    public abstract bool Check(object value);
}
