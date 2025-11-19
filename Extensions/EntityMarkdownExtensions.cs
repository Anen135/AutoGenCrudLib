using System.Reflection;
using System.Text;

namespace AutoGenCrudLib.Extensions;

public static class EntityMarkdownExtensions
{
    public static string ToMarkdown<T>(this T entity)
    {
        if (entity == null)
            return "# Null Entity";

        var sb = new StringBuilder();

        sb.AppendLine($"# {typeof(T).Name} Details");
        sb.AppendLine();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanRead) continue;

            var value = prop.GetValue(entity);
            string text = value?.ToString() ?? "—";

            sb.AppendLine($"**{prop.Name}**: {text}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
