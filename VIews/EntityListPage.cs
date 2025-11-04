using System.Reflection;

namespace AutoGenCrudLib.Views;

public class EntityListPage<T> : ContentPage where T : new()
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
        var clearButton = new Button { Text = "Clear All", BackgroundColor = Colors.Red };

        refreshButton.Clicked += (_, __) => Refresh();
        addButton.Clicked += (_, __) => Add();
        clearButton.Clicked += async (_, __) => await ClearAll();
        addButton.IsEnabled = CrudContext.Access.CanCreate<T>();
        clearButton.IsEnabled = CrudContext.Access.CanDelete<T>();
        var buttonPanel = new HorizontalStackLayout
        {
            Padding = new Thickness(10, 5),
            Spacing = 10,
            Children = { refreshButton, addButton, clearButton }
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
        CrudContext.Database.Connection.Insert(new T());
        Refresh();
    }

    private async Task ClearAll()
    {
        var answer = await DisplayAlert("Delete", "Are you sure?", "Yes", "No");
        if (!answer) return;

        CrudContext.Database.Connection.DeleteAll<T>();
        Refresh();

        await DisplayAlert("Delete", "Delete completed", "OK");
    }
}
