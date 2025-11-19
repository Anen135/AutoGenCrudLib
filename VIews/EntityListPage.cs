using AutoGenCrudLib.Models;
using System.Reflection;


namespace AutoGenCrudLib.Views;

public class EntityListPage<T> : ContentPage where T : EntityBase, new()
{
    private CollectionView CV;

    public EntityListPage()
    {
        Title = $"{typeof(T).Name} List";
        var items = CrudContext.Database.Connection.Table<T>().ToList();
        CV = new CollectionView
        {
            ItemsSource = items,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
            {
                ItemSpacing = 8
            },
            ItemTemplate = new DataTemplate(() => new EntityListView<T>())
        };
        var refreshButton = new Button { Text = "Refresh" };
        var addButton = new Button { Text = "Add" };
        var filterButton = new Button { Text = "Filter" };

        var clearButton = new Button { Text = "Clear All", BackgroundColor = Colors.Red };
        var exportButton = new Button { Text = "Export", BackgroundColor = Colors.Green };
        var importButton = new Button { Text = "Import", BackgroundColor = Colors.Green };

        refreshButton.Clicked += (_, __) => Refresh();
        addButton.Clicked += (_, __) => Add();
        filterButton.Clicked += async (_, __) => await OpenFilterPage();
        exportButton.Clicked += async (_, __) => await ExportCsv();
        importButton.Clicked += async (_, __) => await ImportCsv();
        clearButton.Clicked += async (_, __) => await ClearAll();

        addButton.IsVisible = CrudContext.Access.CanCreate<T>();
        clearButton.IsVisible = CrudContext.Access.CanDelete<T>();
        importButton.IsVisible = CrudContext.Access.CanEdit<T>() && addButton.IsVisible && clearButton.IsVisible;
        filterButton.IsVisible = CrudContext.Access.CanFilter<T>();
        var buttonPanel = new HorizontalStackLayout
        {
            Padding = new Thickness(10, 5),
            Spacing = 10,
            Children = { refreshButton, addButton, clearButton, exportButton, importButton, filterButton }
        };
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        grid.Add(buttonPanel, 0, 0);
        grid.Add(CV, 0, 1);

        Content = grid;
    }
    private void Refresh()
    {
        CV.ItemsSource = CrudContext.Database.Connection.Table<T>().ToList();
    }

    private void Add()
    {
        var entity = new T();
        CrudContext.Database.Connection.Insert(entity);
        CrudContext.Database.Connection.Insert(new Audit { Name = $"{CrudContext.CurrentUser?.Name ?? "Unknown"} - Add {entity.Name}", Description = $"{typeof(T)}" });
        Refresh();
    }

    private async Task ClearAll()
    {
        var answer = await DisplayAlert("Delete", "Are you sure?", "Yes", "No");
        if (!answer) return;

        CrudContext.Database.Connection.DeleteAll<T>();
        CrudContext.Database.Connection.Insert(new Audit { Name = $"{CrudContext.CurrentUser?.Name ?? "Unknown"} - ClearAll", Description = $"{typeof(T)}" });
        Refresh();

        await DisplayAlert("Delete", "Delete completed", "OK");
    }

    private async Task OpenFilterPage()
    {
        await Navigation.PushAsync(new FilterEditorPage<T>((Func<T, bool> predicate, Func<IEnumerable<T>, IEnumerable<T>> sorting) => CV.ItemsSource = sorting(CrudContext.Database.Connection.Table<T>().ToList().Where(predicate)).ToList()));
    }

    public void ApplyFilter(Func<T, bool> predicate, Func<IEnumerable<T>, IEnumerable<T>> sorting)
    {
        CV.ItemsSource = sorting(CrudContext.Database.Connection.Table<T>().ToList().Where(predicate)).ToList();
    }

