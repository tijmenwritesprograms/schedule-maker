# MVP verification checklist

## Automated coverage

- [x] Participant add/remove workflow is covered by `HomePageTests`.
- [x] Event type and task creation/removal is covered by `HomePageTests`.
- [x] Dated events and descriptions are covered by `HomePageTests`.
- [x] Generation, read-only schedule display, assignment totals, and stale-state behavior are covered by `HomePageTests`.
- [x] Empty and invalid configurations are covered by `HomePageTests` and the configuration validation tests.
- [x] Local-storage round trips, refresh recovery, and malformed data handling are covered by `LocalStorageApplicationStatePersistenceTests`.

## Manual release checks

- [ ] Run `dotnet run --project ScheduleMaker.App/ScheduleMaker.App.csproj` and complete the organizer workflow in a browser.
- [ ] Refresh the browser and confirm participants, event types, events, descriptions, and the latest schedule remain available.
- [ ] Confirm the app has no required backend request after the initial app load.
- [ ] Confirm the app targets .NET 10 Blazor WebAssembly.
- [ ] Resize to narrow and wide viewports and confirm setup and schedule sections remain readable without horizontal scrolling.
- [ ] Complete the workflow using keyboard focus, Enter, and standard select controls without requiring a pointer.
