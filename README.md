# Municipal Services App

A Windows Forms desktop application (C#, .NET Framework 4.8) that lets citizens report municipal issues, browse local events and announcements, and track service requests. It doubles as a practical demonstration of core data structures: an AVL tree, a min-heap and a directed graph.

Requests are saved to a local JSON file, so reports you submit appear in the status screen and survive restarts. There is no server or database.

## Features

### Report an Issue
- Location, category, priority and description, with inline validation and a character counter.
- Optional attachment (`.jpg`, `.jpeg`, `.png`, `.pdf`, `.doc`, `.docx`, max 10 MB) with an image preview.
- On submit the request gets a unique ID and is saved immediately.

### Local Events and Announcements
- Upcoming events with search (name, place, description), category filter and a "next 7 / 30 days" range.
- Detail panel for the selected event and **Add to calendar**, which exports an `.ics` file.
- Announcements tab with the latest municipal notices.

### Service Request Status
- Summary tiles: total, open, completed, urgent and open.
- Search by request ID (exact lookup in the AVL tree) or by place or keyword; filter by status; sort by ID, date or priority.
- Double-click a request (or **View / update**) to see its details, open the attachment, change its status and see related requests. Completing a request that still depends on open work asks for confirmation.
- Dependency graph with arrows, coloured by status. Selecting a request highlights it and everything connected to it; clicking a node selects the request.
- **Export CSV** of the current view.

## Data structures

| Structure | File | Used for |
|-----------|------|----------|
| AVL tree (self-balancing BST) | [AvlTree.cs](DataStructures/AvlTree.cs) | Requests keyed by ID; O(log n) lookup and an in-order (sorted) listing |
| Min-heap | [MinHeap.cs](DataStructures/MinHeap.cs) | Priority ordering via heap sort (most urgent first, ties by oldest ID) |
| Directed graph | [Graph.cs](DataStructures/Graph.cs) | Request dependencies: BFS, DFS, related-request search and cycle detection |

## Requirements

- Windows
- [Visual Studio 2019 or later](https://visualstudio.microsoft.com/) with the **.NET desktop development** workload (includes the .NET Framework 4.8 targeting pack)

## Build and run

With Visual Studio: open `MunicipalServicesApp.sln`, set **MunicipalServicesApp** as the startup project and press `F5`.

From a Developer Command Prompt:

```
msbuild MunicipalServicesApp.csproj /p:Configuration=Release
bin\Release\MunicipalServicesApp.exe
```

## Tests

Unit tests for the data structures and the repository live in `MunicipalServicesApp.Tests` (MSTest). Run them from Visual Studio's Test Explorer or with:

```
dotnet test MunicipalServicesApp.Tests
```

## Where data is stored

Requests are saved to `%AppData%\MunicipalServicesApp\requests.json`. Delete that file to reset to the sample data. If the file is ever corrupt, it is copied to `requests.json.bak` and sample data is restored.

## Project structure

```
Models/                Service request, event and announcement types
DataStructures/        AvlTree, MinHeap, Graph
Services/              RequestRepository (JSON persistence)
Theme.cs               Shared colours, fonts and control styling
MainMenu.cs            Landing screen
ReportIssuesForm.cs    Issue reporting
LocalEventsForm.cs     Events and announcements
ServiceRequestStatusForm.cs / RequestDetailsForm.cs   Request tracking
MunicipalServicesApp.Tests/   Unit tests
```

The UI is built in code (no designer files), so layouts resize with the window.

## Possible next steps

- Replace the JSON file with SQLite or a web API.
- Let users edit or delete requests and add dependencies when reporting.
- Add user accounts so people only see their own requests.

## Contributing

1. Fork the repository.
2. Create a branch: `git checkout -b feature-name`
3. Commit your changes: `git commit -m "Add new feature"`
4. Push: `git push origin feature-name`
5. Open a pull request.

## License

Released under the [MIT License](LICENSE).
