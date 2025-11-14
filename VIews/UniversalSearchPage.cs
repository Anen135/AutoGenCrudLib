using System.Reflection;
using AutoGenCrudLib.Models;
using AutoGenCrudLib.Attributes;

namespace AutoGenCrudLib.Views;

public class UniversalSearchPage : ContentPage
{
    private Picker modelPicker;
    private Picker propertyPicker;
    private Entry textEntry;
    private Picker enumPicker;
    private CheckBox boolCheck;
    private Picker foreignPicker;
    private Button searchButton;
    private CollectionView resultView;

    private Type selectedModel;
    private PropertyInfo selectedProperty;

    public UniversalSearchPage()
    {
        Title = "Универсальный поиск";
        BuildView();
    }

    private void BuildView()
    {
        modelPicker = new Picker { Title = "Выберите модель" };
        propertyPicker = new Picker { Title = "Выберите свойство" };

        // Подготовка списка моделей
        var allModels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(EntityBase)) && !t.IsAbstract)
            .ToList();

        modelPicker.ItemsSource = allModels.Select(m => m.Name).ToList();

        modelPicker.SelectedIndexChanged += (_, __) =>
        {
            if (modelPicker.SelectedIndex == -1) return;
            selectedModel = allModels[modelPicker.SelectedIndex];
            LoadProperties(selectedModel);
        };

        propertyPicker.SelectedIndexChanged += (_, __) =>
        {
            if (propertyPicker.SelectedIndex == -1 || selectedModel == null) return;
            selectedProperty = selectedModel.GetProperties()[propertyPicker.SelectedIndex];
            ShowPropertyInput(selectedProperty);
        };

        searchButton = new Button
        {
            Text = "Найти",
            BackgroundColor = Colors.Purple,
            TextColor = Colors.White
        };
        searchButton.Clicked += (_, __) => DoSearch();

        resultView = new CollectionView
        {
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
            ItemTemplate = new DataTemplate(() =>
            {
                var nameLabel = new Label { FontAttributes = FontAttributes.Bold };
                nameLabel.SetBinding(Label.TextProperty, "Name");
                return new Border
                {
                    Content = nameLabel,
                    Margin = new Thickness(10, 5),
                    BackgroundColor = Colors.LightGray,
                };
            })
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(15),
                Spacing = 10,
                Children =
                {
                    modelPicker,
                    propertyPicker,
                    new Label { Text = "Значение свойства:" },
                    new ContentView { Content = new Label { Text = "Выберите модель и поле..." }, AutomationId = "InputContainer" },
                    searchButton,
                    new Label { Text = "Результаты поиска:" },
                    resultView
                }
            }
        };
    }

    private void LoadProperties(Type modelType)
    {
        var props = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        propertyPicker.ItemsSource = props.Select(p => p.Name).ToList();
    }

    private void ShowPropertyInput(PropertyInfo prop)
    {
        var container = ((ContentView)((VerticalStackLayout)((ScrollView)Content).Content).Children
            .FirstOrDefault(c => c is ContentView cv && cv.AutomationId == "InputContainer"))!;
        View inputControl;

        if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(double))
        {
            textEntry = new Entry { Placeholder = "Введите значение..." };
            inputControl = textEntry;
        }
        else if (prop.PropertyType == typeof(bool))
        {
            boolCheck = new CheckBox();
            inputControl = new HorizontalStackLayout
            {
                Children = { new Label { Text = "Да/Нет", VerticalOptions = LayoutOptions.Center }, boolCheck }
            };
        }
        else if (prop.PropertyType.IsEnum)
        {
            enumPicker = new Picker
            {
                ItemsSource = Enum.GetValues(prop.PropertyType),
                ItemDisplayBinding = new Binding(".")
            };
            inputControl = enumPicker;
        }
        else if (prop.GetCustomAttribute<ForeignAttribute>() is ForeignAttribute foreignAttr)
        {
            var items = CrudContext.Database.ForeignMap[foreignAttr.ForeignType]();
            foreignPicker = new Picker
            {
                ItemsSource = items,
                ItemDisplayBinding = new Binding("Name"),
                Title = $"Выберите {prop.Name}"
            };
            inputControl = foreignPicker;
        }
        else
        {
            inputControl = new Label { Text = "Тип свойства не поддерживается для поиска." };
        }

        container.Content = inputControl;
    }

    private void DoSearch()
    {
        if (selectedModel == null || selectedProperty == null)
        {
            DisplayAlert("Ошибка", "Выберите модель и свойство", "OK");
            return;
        }

        var tableMethod = typeof(SQLite.SQLiteConnection).GetMethod("Table")!
            .MakeGenericMethod(selectedModel);
        var table = (IEnumerable<object>)tableMethod.Invoke(CrudContext.Database.Connection, null)!;

        IEnumerable<object> results = table;

        if (selectedProperty.PropertyType == typeof(string))
        {
            var val = textEntry?.Text ?? "";
            results = table.Where(e => (selectedProperty.GetValue(e)?.ToString() ?? "").Contains(val, StringComparison.OrdinalIgnoreCase));
        }
        else if (selectedProperty.PropertyType == typeof(double))
        {
            if (double.TryParse(textEntry?.Text, out double val))
                results = table.Where(e => Math.Abs(Convert.ToDouble(selectedProperty.GetValue(e)) - val) < 0.0001);
        }
        else if (selectedProperty.PropertyType == typeof(bool))
        {
            var val = boolCheck?.IsChecked ?? false;
            results = table.Where(e => (bool?)selectedProperty.GetValue(e) == val);
        }
        else if (selectedProperty.PropertyType.IsEnum)
        {
            var val = enumPicker?.SelectedItem;
            if (val != null)
                results = table.Where(e => Equals(selectedProperty.GetValue(e), val));
        }
        else if (selectedProperty.GetCustomAttribute<ForeignAttribute>() is ForeignAttribute)
        {
            if (foreignPicker?.SelectedItem is EntityBase selected)
                results = table.Where(e => (int?)selectedProperty.GetValue(e) == selected.Id);
        }

        resultView.ItemsSource = results.Cast<EntityBase>().ToList();
    }
}
