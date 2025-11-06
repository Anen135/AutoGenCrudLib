using AutoGenCrudLib.Attributes;
using SQLite;
using System.Reflection;

namespace AutoGenCrudLib.Views;



public class EntityDetailPage<T> : ContentPage where T : Models.EntityBase, new()
{
    private T Entity;
    private PropertyInfo[] properties;
    private Dictionary<PropertyInfo, Entry> entries = new();
    private Dictionary<PropertyInfo, Entry> numeric = new();
    private Dictionary<PropertyInfo, Picker> pickers = new();
    private Dictionary<PropertyInfo, Picker> foreigns = new();
    private Dictionary<PropertyInfo, CheckBox> checkboxes = new();
    private Dictionary<PropertyInfo, List<CheckBox>> manytomany = new();

    public EntityDetailPage(T entity)
    {
        Entity = entity;
        properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Title = $"{typeof(T).Name} Detail";
        Content = BuildView();
    }

    private View BuildView()
    {
        var VSL = new VerticalStackLayout
        {
            Padding = new Thickness(15),
            Spacing = 8
        };
        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;
            VSL.Add(GetLine(prop));
        }
        var saveBtn = new Button { Text = "Save" };
        var deleteBtn = new Button { Text = "Delete" };
        var refreshBtn = new Button { Text = "Refresh" };
        saveBtn.Clicked += (_, __) => Save();
        deleteBtn.Clicked += (_, __) => Delete();
        refreshBtn.Clicked += (_, __) => Refresh();
        VSL.Add(saveBtn);
        VSL.Add(deleteBtn);
        VSL.Add(refreshBtn);
        return new ScrollView { Content = VSL };
    }

    private View GetLine(PropertyInfo prop)
    {
        var val = prop.GetValue(Entity);
        if (prop.GetCustomAttribute<PrimaryKeyAttribute>() != null || prop.GetCustomAttribute<FreezeAttribute>() != null) return GetLabel(prop, ref val);
        else if (prop.GetCustomAttribute<ManyToManyAttribute>() is ManyToManyAttribute mmattr) return GetManyToMany(prop, val, mmattr);
        else if (prop.GetCustomAttribute<ForeignAttribute>() is not null and var foreignattr) return GetForeign(prop, val, foreignattr);
        else if (prop.PropertyType == typeof(string)) return GetEntry(prop, ref val);
        else if (prop.PropertyType == typeof(double)) return GetDigital(prop, ref val);
        else if (prop.PropertyType.IsEnum) return GetEnumerate(prop, val);
        else if (prop.PropertyType == typeof(bool)) return GetCheckbox(prop, val);
        else return new Label { Text = $"{prop.Name} : {val?.ToString() ?? "Unexpected"}" };
    }

    private View GetManyToMany(PropertyInfo prop, object val, ManyToManyAttribute mmattr)
    {
        var items = CrudContext.Database.ForeignMap[mmattr.ForeignType]();

        var existingIds = (val as string ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : -1)
            .Where(id => id > 0)
            .ToHashSet();

        var label = new Label { Text = $"{prop.Name}" };
        var stack = new VerticalStackLayout { Children = { label } };
        var checkboxes = new List<CheckBox>();

        foreach (var item in items)
        {
            var cb = new CheckBox { IsChecked = existingIds.Contains(item.Id) };

            var nameLabel = new Label
            {
                Text = item.Name,
                VerticalOptions = LayoutOptions.Center,
            };
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (_, __) =>
            {
                try
                {
                    var pageType = typeof(EntityDetailPage<>).MakeGenericType(mmattr.ForeignType);
                    var page = (Page)Activator.CreateInstance(pageType, item);
                    await Navigation.PushAsync(page);
                }
                catch (Exception ex)
                {
                    await CrudContext.UI.ShowAlert("Error", $"Не удалось открыть детали: {ex.Message}", "OK");
                }
            };
            nameLabel.GestureRecognizers.Add(tapGesture);

            var h = new HorizontalStackLayout
            {
                Spacing = 5,
                Children = { cb, nameLabel }
            };

            checkboxes.Add(cb);
            stack.Children.Add(h);
        }

        manytomany[prop] = checkboxes;
        return stack;
    }



    private View GetForeign(PropertyInfo prop, object val, ForeignAttribute foreignattr)
    {
        var items = CrudContext.Database.ForeignMap[foreignattr.ForeignType]();
        var picker = new Picker
        {
            Title = prop.Name,
            ItemsSource = items,
            ItemDisplayBinding = new Binding("Name"),
            VerticalOptions = LayoutOptions.Fill,
            Margin = 0,
        };

        if (val is int Id)
            picker.SelectedItem = items.FirstOrDefault(x => x.Id == Id);

        foreigns[prop] = picker;

        var openButton = new Button
        {
            Text = "Next",
            WidthRequest = 40,
            BackgroundColor = Colors.Transparent,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(0),
            VerticalOptions = LayoutOptions.Fill, 
            HorizontalOptions = LayoutOptions.Fill,
            CornerRadius = 0,
            BorderWidth = 0,
            BorderColor = Colors.Transparent,
        };

        openButton.Clicked += async (_, __) =>
        {
            if (picker.SelectedItem is Models.EntityBase selectedEntity)
            {
                var pageType = typeof(EntityDetailPage<>).MakeGenericType(foreignattr.ForeignType);
                var page = (Page)Activator.CreateInstance(pageType, selectedEntity);
                await Navigation.PushAsync(page);
            }
            else
            {
                await CrudContext.UI.ShowAlert("Info", "Выберите элемент перед переходом", "OK");
            }
        };

        var grid = new Grid
        {
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto } }, 
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 40 }
            },
            Margin = new Thickness(10, 5)
        };

        grid.Add(picker, 0, 0);
        grid.Add(openButton, 1, 0);

        return grid;
    }




    private View GetCheckbox(PropertyInfo prop, object val)
    {
        var checkbox = new CheckBox { IsChecked = val is bool b && b };
        checkboxes[prop] = checkbox;
        return new HorizontalStackLayout { Children = { new Label { Text = $"{prop.Name}" }, checkbox } };
    }

    private View GetEnumerate(PropertyInfo prop, object val)
    {
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

    private View GetDigital(PropertyInfo prop, ref object val)
    {
        if (val == null || val.ToString() == null) val = "--";
        var label = new Label
        {
            Text = prop.Name,
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = 120
        };
        var entry = new Entry
        {
            Text = val.ToString(),
            Keyboard = Keyboard.Numeric
        };
        numeric[prop] = entry;
        var grid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star }
        },
            Margin = new Thickness(10, 4),
        };
        grid.Add(label, 0, 0);
        grid.Add(entry, 1, 0);
        return grid;
    }

    private View GetEntry(PropertyInfo prop, ref object val)
    {
        if (val == null || val.ToString() == null) val = "--";
        var label = new Label
        {
            Text = prop.Name,
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = 120
        };

        var entry = new Entry
        {
            Text = val.ToString()
        };
        entries[prop] = entry;
        var grid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star }
        },
            Margin = new Thickness(10, 4),
        };
        grid.Add(label, 0, 0);
        grid.Add(entry, 1, 0);
        return grid;
    }


    private static View GetLabel(PropertyInfo prop, ref object val)
    {
        if (val == null || val.ToString() == null) val = "--";
        return new Label { Text = $"{SplitPascalCase(prop.Name)}: {val}", FontAttributes = FontAttributes.Bold };
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
            foreach (var (key, val) in checkboxes)
            {
                key.SetValue(Entity, val.IsChecked);
            }
            foreach (var (key, list) in manytomany)
            {
                var items = CrudContext.Database.ForeignMap[key.GetCustomAttribute<ManyToManyAttribute>()!.ForeignType]();

                var selectedIds = new List<int>();
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsChecked)
                        selectedIds.Add(items[i].Id);
                }

                key.SetValue(Entity, string.Join(",", selectedIds));
            }


            CrudContext.Database.Connection.Update(Entity);
            CrudContext.Database.Connection.Insert(new Models.Audit { Name = $"{CrudContext.CurrentUser?.Name ?? "Unknown"} - Save {Entity.Name}", Description = $"{typeof(T)}" });
            await CrudContext.UI.ShowAlert("Save", "Saved successfully", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await CrudContext.UI.ShowAlert("Error", $"Error on save: {ex.Message}", "OK");
        }
    }

    private void Refresh()
    {
        Content = BuildView();
    }

    private async void Delete()
    {
        var answer = await CrudContext.UI.ShowConfirm("Delete", "Are you sure?", "Yes", "Noooo");
        if (!answer) return;
        CrudContext.Database.Connection.Delete(Entity);
        CrudContext.Database.Connection.Insert(new Models.Audit { Name = $"{CrudContext.CurrentUser?.Name ?? "Unknown"} - Delete {Entity.Name}", Description = $"{typeof(T)}" });
        await Navigation.PopAsync();
    }

    private static string SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) &&
                !char.IsUpper(input[i - 1]) &&
                !(i + 1 < input.Length && char.IsUpper(input[i + 1]) && char.IsLower(input[i - 1])))
            {
                result.Append(' ');
            }
            result.Append(input[i]);
        }

        return result.ToString();
    }
}
