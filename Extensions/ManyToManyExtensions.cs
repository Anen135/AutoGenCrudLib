namespace SP12.Model;

using AutoGenCrudLib.Models;
using System.Reflection;

public static class ManyToManyExtensions
{
    public static List<EntityBase> GetManyToManyList(this EntityBase entity, string propertyName)
    {
        var prop = entity.GetType().GetProperty(propertyName);
        if (prop == null)
            throw new ArgumentException($"Property {propertyName} not found on {entity.GetType().Name}");

        var mmAttr = prop.GetCustomAttribute<AutoGenCrudLib.Attributes.ManyToManyAttribute>();
        if (mmAttr == null)
            throw new InvalidOperationException($"Property {propertyName} is not marked with [ManyToMany]");

        var val = prop.GetValue(entity) as string ?? "";
        var ids = val
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : -1)
            .Where(id => id > 0)
            .ToList();

        var foreignType = mmAttr.ForeignType;
        var allItems = AutoGenCrudLib.CrudContext.Database.ForeignMap[foreignType]();

        return allItems.Where(x => ids.Contains(x.Id)).ToList();
    }

    public static List<T> GetManyToManyList<T>(this EntityBase entity, string propertyName) where T : EntityBase => entity.GetManyToManyList(propertyName).Cast<T>().ToList();

    public static void SetManyToManyList(this EntityBase entity, string propertyName, List<EntityBase> items)
    {
        var prop = entity.GetType().GetProperty(propertyName);
        if (prop == null)
            throw new ArgumentException($"Property {propertyName} not found on {entity.GetType().Name}");
        var mmAttr = prop.GetCustomAttribute<AutoGenCrudLib.Attributes.ManyToManyAttribute>();
        if (mmAttr == null)
            throw new InvalidOperationException($"Property {propertyName} is not marked with [ManyToMany]");
        prop.SetValue(entity, string.Join(",", items.Select(x => x.Id.ToString())));
    }

    public static void SetManyToManyList<T>(this EntityBase entity, string propertyName, List<T> items) where T : EntityBase => entity.SetManyToManyList(propertyName, items.Cast<EntityBase>().ToList());
}