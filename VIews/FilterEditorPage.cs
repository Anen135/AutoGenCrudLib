using System.Reflection;

namespace AutoGenCrudLib.Views;

public class FilterEditorPage<T> : ContentPage where T : Models.EntityBase, new()
{
    private Action<Func<T, bool>, Func<IEnumerable<T>, IEnumerable<T>>> callBack;
    private StackLayout rows = new();
    private readonly StackLayout sortRows = new();

    public FilterEditorPage(Action<Func<T, bool>, Func<IEnumerable<T>, IEnumerable<T>>> applyCallback)
    {
        callBack = applyCallback;
        Title = $"Filter {typeof(T).Name}";

        var addButton = new Button { Text = "Add condition" };
        addButton.Clicked += (_, __) => AddFilterRow();

        var applyButton = new Button { Text = "Apply", BackgroundColor = Colors.Green };
        applyButton.Clicked += (_, __) => ApplyFilters();

        var clearButton = new Button { Text = "Clear", BackgroundColor = Colors.Gray };
        clearButton.Clicked += (_, __) => rows.Children.Clear();

        var sortingUI = BuildSortingUI();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Padding = 10,
                Children = { addButton, rows, sortingUI, applyButton, clearButton }
            }
        };
    }


    // ---------------------
    // Добавить строку фильтра
    // ---------------------
    private void AddFilterRow()
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var fieldPicker = new Picker { Title = "Field" };
        foreach (var p in props) fieldPicker.Items.Add(p.Name);

        var operatorPicker = new Picker { Title = "Op" };
        operatorPicker.Items.Add("=");
        operatorPicker.Items.Add(">");
        operatorPicker.Items.Add("<");
        operatorPicker.Items.Add("!");
        operatorPicker.Items.Add("contains");

        var valueEntry = new Entry { Placeholder = "Value" };

        var removeBtn = new Button
        {
            Text = "X",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            WidthRequest = 40
        };
        removeBtn.Clicked += (_, __) => rows.Children.Remove((View)removeBtn.Parent);

        rows.Children.Add(new HorizontalStackLayout
        {
            Spacing = 6,
            Children = { fieldPicker, operatorPicker, valueEntry, removeBtn }
        });
    }

    private View BuildSortingUI()
    {
        var addSortButton = new Button { Text = "Add sort field" };
        addSortButton.Clicked += (_, __) => AddSortRow();

        return new VerticalStackLayout
        {
            Children =
            {
                addSortButton,
                sortRows
            }
        };
    }

    private void AddSortRow()
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var fieldPicker = new Picker { Title = "Field" };
        foreach (var p in props) fieldPicker.Items.Add(p.Name);

        var directionPicker = new Picker { Title = "Direction" };
        directionPicker.Items.Add("ASC");
        directionPicker.Items.Add("DESC");

        var removeBtn = new Button
        {
            Text = "X",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            WidthRequest = 40
        };
        removeBtn.Clicked += (_, __) => sortRows.Children.Remove((View)removeBtn.Parent);

        sortRows.Children.Add(new HorizontalStackLayout
        {
            Spacing = 6,
            Children = { fieldPicker, directionPicker, removeBtn }
        });
    }


    // ---------------------
    // Применить фильтры
    // ---------------------
    private void ApplyFilters()
    {
        var filters = BuildFilterFunction();
        var sorting = BuildSortingFunction();

        Func<T, bool> predicate = x => filters.All(f => f(x));
        callBack(predicate, sorting);
        Navigation.PopAsync();
    }

    private List<Func<T, bool>> BuildFilterFunction()
    {
        var filters = new List<Func<T, bool>>();
        foreach (HorizontalStackLayout row in rows.Children)
        {
            var field = ((Picker)row.Children[0]).SelectedItem?.ToString();
            var op = ((Picker)row.Children[1]).SelectedItem?.ToString();
            var val = ((Entry)row.Children[2]).Text;

            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(op))
                continue;

            var prop = typeof(T).GetProperty(field);
            if (prop == null) continue;

            // ----- Текст -----
            if (prop.PropertyType == typeof(string))
            {
                if (op == "contains")
                    filters.Add(x => ((string)prop.GetValue(x) ?? "").Contains(val, StringComparison.OrdinalIgnoreCase));

                if (op == "=")
                    filters.Add(x => ((string)prop.GetValue(x) ?? "") == val);

                continue;
            }

            // ----- int -----
            if ((prop.PropertyType == typeof(int?) || prop.PropertyType == typeof(int)) && int.TryParse(val, out int intVal))
            {
                if (op == "=") filters.Add(x => (int)prop.GetValue(x) == intVal);
                if (op == ">") filters.Add(x => (int)prop.GetValue(x) > intVal);
                if (op == "<") filters.Add(x => (int)prop.GetValue(x) < intVal);
                continue;
            }

            // ----- double -----
            if (prop.PropertyType == typeof(double) && double.TryParse(val, out double dblVal))
            {
                if (op == "=") filters.Add(x => (double)prop.GetValue(x) == dblVal);
                if (op == ">") filters.Add(x => (double)prop.GetValue(x) > dblVal);
                if (op == "<") filters.Add(x => (double)prop.GetValue(x) < dblVal);
                continue;
            }


            // ----- bool -----
            if (prop.PropertyType == typeof(bool) && bool.TryParse(val, out bool boolVal))
            {
                if (op == "=") filters.Add(x => (bool)prop.GetValue(x) == boolVal);
                if (op == "!") filters.Add(x => (bool)prop.GetValue(x) != boolVal);
            }
        }
        return filters;
    }

    private Func<IEnumerable<T>, IEnumerable<T>> BuildSortingFunction()
    {
        var sortList = new List<(PropertyInfo prop, bool asc)>();

        foreach (HorizontalStackLayout row in sortRows.Children)
        {
            var field = ((Picker)row.Children[0]).SelectedItem?.ToString();
            var dir = ((Picker)row.Children[1]).SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(dir))
                continue;

            var prop = typeof(T).GetProperty(field);
            if (prop == null) continue;

            bool asc = dir == "ASC";
            sortList.Add((prop, asc));
        }

        // Если сортировок нет — возвращаем как есть
        if (sortList.Count == 0)
            return items => items;

        return items =>
        {
            IOrderedEnumerable<T> ordered = null;

            for (int i = 0; i < sortList.Count; i++)
            {
                var (prop, asc) = sortList[i];

                if (i == 0)
                {
                    ordered = asc
                        ? items.OrderBy(x => prop.GetValue(x))
                        : items.OrderByDescending(x => prop.GetValue(x));
                }
                else
                {
                    ordered = asc
                        ? ordered.ThenBy(x => prop.GetValue(x))
                        : ordered.ThenByDescending(x => prop.GetValue(x));
                }
            }

            return ordered!;
        };
    }

}
