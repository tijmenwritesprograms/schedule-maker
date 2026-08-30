# ScheduleMaker

Schedule Maker is a client-side .NET 10 Blazor WebAssembly application for
creating fair recurring task schedules. The app keeps domain models and
application state in `ScheduleMaker.App/Domain` and
`ScheduleMaker.App/Application`, separate from the Razor UI.

## Run

```bash
dotnet run --project ScheduleMaker.App/ScheduleMaker.App.csproj
```

The app starts with an empty in-memory state. Browser persistence and schedule
configuration features can be added through the registered
`ApplicationStateStore` seam without introducing a backend dependency.

## Test

```bash
dotnet test ScheduleMaker.slnx
```