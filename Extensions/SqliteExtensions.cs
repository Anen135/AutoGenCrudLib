using SQLite;
using System.Reflection;

namespace AutoGenCrudLib.Extensions;

public static class SqliteExtensions
{
    public static object DuplicateRecord(this object entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var type = entity.GetType();
        var newEntity = Activator.CreateInstance(type)!;

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if ((prop.GetCustomAttribute<PrimaryKeyAttribute>() != null
                 && prop.GetCustomAttribute<AutoIncrementAttribute>() != null) || prop.GetCustomAttribute<UniqueAttribute>() != null)
                continue;

            var value = prop.GetValue(entity);
            prop.SetValue(newEntity, value);
        }
        CrudContext.Database.Connection.Insert(newEntity);
        return newEntity;
    }
}
