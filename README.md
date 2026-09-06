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

## GitHub Pages deployments

The `main` branch is published to GitHub Pages after every push, including
merges. The production URL is
`https://tijmenwritesprograms.github.io/schedule-maker/`; the repository must
have GitHub Pages configured to use GitHub Actions.

When a pull request is opened, reopened, updated, or marked ready for review,
the Blazor WebAssembly app is built from that pull request and deployed as a
GitHub Pages preview. Draft pull requests are skipped. The preview URL is
shown in the pull request's **Deployments** section through the
`preview-pr-<number>` environment and in an automated pull request comment.
The preview deployment and environment are removed automatically when the pull
request is closed or merged.

The deployment workflows use the repository's built-in `GITHUB_TOKEN`; no application
secrets or backend services are required. Deployment concurrency is serialized
per production site or pull request preview, so a cancelled older run cannot
replace a newer deployment.
