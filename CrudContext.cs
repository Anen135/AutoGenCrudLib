using AutoGenCrudLib.Abstractions;
using AutoGenCrudLib.Models;

namespace AutoGenCrudLib;

public static class CrudContext
{
    public static DatabaseProvider Database { get; set; }
    public static AccessControlBase Access { get; set; }
    public static UIBase UI { get; set; }
    public static EntityBase CurrentUser = null;

    /// <summary>
    /// Initializes the library context. Must be called once before using any page or extension.
    /// </summary>
    /// <param name="database">Database provider (SQLite connection and foreign-key maps).</param>
    /// <param name="access">Access control provider (CanCreate/CanRead/CanUpdate/CanDelete checks).</param>
    /// <param name="ui">UI provider (alerts and confirm dialogs).</param>
    /// <param name="currentUser">Optional user entity recorded in the audit trail.</param>
    public static void Init(
        DatabaseProvider database,
        AccessControlBase access,
        UIBase ui,
        EntityBase currentUser = null)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
        Access = access ?? throw new ArgumentNullException(nameof(access));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
        CurrentUser = currentUser;
    }
}
