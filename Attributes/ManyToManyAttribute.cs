namespace AutoGenCrudLib.Attributes;
public class ManyToManyAttribute(Type foreign) : Attribute
{
    public Type ForeignType { get; set; } = foreign;
}
