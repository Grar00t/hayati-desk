# HayatiDesk

**Offline-first life organizer and local infographic engine.** 
Zero cloud dependencies. Zero telemetry. Strict local execution.

## Architectural Scope
HayatiDesk is a high-performance Windows desktop application engineered for deterministic local execution. It leverages .NET 10 LTS, WPF, and a strict SQLite WAL backend to manage, visualize, and organize complex local datasets without network reliance.

## Core Stack
- **Runtime**: .NET 10 (LTS) / C# 13
- **UI Framework**: WPF (Windows Presentation Foundation)
- **State Management**: CommunityToolkit.Mvvm (Source Generators)
- **Persistence**: SQLite (`Microsoft.Data.Sqlite`) with Write-Ahead Logging (WAL)
- **Visualization**: LiveCharts2 (SkiaSharp) for native rendering, WebView2 for complex local HTML/JS infographics.

## Engineering Directives
1. **Strict MVVM**: Zero code-behind. All UI state is managed via `[ObservableProperty]` and `[RelayCommand]`.
2. **Resource Safety**: Explicit disposal of all `SqliteConnection`, `WebView2`, and SkiaSharp surfaces.
3. **Asynchronous I/O**: All database and file system operations utilize `IAsyncEnumerable<T>` and `async/await` to prevent UI thread starvation.
4. **Air-Gapped Security**: No external network requests. All web assets for WebView2 are bundled locally.

## Repository Structure
```text
hayati-desk/
├── src/HayatiDesk/          # WPF Application, ViewModels, Views, Services
├── tests/HayatiDesk.Tests/  # xUnit integration and unit tests
├── assets/                  # Local fonts, templates, and vendor JS/CSS
└── docs/                    # Architecture Decision Records (ADRs)
