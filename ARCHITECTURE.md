# PaperlessDashBlazor Architecture & Flow Diagrams

This document outlines the class hierarchy, relationships, and execution stacks for key user interactions across the Blazor UI, service layer, Paperless-NGX REST API, and SQLite database.

---

## 1. Class Structure & Relationships

The following class diagram shows the system layers: **Presentation (Blazor Components)**, **Card & Query Business Logic**, **Integration Services**, and **Database Entities (EF Core)**.

```mermaid
classDiagram
    direction TB

    namespace UI_Components {
        class NavMenu {
            -List~DynamicPageEntry~ _pages
            -bool _showAddModal
            -string _newPageTitle
            +AddPageAsync() Task
            +DeletePageAsync(Guid pageId) Task
        }

        class DynamicPage {
            +string Slug
            -DynamicPageEntry _page
            -Dictionary~Guid, DocCardModel~ _docCards
            -List~PaperlessTag~ _tags
            -PaperlessTag _selectedTag
            -PaperlessDocument _selectedDoc
            +LoadPageAsync() Task
            +OpenDocDialog() Task
            +ConfirmDocCard() Task
            +DeleteCard(Guid cardId) Task
        }

        class DocCard_Razor {
            +DocCardModel Card
            +EventCallback OnDelete
            +SelectDocAsync(PaperlessDocument doc) Task
        }
    }

    namespace Business_Logic {
        class DocCardModel {
            +Guid Id
            +string Title
            +PaperlessQuery Query
            +PaperlessDocument? SelectedDoc
            +string? SelectedDocThumbnail
            +event Action? OnChange
            +InitializeAsync() Task
            +SelectDocumentAsync(PaperlessDocument doc) Task
        }

        class PaperlessQuery {
            +Guid Id
            +QueryType Type
            +PaperlessTag? TagSelected
            +string? SearchText
            +int PageSize
            +List~PaperlessDocument~ Documents
            +int TotalCount
            +PaperlessDocument? DocSelected
            +string? SelectedDocThumbnail
            +bool IsLoading
            +event Action? OnChange
            -PaperlessService _paperless
            +RunQueryAsync() Task
            +SelectDocumentAsync(PaperlessDocument doc) Task
        }

        class QueryType {
            <<enumeration>>
            Tag
            FullText
        }
    }

    namespace Services_And_Data {
        class PageService {
            -IDbContextFactory~AppDbContext~ _dbFactory
            +event Action? OnChange
            +GetPagesAsync() Task~List~
            +GetBySlugAsync(string slug) Task~DynamicPageEntry~
            +AddPageAsync(string title) Task~DynamicPageEntry~
            +DeletePageAsync(Guid pageId) Task
            +AddDocCardAsync(Guid pageId, int docId, string docName) Task
            +DeleteCardAsync(Guid cardId) Task
        }

        class PaperlessService {
            -HttpClient _http
            +GetTagsAsync() Task~List~PaperlessTag~~
            +GetDocumentsByTagAsync(int tagId, int limit) Task
            +GetDocumentByIdAsync(int documentId) Task~PaperlessDocument~
            +GetDocumentThumbnailAsync(int documentId) Task~string~
            +GetDocumentUrl(int documentId) string
        }

        class AppDbContext {
            +DbSet~DynamicPageEntry~ Pages
            +DbSet~CardEntry~ Cards
        }

        class DynamicPageEntry {
            +Guid Id
            +string Title
            +string Slug
            +List~CardEntry~ Cards
        }

        class CardEntry {
            +Guid Id
            +CardSize Size
            +int? DocId
            +string? DocName
            +Guid PageId
            +DynamicPageEntry Page
        }

        class CardSize {
            <<enumeration>>
            Doc
            Table
        }
    }

    %% Relationships
    NavMenu ..> PageService : Injects & calls
    DynamicPage ..> PageService : Injects
    DynamicPage ..> PaperlessService : Injects
    DynamicPage "1" *-- "many" DocCardModel : Owns & creates
    DynamicPage ..> DocCard_Razor : Renders & passes Card
    DocCard_Razor o-- DocCardModel : Bound as [Parameter]
    DocCardModel "1" *-- "1" PaperlessQuery : Holds & delegates to
    PaperlessQuery ..> PaperlessService : Calls API methods
    PaperlessQuery ..> QueryType : Uses

    PageService ..> AppDbContext : Creates context via Factory
    AppDbContext o-- DynamicPageEntry : Maps
    AppDbContext o-- CardEntry : Maps
    DynamicPageEntry "1" *-- "many" CardEntry : Cascade delete
    CardEntry ..> CardSize : Uses
```

---

## 2. User Flows & Execution Stacks

