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
    private Dictionary<PropertyInfo, List<int>> manytomany = new();
    private Dictionary<PropertyInfo, object> files = new();

    public EntityDetailPage(T entity)
    {
        Entity = entity;
        properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Title = $"{typeof(T).Name} Detail";
        Content = BuildView();
    }

    public virtual View BuildView()
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
        deleteBtn.IsVisible = CrudContext.Access.CanDelete<T>();
        saveBtn.IsVisible = CrudContext.Access.CanEdit<T>();
        var refreshBtn = new Button { Text = "Refresh" };
        saveBtn.Clicked += (_, __) => Save();
        deleteBtn.Clicked += (_, __) => Delete();
        refreshBtn.Clicked += (_, __) => Refresh();
        VSL.Add(saveBtn);
        VSL.Add(deleteBtn);
        VSL.Add(refreshBtn);
        return new ScrollView { Content = VSL };
    }

    public View GetLine(PropertyInfo prop)
    {
        var val = prop.GetValue(Entity);
        if (prop.GetCustomAttribute<PrimaryKeyAttribute>() != null || prop.GetCustomAttribute<FreezeAttribute>() != null) return GetLabel(prop, ref val);
        else if (prop.GetCustomAttribute<ManyToManyAttribute>() is ManyToManyAttribute mmattr) return GetManyToMany(prop, val, mmattr);
        else if (prop.GetCustomAttribute<ForeignAttribute>() is not null and var foreignattr) return GetForeign(prop, val, foreignattr);
        else if (prop.GetCustomAttribute<FileAttribute>() != null && prop.PropertyType == typeof(string)) return GetFileField(prop, val);
        else if (prop.PropertyType == typeof(string)) return GetEntry(prop, ref val);
        else if (prop.PropertyType == typeof(double)) return GetDigital(prop, ref val);
        else if (prop.PropertyType.IsEnum) return GetEnumerate(prop, val);
        else if (prop.PropertyType == typeof(bool)) return GetCheckbox(prop, val);
        else return new Label { Text = $"{prop.Name} : {val?.ToString() ?? "Unexpected"}" };
    }

    public Grid GetFileField(PropertyInfo prop, object val)
    {
        if (val == null) val = "";

        var label = new Label
        {
            Text = prop.Name,
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = 120
        };

        var selectButton = new Button { Text = "Select File" };
        var openButton = new Button { Text = "Open File" };

        selectButton.Clicked += async (_, __) =>
        {
            try
            {
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    val = result.FullPath;
                    prop.SetValue(Entity, val);
                    files[prop] = val;
                }
            }
            catch (Exception ex)
            {
                await CrudContext.UI.ShowAlert("Error", $"File selection failed: {ex.Message}", "OK");
            }
        };

        openButton.Clicked += async (_, __) =>
        {
            try
            {
                var filePath = val?.ToString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    await Launcher.Default.OpenAsync(filePath);
                }
                else
                {
                    await CrudContext.UI.ShowAlert("Info", "File path is empty", "OK");
                }
            }
            catch (Exception ex)
            {
                await CrudContext.UI.ShowAlert("Error", $"Cannot open file: {ex.Message}", "OK");
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Auto }
        },
            Margin = new Thickness(10, 4)
        };

        grid.Add(label, 0, 0);
        grid.Add(selectButton, 1, 0);
        grid.Add(openButton, 2, 0);

        return grid;
    }



    public VerticalStackLayout GetManyToMany(PropertyInfo prop, object val, ManyToManyAttribute mmattr)
    {
        var items = CrudContext.Database.ForeignMap[mmattr.ForeignType]();

        var selectedIds = (val as string ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out int id) ? id : -1)
            .ToList();

        manytomany[prop] = selectedIds;

        var label = new Label { Text = prop.Name };

        CollectionView cv = new CollectionView();

        cv.ItemTemplate = new DataTemplate(() =>
        {
            var nameLabel = new Label
            {
                VerticalOptions = LayoutOptions.Center
            };
            nameLabel.SetBinding(Label.TextProperty, "Name");

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                if (nameLabel.BindingContext is not Models.EntityBase be)
                    return;
                if (!(bool)CrudContext.Access.GetType().GetMethod("CanView")!.MakeGenericMethod(mmattr.ForeignType).Invoke(CrudContext.Access, null)!)
                {
                    await CrudContext.UI.ShowAlert("Access is denied", "No permission to view", "OK");
                    return;
                }

                try
                {
                    var pageType = typeof(EntityDetailPage<>).MakeGenericType(mmattr.ForeignType);
                    var page = (Page)Activator.CreateInstance(pageType, be);
                    await Navigation.PushAsync(page);
                }
                catch (Exception ex)
                {
                    await CrudContext.UI.ShowAlert("Error",
                        $"Couldn't open details: {ex.Message}", "OK");
                }
            };
            nameLabel.GestureRecognizers.Add(tap);

            var deleteBtn = new Button
            {
                Text = "X",
                BackgroundColor = Colors.Transparent,
                BorderWidth = 0,
                FontAttributes = FontAttributes.Bold
            };

            deleteBtn.Clicked += (_, __) =>
            {
                if (deleteBtn.BindingContext is Models.EntityBase be)
                {
                    selectedIds.Remove(be.Id);
                    cv.ItemsSource = selectedIds.Select(id => items.First(i => i.Id == id)).ToList();
                }
            };

            return new HorizontalStackLayout
            {
                Spacing = 10,
                Children = { nameLabel, deleteBtn }
            };
        });

        cv.ItemsSource = selectedIds
            .Select(id => items.First(i => i.Id == id))
            .ToList();

        var picker = new Picker
        {
            Title = "Добавить...",
            ItemsSource = items,
            ItemDisplayBinding = new Binding("Name")
        };

        picker.SelectedIndexChanged += (_, __) =>
        {
            if (picker.SelectedItem is Models.EntityBase be)
            {
                selectedIds.Add(be.Id);

                cv.ItemsSource = selectedIds
                    .Select(id => items.First(i => i.Id == id))
                    .ToList();

                picker.SelectedIndex = -1;
            }
        };

        return new VerticalStackLayout
        {
            Children = { label, cv, picker }
        };
    }

    public Grid GetForeign(PropertyInfo prop, object val, ForeignAttribute foreignattr)
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
            IsEnabled = picker.SelectedItem is not null,
            BorderColor = Colors.Transparent,
        };

        openButton.Clicked += async (_, __) =>
        {
            if (picker.SelectedItem is not Models.EntityBase selectedEntity)
            {
                await CrudContext.UI.ShowAlert("Info", "Select the item before the transition", "OK");
                return;
            }

            if (!(bool)CrudContext.Access.GetType().GetMethod("CanView").MakeGenericMethod(foreignattr.ForeignType).Invoke(CrudContext.Access, null))
            {
                await CrudContext.UI.ShowAlert("Access is denied", "No permission to view", "OK");
                return;
            }

            var pageType = typeof(EntityDetailPage<>).MakeGenericType(foreignattr.ForeignType);
            var page = (Page)Activator.CreateInstance(pageType, selectedEntity);
            await Navigation.PushAsync(page);
            
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




    public HorizontalStackLayout GetCheckbox(PropertyInfo prop, object val)
    {
        var checkbox = new CheckBox { IsChecked = val is bool b && b };
        checkboxes[prop] = checkbox;
        return new HorizontalStackLayout { Children = { new Label { Text = $"{prop.Name}" }, checkbox } };
    }

    public Picker GetEnumerate(PropertyInfo prop, object val)
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

    public Grid GetDigital(PropertyInfo prop, ref object val)
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

    public Grid GetEntry(PropertyInfo prop, ref object val)
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


    public static Label GetLabel(PropertyInfo prop, ref object val)
    {
        if (val == null || val.ToString() == null) val = "--";
        return new Label { Text = $"{SplitPascalCase(prop.Name)}: {val}", FontAttributes = FontAttributes.Bold };
    }

    public async void Save()
    {
        try
        {
            foreach (var (key, val) in entries)
                key.SetValue(Entity, val.Text);
            foreach (var (key, val) in numeric)
                key.SetValue(Entity, double.Parse(val.Text));
            foreach (var (key, val) in pickers)
                key.SetValue(Entity, val.SelectedItem);
            foreach (var (key, val) in checkboxes)
                key.SetValue(Entity, val.IsChecked);
            foreach (var (key, val) in files)
                key.SetValue(Entity, val?.ToString());
            foreach (var (key, val) in foreigns)
            {
                if (val.SelectedItem is Models.EntityBase entity)
                    key.SetValue(Entity, entity.Id);
            }
            foreach (var (key, ids) in manytomany)
            {
                key.SetValue(Entity, string.Join(",", ids));
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

    public void Refresh()
    {
        Content = BuildView();
    }

    public async void Delete()
    {
        var answer = await CrudContext.UI.ShowConfirm("Delete", "Are you sure?", "Yes", "Noooo");
        if (!answer) return;
        CrudContext.Database.Connection.Delete(Entity);
        CrudContext.Database.Connection.Insert(new Models.Audit { Name = $"{CrudContext.CurrentUser?.Name ?? "Unknown"} - Delete {Entity.Name}", Description = $"{typeof(T)}" });
        await Navigation.PopAsync();
    }

    public static string SplitPascalCase(string input)
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
