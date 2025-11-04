namespace AutoGenCrudLib.Attributes;
public class ForeignAttribute(Type foreign) : Attribute
{
    public Type ForeignType { get; set; } = foreign;
}
