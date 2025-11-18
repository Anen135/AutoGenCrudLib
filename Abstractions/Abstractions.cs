using AutoGenCrudLib.Models;
using SQLite;
using System.Linq;

namespace AutoGenCrudLib.Abstractions;

public interface IDatabaseProvider
{
    Dictionary<Type, Func<List<EntityBase>>> ForeignMap { get; }
    SQLiteConnection Connection { get; }
}

public class DatabaseProvider : IDatabaseProvider
{
    public DatabaseProvider(string ConnectionString)
    {
        Connection = new SQLiteConnection(ConnectionString);
        Connection.CreateTable<Audit>();
    }
    public void CreateForeign<T>() where T : EntityBase, new()
    {
        ForeignMap[typeof(T)] = () => Connection.Table<T>().ToList<EntityBase>();
    }

    public Dictionary<Type, Func<List<EntityBase>>> ForeignMap { get; set; } = new();
    public SQLiteConnection Connection { get; set; }


}

public interface IAccessControlProvider
{
    bool CanCreate<T>();
    bool CanDelete<T>();
    bool CanEdit<T>();
    bool CanView<T>();
}

public class AccesControlBase : IAccessControlProvider
{
    public virtual bool CanCreate<T>()
    {
        return true;
    }

    public virtual bool CanDelete<T>()
    {
        return true;
    }

    public virtual bool CanEdit<T>()
    {
        return true;
    }

    public virtual bool CanView<T>()
    {
        return true;
    }
    public virtual bool CanFilter<T>()
    {
        return true;
    }
}

public interface IUIProvider
{
    Task ShowAlert(string title, string message, string confirm);
    Task<bool> ShowConfirm(string title, string message, string confirm, string cancel);
}

public class UIBase : IUIProvider 
{
    private readonly Func<string, string, string, Task> _showAlert;
    private readonly Func<string, string, string, string, Task<bool>> _showConfirm;

    public UIBase( Func<string, string, string, Task> showAlert, Func<string, string, string, string, Task<bool>> showConfirm)
    {
        _showAlert = showAlert;
        _showConfirm = showConfirm;
    }

    public virtual Task ShowAlert(string title, string message, string confirm) => _showAlert(title, message, confirm);
    public virtual Task<bool> ShowConfirm(string title, string message, string confirm, string cancel) => _showConfirm(title, message, confirm, cancel);
}
