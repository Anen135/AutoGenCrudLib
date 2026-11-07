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
* Flexible integration with providers (interfaces + ready-to-use base classes):

  * `IDatabaseProvider` — implemented by `DatabaseProvider`
  * `IAccessControlProvider` — implemented by `AccessControlBase`
  * `IUIProvider` — implemented by `UIBase`

---

## Installation

```bash
dotnet add package SP1
```

---

## Initialization

Before use, initialize the library context (for example in `MauiProgram.CreateMauiApp`):

```csharp
using AutoGenCrudLib;
using AutoGenCrudLib.Abstractions;

var database = new DatabaseProvider("crud.db3");
database.CreateForeign<Category>(); // register each [Foreign]/[ManyToMany] target table
database.CreateForeign<Tag>();

var ui = new UIBase(
    showAlert: async (title, message, confirm) =>
        await Application.Current.MainPage.DisplayAlert(title, message, confirm),
    showConfirm: async (title, message, confirm, cancel) =>
        await Application.Current.MainPage.DisplayAlert(title, message, confirm, cancel));

CrudContext.Init(
    database: database,
    access: new AccessControlBase(),
    ui: ui,
    currentUser: user); // optional, used for the audit trail
```

`CrudContext.Init` parameters (all required except `currentUser`):

* `database: DatabaseProvider` — SQLite connection and foreign-key maps (`Connection`, `ForeignMap`, `CreateForeign<T>()`)
* `access: AccessControlBase` — access control management; subclass it and override `CanCreate<T>`, `CanView<T>`, `CanEdit<T>`, `CanDelete<T>`, `CanFilter<T>` (all return `true` by default)
* `ui: UIBase` — user interaction (`ShowAlert`, `ShowConfirm`); pass your `DisplayAlert` delegates to its constructor
* `currentUser: EntityBase` (optional) — recorded in `Audit` entries as the acting user

The provider types implement the following interfaces:

* `IDatabaseProvider` — database access (`ForeignMap`, `Connection`)
* `IAccessControlProvider` — access control management
* `IUIProvider` — user interaction (e.g., `ShowAlert`, `ShowConfirm`)

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
├── VIews/                      (note: the folder is spelled "VIews" in the repo)
│   ├── EntityListPage.cs
│   ├── EntityDetailPage.cs
│   ├── EntityListView.cs
│   └── ... (FilterEditorPage, UniversalSearchPage, PDF/QR pages, ...)
│
├── Abstractions/
│   └── Abstractions.cs   (IDatabaseProvider, DatabaseProvider,
│                          IAccessControlProvider, AccessControlBase,
│                          IUIProvider, UIBase)
│
├── Models/
│   ├── EntityBase.cs
│   └── Audit.cs
│
├── Extensions/
│   ├── ManyToManyExtensions.cs  (namespace SP12.Model)
│   └── Filters/…
│
└── CrudContext.cs
```

---

## Dependencies

* **.NET MAUI**
* **SQLite-net** (`sqlite-net-pcl`)
* Your custom provider implementations (or use the built-in base classes):

  * `IDatabaseProvider` / `DatabaseProvider`
  * `IAccessControlProvider` / `AccessControlBase`
  * `IUIProvider` / `UIBase`

---

## License

**GPLv3 License** — This library is distributed under the terms of the GNU General Public License v3.
If you distribute a project using this library, you are required to make your source code publicly available under the same license.
