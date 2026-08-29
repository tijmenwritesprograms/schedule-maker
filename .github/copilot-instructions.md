# Schedule Maker Copilot Instructions

## Project context

This repository is a Blazor WebAssembly application named Schedule Maker. The product is a client-side scheduler that assigns recurring tasks fairly among participants. The primary goal is to support the MVP described in PRD.md without adding backend services, user accounts, or multi-device sync.

Follow the PRD as the source of truth for behavior, acceptance criteria, and product constraints. When a requirement is ambiguous, prefer the simplest implementation that satisfies the MVP and keeps the app deterministic and easy to reason about.

## Architecture and stack

- Build with .NET and Blazor WebAssembly.
- Target the existing app project in ScheduleMaker.App.
- Keep the app browser-only: use local storage for persistence, not a database or server API.
- Separate domain logic from UI when it improves clarity, especially for scheduling, validation, and persistence.
- Prefer efficient, readable C# and Razor code over clever abstractions.
- Respect the current setup in Program.cs and the project file: .NET 10 WebAssembly, nullable enabled, implicit usings enabled.

## Product requirements to preserve

- Manage participants, event types, tasks, and dated events.
- Generate schedules deterministically from the current configuration.
- Balance assignment totals fairly across participants.
- Prefer reducing repeated assignment of the same task to the same participant when alternatives exist.
- Snapshot generated schedule data so it remains readable after regeneration or refresh.
- Persist state locally and recover gracefully from malformed or missing storage.
- Keep the generated schedule read-only and clearly mark stale state after configuration changes.
- Validate input and surface actionable user-facing errors.

## Fairness and scheduling rules

- Use a deterministic greedy scheduling algorithm as described in PRD.md.
- Process events in chronological order and tasks in event-type order.
- Keep per-participant total counts and per-task counts during generation.
- Prefer the lowest total assignments first, then lowest count for the current task, then stable participant order.
- Ensure generated output is reproducible for the same input configuration.
- Avoid manual editing or unsupported assignment overrides in the MVP.

## Data and persistence rules

- Store all app state in browser local storage only.
- Maintain a schema or version field for future migrations.
- Treat missing or malformed storage as a clean startup state.
- Avoid unhandled exceptions from persisted data.
- Keep storage logic separate from components so future server or sync implementations can replace it.
- Do not store sensitive information.

## UI and component guidance

- Follow the existing Blazor conventions in .github/instructions/blazor.instructions.md.
- Use PascalCase for component names, methods, and public members.
- Use camelCase for private fields and locals.
- Keep components focused and readable; move business logic into services or domain models when it grows.
- Use data binding and DI idiomatically in Blazor.
- Prefer clear empty states and validation messages that tell the user what to add or fix.
- Make the interface responsive and keyboard-accessible where practical.

## Validation and edge cases

- Reject blank or duplicate participant names after trimming and case-insensitive comparison.
- Reject blank or duplicate event type and task names after trimming and case-insensitive comparison.
- Require a usable event type to contain at least one task.
- Require valid date and event-type references for events.
- Limit event descriptions to 500 characters.
- Prevent generation when required data is missing and explain why.
- Keep stale schedule state visible when configuration changes.

## Coding expectations

- Keep changes aligned to the MVP and do not add out-of-scope features.
- Prefer simple, maintainable code patterns over over-engineering.
- Follow C# and Blazor conventions, including nullable reference types and idiomatic async usage when needed.
- Use strong validation and defensive logic around persisted local-state data.
- Prefer deterministic behavior and stable ordering in all algorithmic and display code.
- Keep names and identifiers meaningful and consistent with the domain terms in the PRD.

## Testing and verification

- Validate changes with the smallest relevant build or test command.
- Prefer real behavior checks over mocked-only verification.
- When fixing a bug, reproduce the issue and confirm the fix with a focused verification step.
- Preserve deterministic schedules and edge-case validation when making changes.

## Scope guardrails

- Do not add authentication, server storage, user accounts, exports, or notifications unless explicitly requested.
- Do not introduce multi-team or server-side concepts at this stage.
- Do not broaden the project beyond the MVP without an explicit requirement.
- Prefer implementing the next requirement in a way that supports the PRD’s fairness, persistence, and UX goals.

## Useful prompts for this repo

Good prompts for Copilot should reference the MVP behavior directly, for example:

- "Add participant validation with trimmed, case-insensitive duplicate checks and a local-storage persistence layer."
- "Implement a deterministic fairness scheduler that balances participant totals and prefers fewer repeated task assignments."
- "Add stale-schedule handling to the UI when config changes invalidate the current generated schedule."
- "Create a read-only generated schedule view grouped by event and task, with totals by participant."

When in doubt, follow the PRD, the Blazor guidance, and the requirement to keep the app client-side, fair, and deterministic.
