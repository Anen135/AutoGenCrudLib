using SQLite;
namespace AutoGenCrudLib.Models;

public class EntityBase
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = "Default Description";
    [Attributes.Freeze]
    public string CreatedAt { get; set; } = DateTime.Now.ToString();
}