### Flow A: User Creates a New Page

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant NavMenu as NavMenu.razor
    participant PageService as PageService (Scoped)
    participant DbFactory as IDbContextFactory<AppDbContext>
    participant SQLite as SQLite DB (paperless-dash.db)
    participant DynamicPage as DynamicPage.razor

    User->>NavMenu: Types title & clicks "Create"
    NavMenu->>PageService: AddPageAsync(title)
    PageService->>PageService: Generate unique slug
    PageService->>DbFactory: CreateDbContextAsync()
    PageService->>SQLite: INSERT INTO Pages (Id, Title, Slug)
    SQLite-->>PageService: Saved
    PageService->>PageService: OnChange?.Invoke() (triggers re-render)
    PageService-->>NavMenu: Returns DynamicPageEntry
    NavMenu->>User: Navigates to /pages/{slug}
    DynamicPage->>PageService: GetBySlugAsync(slug)
    PageService->>SQLite: SELECT * FROM Pages WHERE Slug = @slug
    SQLite-->>DynamicPage: Page model with Cards = []
    DynamicPage->>User: Renders empty page view
```

---

### Flow B: User Adds a 1×1 Doc Card

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Page as DynamicPage.razor
    participant Paperless as PaperlessService
    participant RestApi as Paperless-NGX REST API
    participant PageService as PageService
    participant SQLite as SQLite DB
    participant CardModel as DocCardModel
    participant Query as PaperlessQuery
    participant CardUI as DocCard.razor

    %% 1. Open dialog
    User->>Page: Clicks "Add Card" -> "Doc (1×1)"
    Page->>Paperless: GetTagsAsync()
    Paperless->>RestApi: GET /api/tags/?page_size=200
    RestApi-->>Paperless: JSON tags list
    Paperless-->>Page: List<PaperlessTag>
    Page->>User: Displays dialog with searchable tags list

    %% 2. Tag selection & preview
    User->>Page: Selects tag (e.g. "Passport")
    Page->>Paperless: GetDocumentsByTagAsync(tagId)
    Paperless->>RestApi: GET /api/documents/?tags__id__all={tagId}
    RestApi-->>Paperless: JSON matching documents
    Paperless-->>Page: List<PaperlessDocument>
    Page->>User: Shows document list inside dialog

    %% 3. Document picking & saving
    User->>Page: Selects specific document & clicks "Add Card"
    Page->>PageService: AddDocCardAsync(pageId, doc.Id, doc.Title)
    PageService->>SQLite: INSERT INTO Cards (Size="Doc", DocId, DocName, PageId)
    SQLite-->>PageService: Saved
    PageService->>PageService: OnChange?.Invoke()

    %% 4. Page reloads and constructs models
    Page->>Page: LoadAllDocCardsAsync()
    Page->>Query: new PaperlessQuery(PaperlessService) { TagSelected }
    Page->>CardModel: new DocCardModel(query, card.DocName)
    Page->>CardModel: InitializeAsync()
    CardModel->>Query: RunQueryAsync()
    Query->>Paperless: GetDocumentsByTagAsync(tagId)
    Paperless->>RestApi: GET /api/documents/?tags__id__all={tagId}
    RestApi-->>Query: Matched documents
    Query->>Paperless: GetDocumentThumbnailAsync(firstDoc.Id)
    Paperless->>RestApi: GET /api/documents/{id}/thumb/
    RestApi-->>Query: Base64 JPEG thumbnail
    Query->>CardModel: OnChange?.Invoke()
    CardModel->>CardUI: OnChange fires -> StateHasChanged()
    CardUI->>User: Renders DocCard with title, count, doc list & image
```

---

### Flow C: User Clicks a Different Document in the DocCard

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant CardUI as DocCard.razor
    participant CardModel as DocCardModel
    participant Query as PaperlessQuery
    participant Paperless as PaperlessService
    participant RestApi as Paperless-NGX REST API

    User->>CardUI: Clicks on a document row in the list
    CardUI->>CardModel: SelectDocumentAsync(doc)
    CardModel->>Query: SelectDocumentAsync(doc)
    Query->>Query: DocSelected = doc, SelectedDocThumbnail = null
    Query->>Query: NotifyChanged() -> CardUI renders loading placeholder
    Query->>Paperless: GetDocumentThumbnailAsync(doc.Id)
    Paperless->>RestApi: GET /api/documents/{doc.Id}/thumb/
    RestApi-->>Paperless: Binary JPEG bytes
    Paperless-->>Query: "data:image/jpeg;base64,..."
    Query->>Query: SelectedDocThumbnail = base64Data
    Query->>CardModel: NotifyChanged()
    CardModel->>CardUI: OnChange fires -> StateHasChanged()
    CardUI->>User: Selected row highlighted + new thumbnail displayed
```

