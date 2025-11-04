using AutoGenCrudLib.Attributes;
using SQLite;
using System.Reflection;

namespace AutoGenCrudLib.Views;

public class EntityDetailPage<T> : ContentPage where T : new()
{
    private T Entity;
    private PropertyInfo[] properties;
    private Dictionary<PropertyInfo, Entry> entries = new();
    private Dictionary<PropertyInfo, Entry> numeric = new();
    private Dictionary<PropertyInfo, Picker> pickers = new();
    private Dictionary<PropertyInfo, Picker> foreigns = new();

    public EntityDetailPage(T entity)
    {
        Entity = entity;
        properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Title = $"{typeof(T).Name} Detail";
        BuildView();
    }

    private void BuildView()
    {
        var VSL = new VerticalStackLayout();
        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;
            VSL.Add(GetLine(prop));
        }
        var saveBtn = new Button { Text = "Save" };
        var deleteBtn = new Button { Text = "Delete" };
        saveBtn.Clicked += (_, __) => Save();
        deleteBtn.Clicked += (_, __) => Delete();
        VSL.Add(saveBtn);
        VSL.Add(deleteBtn);
        Content = new ScrollView { Content = VSL };
    }

    private View GetLine(PropertyInfo prop)
    {
        var val = prop.GetValue(Entity);
        if (prop.GetCustomAttribute<PrimaryKeyAttribute>() != null || prop.GetCustomAttribute<FreezeAttribute>() != null)
        { // Label 
            if (val == null || val.ToString() == null) val = "--";
            return new Label { Text = $"{prop.Name}: {val}" };
        }
        else if (prop.PropertyType == typeof(string))
        { // Entry
            if (val == null || val.ToString() == null) val = "--";
            var entry = new Entry { Text = val.ToString() };
            entries[prop] = entry;
            return new HorizontalStackLayout { Children = { new Label { Text = $"{prop.Name}" }, entry } };
        }
        else if (prop.PropertyType == typeof(double))
        { // Double
            if (val == null || val.ToString() == null) val = "--";
            var entry = new Entry { Text = val.ToString() };
            numeric[prop] = entry;
            return new HorizontalStackLayout { Children = { new Label { Text = $"{prop.Name}" }, entry } };
        }
        else if (prop.PropertyType.IsEnum)
        { // Picker
            var picker = new Picker
            {
                Title = $"Select {prop.Name}",
                ItemsSource = Enum.GetValues(prop.PropertyType),
                ItemDisplayBinding = new Binding(".")
            };
            if (val != null) picker.SelectedItem = val;
            pickers[prop] = picker;
            return picker;
        }
        else if (prop.GetCustomAttribute<ForeignAttribute>() is not null and var foreignattr)
        {
            var items = CrudContext.Database.ForeignMap[foreignattr.ForeignType]();
            var picker = new Picker
            {
                Title = $"{prop.Name}",
                ItemsSource = items,
                ItemDisplayBinding = new Binding("Name")
            };
            if (val is int Id)
                picker.SelectedItem = items.FirstOrDefault(x => x.Id == Id);
            foreigns[prop] = picker;
            return picker;
        }
        else
        { // Unexpected
            return new Label { Text = $"{prop.Name} : {val?.ToString() ?? "Unexpected"}" };
        }
    }

    private async void Save()
    {
        try
        {
            foreach (var (key, val) in entries)
            {
                key.SetValue(Entity, val.Text);
            }
            foreach (var (key, val) in numeric)
            {
                key.SetValue(Entity, double.Parse(val.Text));
            }
            foreach (var (key, val) in pickers)
            {
                key.SetValue(Entity, val.SelectedItem);
            }
            foreach (var (key, val) in foreigns)
            {
                if (val.SelectedItem is Models.EntityBase entity)
                    key.SetValue(Entity, entity.Id);
            }

            CrudContext.Database.Connection.Update(Entity);
            await CrudContext.UI.ShowAlert("Save", "Saved successfully", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await CrudContext.UI.ShowAlert("Error", $"Error on save: {ex.Message}", "OK");
        }
    }

    private async void Delete()
    {
        var answer = await CrudContext.UI.ShowConfirm("Delete", "Are you sure?", "Yes", "Noooo");
        if (!answer) return;
        CrudContext.Database.Connection.Delete(Entity);
        await Navigation.PopAsync();
    }
}
