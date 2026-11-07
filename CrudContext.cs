using AutoGenCrudLib.Abstractions;
using AutoGenCrudLib.Models;

namespace AutoGenCrudLib;

public static class CrudContext
{
    // The provider set is stored in one immutable snapshot and published atomically,
    // so Init can never leave the context half-updated (by a failed or concurrent call).
    private sealed record ProviderSet(
        IDatabaseProvider Database,
        IAccessControlProvider Access,
        IUIProvider UI,
        EntityBase CurrentUser);

    private static volatile ProviderSet _providers;

    public static IDatabaseProvider Database => _providers?.Database;
    public static IAccessControlProvider Access => _providers?.Access;
    public static IUIProvider UI => _providers?.UI;
    public static EntityBase CurrentUser => _providers?.CurrentUser;

    /// <summary>
    /// Initializes the library context. Must be called once before using any page or extension.
    /// All arguments are validated before the context is updated; the provider set is then
    /// published atomically, so a failed or concurrent call cannot mix providers.
    /// </summary>
    /// <param name="database">Database provider (SQLite connection and foreign-key maps).</param>
    /// <param name="access">Access control provider (CanCreate/CanView/CanEdit/CanDelete/CanFilter checks).</param>
    /// <param name="ui">UI provider (alerts and confirm dialogs).</param>
    /// <param name="currentUser">Optional user entity recorded in the audit trail.</param>
    public static void Init(
        IDatabaseProvider database,
        IAccessControlProvider access,
        IUIProvider ui,
        EntityBase currentUser = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (access == null) throw new ArgumentNullException(nameof(access));
        if (ui == null) throw new ArgumentNullException(nameof(ui));

        _providers = new ProviderSet(database, access, ui, currentUser);
    }
}
