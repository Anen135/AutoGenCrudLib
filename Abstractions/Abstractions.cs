using AutoGenCrudLib.Models;
using SQLite;

namespace AutoGenCrudLib.Abstractions;

public interface IDatabaseProvider
{
    Dictionary<Type, Func<List<EntityBase>>> ForeignMap { get; }
    SQLiteConnection Connection { get; }
}

public interface IAccessControlProvider
{
    bool CanCreate<T>();
    bool CanDelete<T>();
    bool CanEdit<T>();
    bool CanView<T>();
}

public interface IUIProvider
{
    Task ShowAlert(string title, string message, string confirm);
    Task<bool> ShowConfirm(string title, string message, string confirm, string cencel);
}
