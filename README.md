# AutoGenCrudLib

**Automatic CRUD Interface Generation for .NET MAUI Applications**

AutoGenCrudLib is a lightweight library designed to automate the creation of CRUD (Create, Read, Update, Delete) interfaces and logic based on data models. It simplifies working with SQLite, MAUI Pages, and relationships between entities.

---

## Features

* Automatic generation of:

  * List pages (`EntityListPage<T>`)
  * Detail pages (`EntityDetailPage<T>`)
* Attribute support:

  * `[Foreign]` — defines a foreign key
  * `[ManyToMany]` — defines a many-to-many relationship
  * `[Freeze]` — marks a field as read-only
* Simplified record creation, editing, and deletion
* Automatic database binding through `CrudContext`
* Flexible integration with providers:

  * `IDatabaseProvider`
  * `IAccessControlProvider`
  * `IUIProvider`

---

## Installation

```bash
dotnet add package SP1
```

---

## Initialization

Before use, initialize the library context:

```csharp
CrudContext.Init(
    database: new MyDatabaseProvider(),
    access: new MyAccessControlProvider(),
    ui: new MyUIProvider()
);
```

These providers implement the following interfaces:

* `IDatabaseProvider` — database access
* `IAccessControlProvider` — access control management
* `IUIProvider` — user interaction (e.g., `DisplayAlert`, `DisplayConfirm`)

---

## Attributes

### `[Foreign(Type foreign)]`

Specifies a foreign key reference to another entity.

```csharp
[Foreign(typeof(Category))]
public int CategoryId { get; set; }
```

In the UI, this field is automatically displayed as a `Picker` populated with values from the related table.

---

### `[Freeze]`

Marks a field as read-only in the UI.

```csharp
[Freeze]
public string Code { get; set; }
```

---

### `[ManyToMany(Type foreign)]`

Defines a many-to-many relationship.
Data is stored as a comma-separated string of IDs.

```csharp
[ManyToMany(typeof(Tag))]
public string Tags { get; set; } = "";
```

You can work with such properties through extension methods:

```csharp
var tags = post.GetManyToManyList<Tag>("Tags");
post.SetManyToManyList("Tags", selectedTags);
```

---

## Pages

### `EntityListPage<T>`

Displays a list of `T` entities with options to add, edit, and delete records.

```csharp
await Navigation.PushAsync(new EntityListPage<Product>());
```

Buttons such as “Add,” “Delete,” and “Refresh” are automatically managed through the `AccessControlProvider`.

---

### `EntityDetailPage<T>`

Automatically generates an editable form for entity `T`.

```csharp
await Navigation.PushAsync(new EntityDetailPage<Product>(product));
```

Supports:

* Text fields (`string`, `double`)
* Enumerations (`enum`)
* Foreign keys (`[Foreign]`)
* Read-only fields (`[Freeze]`)

---

## Many-to-Many Extensions

Located in the `SP12.Model` namespace:

```csharp
entity.GetManyToManyList("Tags");
entity.SetManyToManyList("Tags", listOfTags);
```

These methods support both `EntityBase` and generic versions `<T>`.

---

## Example Usage

```csharp
public class Product : EntityBase
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }

    [Foreign(typeof(Category))]
    public int CategoryId { get; set; }

    [ManyToMany(typeof(Tag))]
    public string Tags { get; set; } = "";
}
```

```csharp
// Open list page
await Navigation.PushAsync(new EntityListPage<Product>());
```

---

## Project Structure

```
AutoGenCrudLib/
│
├── Attributes/
│   ├── ForeignAttribute.cs
│   ├── FreezeAttribute.cs
│   └── ManyToManyAttribute.cs
│
├── Views/
│   ├── EntityListPage.cs
│   ├── EntityDetailPage.cs
│   └── EntityListView.cs
│
├── CrudContext.cs
└── Models/ (extensions)
```

---

## Dependencies

* **.NET MAUI**
* **SQLite-net** (via `SQLiteAttribute`)
* Your custom provider implementations:

  * `IDatabaseProvider`
  * `IAccessControlProvider`
  * `IUIProvider`

---

## License

**GPLv3 License** — This library is distributed under the terms of the GNU General Public License v3.
If you distribute a project using this library, you are required to make your source code publicly available under the same license.
