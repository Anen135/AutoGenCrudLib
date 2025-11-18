namespace AutoGenCrudLib.Extensions.Filters;

public class NumberRangeFilter : FilterExpression
{
    public double? Min { get; set; }
    public double? Max { get; set; }

    public override bool Check(object value)
    {
        if (value == null) return false;
        var v = Convert.ToDouble(value);

        if (Min.HasValue && v < Min.Value) return false;
        if (Max.HasValue && v > Max.Value) return false;

        return true;
    }
}
