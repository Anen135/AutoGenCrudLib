namespace AutoGenCrudLib;

public static class CrudContext
{
    public static Abstractions.DatabaseProvider Database { get;  set; }
    public static Abstractions.AccesControlBase Access { get;  set; }
    public static Abstractions.UIBase UI { get;  set; }
    public static Models.EntityBase CurrentUser = null;
}
