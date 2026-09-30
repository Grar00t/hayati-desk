# HayatiDesk

Offline-first Windows life organizer backed by local SQLite storage.

## Current implementation

The current source tree is a .NET 10 / WPF application. Its project file currently references:

- `CommunityToolkit.Mvvm` 8.4.2;
- `Microsoft.Data.Sqlite` 10.0.12.

Earlier design text referenced LiveCharts2/SkiaSharp and WebView2. Those package references have been removed from the current project and are therefore not part of the current runtime contract.

## Repository structure

```text
hayati-desk/
├── src/HayatiDesk/          # WPF application
├── tests/HayatiDesk.Tests/  # xUnit tests
└── .github/workflows/       # CI definition
```

The current source includes local SQLite persistence, repository/services code, view models, and WPF views.

## Build and test

```powershell
dotnet restore .\HayatiDesk.slnx
dotnet build .\HayatiDesk.slnx -c Release
dotnet test .\HayatiDesk.slnx -c Release --no-build
```

## Locality boundary

The application is designed for local storage and the current repository search did not identify an application HTTP client. That is a source-level observation, not a runtime zero-egress proof.

Do not describe a build as air-gapped or zero-egress solely because no cloud package is present. Runtime egress requires an execution-time network gate on the exact build being evaluated.

## Evidence rule

A successful build establishes buildability for the exercised revision and environment. It does not by itself establish zero telemetry, zero egress, production readiness, or security certification.
