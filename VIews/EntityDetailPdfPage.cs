using AutoGenCrudLib.Extensions;
using QuestPDF.Fluent;
using QuestPDF.Markdown;

namespace AutoGenCrudLib.Views;

public class EntityDetailPdfPage<T> : EntityDetailPage<T> where T : Models.EntityBase, new()
{
    public EntityDetailPdfPage(T entity) : base(entity) { }

    public override View BuildView()
    {
        // получаем стандартный UI
        var root = base.BuildView() as ScrollView;
        var layout = root.Content as VerticalStackLayout;

        // кнопка PDF
        var pdfBtn = new Button
        {
            Text = "Export to PDF",
            BackgroundColor = Colors.LightGray,
            Padding = 10
        };

        pdfBtn.Clicked += async (_, __) => await GeneratePdf();

        // добавляем ниже всех кнопок
        layout.Add(pdfBtn);

        return root;
    }

    public virtual async Task GeneratePdf()
    {
        try
        {
            // Markdown-разметка сущности
            var markdown = Entity.ToMarkdown();

            // Подготовка QuestPDF
            var fileName = $"{typeof(T).Name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

            QuestPDF.Fluent.Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(30);
                    p.Content().Markdown(markdown);
                });
            })
            .GeneratePdf(tempPath);

            // Предложить открыть PDF
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(tempPath)
            });
        }
        catch (Exception ex)
        {
            await CrudContext.UI.ShowAlert("PDF Error", ex.Message, "OK");
        }
    }
}
