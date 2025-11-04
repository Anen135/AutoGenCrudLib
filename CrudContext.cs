namespace AutoGenCrudLib;

public static class CrudContext
{
    public static Abstractions.IDatabaseProvider Database { get; private set; }
    public static Abstractions.IAccessControlProvider Access { get; private set; }
    public static Abstractions.IUIProvider UI { get; private set; }

    public static void Init(
        Abstractions.IDatabaseProvider database,
        Abstractions.IAccessControlProvider access,
        Abstractions.IUIProvider ui)
    {
        Database = database;
        Access = access;
        UI = ui;
    }


}
