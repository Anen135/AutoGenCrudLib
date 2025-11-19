using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.Reflection;

namespace AutoGenCrudLib.Views;



public class EntityPdfDocument<T> : IDocument
    where T : AutoGenCrudLib.Models.EntityBase
{
    private T Entity;
    private PropertyInfo[] Props;
    private Dictionary<PropertyInfo, Picker> Foreigns;
    private Dictionary<PropertyInfo, List<int>> ManyToMany;

    public EntityPdfDocument(
        T entity,
        PropertyInfo[] props,
        Dictionary<PropertyInfo, Picker> foreigns,
        Dictionary<PropertyInfo, List<int>> manyToMany)
    {
        Entity = entity;
        Props = props;
        Foreigns = foreigns;
        ManyToMany = manyToMany;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);

            page.Header().Text($"{typeof(T).Name} (ID: {Entity.Id})")
                .FontSize(20).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

            page.Content().Column(col =>
            {
                foreach (var prop in Props)
                {
                    var value = GetValue(prop);

                    col.Item().Text(text =>
                    {
                        text.Span($"{Split(prop.Name)}: ").SemiBold();
                        text.Span(value ?? "--");
                    });
                }
            });

            page.Footer().AlignRight().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }

    private string? GetValue(PropertyInfo prop)
    {
        object? raw = prop.GetValue(Entity);

        if (raw == null)
            return null;

        // Many-to-many
        if (ManyToMany.TryGetValue(prop, out var ids))
        {
            var items = CrudContext.Database.ForeignMap[prop.GetCustomAttribute<AutoGenCrudLib.Attributes.ManyToManyAttribute>()!.ForeignType]();
            return string.Join(", ", ids.Select(id => items.FirstOrDefault(x => x.Id == id)?.Name));
        }

        // Foreign key
        if (Foreigns.TryGetValue(prop, out var picker))
        {
            if (picker.SelectedItem is AutoGenCrudLib.Models.EntityBase e)
                return e.Name;
            return raw.ToString();
        }

        return raw.ToString();
    }

    private static string Split(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new System.Text.StringBuilder();
        sb.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) && !char.IsUpper(input[i - 1]))
                sb.Append(' ');

            sb.Append(input[i]);
        }
        return sb.ToString();
    }
}