    private async Task ShareScv()
    {
        try
        {
            var items = CrudContext.Database.Connection.Table<T>().ToList();
            if (items.Count == 0)
            {
                await DisplayAlert("Export", "No data to export", "OK");
                return;
            }
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Join(";", props.Select(p => p.Name)));

            foreach (var item in items)
            {
                var values = props.Select(p =>
                {
                    var val = p.GetValue(item);
                    return val?.ToString()?.Replace(";", ",") ?? "";
                });

                sb.AppendLine(string.Join(";", values));
            }

            var fileName = $"{typeof(T).Name}_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            File.WriteAllText(filePath, sb.ToString());

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export CSV",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"CSV export failed: {ex.Message}", "OK");
        }
    }

    private async Task ExportCsv()
    {
        try
        {
            var items = CrudContext.Database.Connection.Table<T>().ToList();
            if (items.Count == 0)
            {
                await DisplayAlert("Export", "No data to export", "OK");
                return;
            }

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var sb = new System.Text.StringBuilder();

            // ------ Заголовки ------
            sb.AppendLine(string.Join(";", props.Select(p => p.Name)));

            // ------ Строки ------
            foreach (var item in items)
            {
                var values = new List<string>();

                foreach (var prop in props)
                {
                    object rawVal = prop.GetValue(item);

                    // ------------------------------------------
                    // 1. MANY-TO-MANY ATTRIBUTE
                    // ------------------------------------------
                    if (prop.GetCustomAttribute<Attributes.ManyToManyAttribute>() is Attributes.ManyToManyAttribute mmAttr)
                    {
                        var ids = (rawVal as string ?? "")
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(idStr => int.TryParse(idStr, out var id) ? id : -1)
                            .Where(id => id > 0)
                            .ToList();

                        var foreignItems = CrudContext.Database.ForeignMap[mmAttr.ForeignType]();
                        var names = foreignItems
                            .Where(f => ids.Contains(f.Id))
                            .Select(f => f.Name)
                            .ToList();

                        values.Add(string.Join(",", names));
                        continue;
                    }

                    // ------------------------------------------
                    // 2. FOREIGN KEY ATTRIBUTE
                    // ------------------------------------------
                    if (prop.GetCustomAttribute<Attributes.ForeignAttribute>() is Attributes.ForeignAttribute foreignAttr)
                    {
                        int id = rawVal is int rid ? rid : 0;
                        var list = CrudContext.Database.ForeignMap[foreignAttr.ForeignType]();

                        var entity = list.FirstOrDefault(x => x.Id == id);
                        values.Add(entity?.Name ?? "");
                        continue;
                    }

                    // ------------------------------------------
                    // 3. NORMAL FIELDS
                    // ------------------------------------------
                    if (rawVal == null)
                    {
                        values.Add("");
                        continue;
                    }

                    // Чтобы ; не ломал формат
                    values.Add(rawVal.ToString()!.Replace(";", ","));
                }

                sb.AppendLine(string.Join(";", values));
            }

            // ---------- ПУТЬ К ЗАГРУЗКАМ ----------
            string downloads;

#if ANDROID
            downloads = "/storage/emulated/0/Download";
#elif WINDOWS
        downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads"
        );
#elif IOS || MACCATALYST
        downloads = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#else
        downloads = FileSystem.AppDataDirectory;
#endif

            // Создаём файл
            var fileName = $"{typeof(T).Name}_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(downloads, fileName);

            File.WriteAllText(filePath, sb.ToString());

            await DisplayAlert("Export", $"Saved:\n{filePath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task ImportCsv()
    {
        try
        {
            // 1. Выбор файла
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select CSV file",
                FileTypes = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.comma-separated-values-text" } },
                    { DevicePlatform.Android, new[] { "text/csv" } },
                    { DevicePlatform.WinUI, new[] { ".csv" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text" } }
                })
            });


            if (result == null) return; // пользователь отменил

            var text = await File.ReadAllTextAsync(result.FullPath);

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                await DisplayAlert("Import", "CSV file is empty or invalid", "OK");
                return;
            }

            // 2. Заголовки
            var headers = lines[0].Split(';');
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p);
            CrudContext.Database.Connection.DeleteAll<T>();

            // 3. Парсим строки
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(';');
                var entity = new T();

                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    var header = headers[j].Trim();
                    var val = values[j].Trim();

                    if (!props.TryGetValue(header, out var prop)) continue;

                    // ------------------------------------------
                    // 1. MANY-TO-MANY ATTRIBUTE
                    // ------------------------------------------
                    if (prop.GetCustomAttribute<Attributes.ManyToManyAttribute>() is Attributes.ManyToManyAttribute mmAttr)
                    {
                        var foreignList = CrudContext.Database.ForeignMap[mmAttr.ForeignType]();
                        var names = val.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim());
                        var ids = foreignList.Where(f => names.Contains(f.Name)).Select(f => f.Id);
                        prop.SetValue(entity, string.Join(",", ids));
                        continue;
                    }

                    // ------------------------------------------
                    // 2. FOREIGN KEY ATTRIBUTE
                    // ------------------------------------------
                    if (prop.GetCustomAttribute<Attributes.ForeignAttribute>() is Attributes.ForeignAttribute foreignAttr)
                    {
                        var foreignList = CrudContext.Database.ForeignMap[foreignAttr.ForeignType]();
                        var foreignEntity = foreignList.FirstOrDefault(f => f.Name == val);
                        if (foreignEntity != null)
                            prop.SetValue(entity, foreignEntity.Id);
                        continue;
                    }

                    // ------------------------------------------
                    // 3. NORMAL FIELDS
                    // ------------------------------------------
                    if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(entity, val);
                    }
                    else if (prop.PropertyType == typeof(double))
                    {
                        if (double.TryParse(val, out var d))
                            prop.SetValue(entity, d);
                    }
                    else if (prop.PropertyType == typeof(bool))
                    {
                        if (bool.TryParse(val, out var b))
                            prop.SetValue(entity, b);
                    }
                    else if (prop.PropertyType.IsEnum)
                    {
                        try
                        {
                            var enumVal = Enum.Parse(prop.PropertyType, val);
                            prop.SetValue(entity, enumVal);
                        }
                        catch { }
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        if (int.TryParse(val, out var id))
                            prop.SetValue(entity, id);
                    }
                }

                // 4. Вставляем в базу
                CrudContext.Database.Connection.Insert(entity);
            }

            // 5. Обновляем UI
            Refresh();

            await DisplayAlert("Import", "CSV imported successfully", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"CSV import failed: {ex.Message}", "OK");
        }
    }
}
