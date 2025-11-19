namespace AutoGenCrudLib.Views;

public static class EntityDetailPageFactory
{
    // по умолчанию возвратит PDF-страницу
    public static Func<object, Page> CreatePage =
        entity => (Page)Activator.CreateInstance(typeof(EntityDetailPage<>).MakeGenericType(entity.GetType()), entity)!;
}
