# Schedule Maker MVP Product Requirements Document

## 1. Overview

Schedule Maker is a client-side Blazor WebAssembly application for dividing recurring event tasks among participants as fairly as possible. It is intended for use cases such as sports teams, clubs, volunteer groups, and other organizations that need to distribute responsibilities across a group.

A user maintains a participant list, defines reusable event types containing tasks, adds dated events, and generates a read-only schedule. The generator assigns every task to a participant, balances total assignments across participants, and avoids assigning the same task to the same participant repeatedly when suitable alternatives exist.

## 2. MVP scope

### In scope

- Participant list management.
- Reusable event types.
- Multiple tasks per event type.
- Dated events based on event types.
- Optional event descriptions.
- Fair schedule generation.
- Read-only generated schedule display.
- Browser local-storage persistence.
- Responsive and keyboard-accessible user interface.

### Out of scope

- User accounts or authentication.
- Server-side storage or a database.
- Multi-device synchronization.
- Participant availability or absences.
- Task priorities, weights, exclusions, or preferences.
- Manual assignment editing or swapping.
- CSV, PDF, or calendar export.
- Notifications and reminders.
- Multiple independent teams or workspaces.

## 3. Goals and success criteria

### Goals

1. Allow an organizer to configure participants and reusable task templates quickly.
2. Generate a complete schedule without manual assignment work.
3. Distribute assignments evenly across participants.
4. Reduce repeated assignment of the same task to the same participant.
5. Preserve the configuration and latest schedule across browser refreshes.
6. Make the resulting schedule easy to scan and understand.

### MVP success criteria

- A user can configure participants, event types, tasks, and dated events without leaving the app.
- A valid configuration produces exactly one assignment for every event task.
- Assignment totals differ by no more than one whenever mathematically possible and no additional constraints exist.
- Repeated tasks are distributed to different participants before repeating the same participant where suitable alternatives exist.
- Data remains available after a browser refresh.
- A user can identify each event, its description, every task, and its assigned participant from the generated schedule.

## 4. Users and primary use case

### Primary user

An organizer, coach, team manager, or volunteer coordinator who needs to distribute recurring responsibilities among a known group of participants.

### Primary workflow

1. Open Schedule Maker.
2. Add participants.
3. Create event types and add their tasks.
4. Add events with dates, event types, and optional descriptions.
5. Select **Generate schedule**.
6. Review the generated read-only schedule and assignment totals.
7. Change configuration and regenerate when a different result is needed.

## 5. Functional requirements

### 5.1 Participant management

The user must be able to:

- Add a participant by name.
- View all participants.
- Remove a participant.
- Reorder participants or otherwise have a stable participant order used for tie-breaking.

Rules:

- Participant names are required.
- Names containing only whitespace are invalid.
- Duplicate names are rejected after trimming and case-insensitive comparison.
- Participant changes are persisted locally.
- A configuration change invalidates or marks an existing generated schedule as stale.

### 5.2 Event type and task management

The user must be able to:

- Add an event type by name.
- Add multiple tasks to an event type.
- Remove tasks from an event type.
- Remove an event type.
- View event types and their tasks.

Rules:

- Event type names and task names are required.
- Duplicate event type names are rejected after trimming and case-insensitive comparison.
- Duplicate task names within one event type are rejected after trimming and case-insensitive comparison.
- An event type must contain at least one task to be usable for scheduling.
- Changes are persisted locally.
- Changes invalidate or mark an existing generated schedule as stale.

### 5.3 Event management

The user must be able to create and remove events. Each event contains:

- A date.
- An event type.
- An optional description.

Rules:

- The date is required.
- The event type is required and must reference an existing event type.
- The description may be blank.
- The description must be limited to 500 characters.
- Multiple events may have the same date.
- Events are displayed in chronological order, with stable ordering for events on the same date.
- Event changes are persisted locally.
- Changes invalidate or mark an existing generated schedule as stale.

### 5.4 Schedule generation

The user must be able to select **Generate schedule** to create a new schedule.

Generation must:

- Process events chronologically.
- Process tasks in their event-type order.
- Assign every task exactly once.
- Assign each task to one participant.
- Balance total assignments across participants as evenly as mathematically possible.
- Prefer participants with fewer total assignments.
- When workload is tied, prefer the participant who has received the current task fewer times.
- Use stable participant order to resolve remaining ties.
- Produce deterministic output for the same input configuration.
- Replace the previous generated schedule only after successful generation.
- Persist the generated schedule locally.

The first release assumes every participant is eligible for every task and event.

### 5.5 Schedule display

The generated schedule must:

- Be read-only.
- Group assignments by event.
- Sort events by date.
- Show the event date.
- Show the event type name.
- Show the optional event description when present.
- Show every task and its assigned participant.
- Show total assignment counts by participant.
- Indicate when the schedule is current or stale.
- Provide a way to regenerate after configuration changes.

### 5.6 Validation and empty states

Generation must be prevented with actionable messages when:

- No participants exist.
- No event types exist.
- A usable event type has no tasks.
- No events exist.
- An event has no date or event type.
- An event references a deleted event type.
- The configuration cannot produce a complete schedule.

The interface must provide useful empty states explaining what the user needs to add next.

## 6. Persistence requirements

The MVP must use browser local storage only. No backend or account is required.

Persist:

- Participants.
- Event types and tasks.
- Events and descriptions.
- The latest generated schedule.
- A schema/version value for future migrations.

The app must:

- Restore state after a browser refresh.
- Treat missing storage as a clean initial state.
- Recover gracefully from malformed or incompatible stored data.
- Avoid unhandled exceptions caused by local-storage data.
- Avoid storing sensitive information.

The storage implementation should be separated from Razor components so a future server or synchronization implementation can replace it.

## 7. Domain concepts

The implementation should represent the following concepts with stable identifiers:

- **Participant**: a person who can receive assignments.
- **Task definition**: a reusable responsibility belonging to an event type.
- **Event type**: a reusable named template containing one or more task definitions.
- **Scheduled event**: a dated occurrence that references an event type and has an optional description.
- **Generated schedule**: the result of running the generator against the current configuration.
- **Task assignment**: the relationship between one generated event task and one participant.

Generated schedules should snapshot relevant display names and the event description so the displayed result remains understandable until regeneration.

## 8. Fairness algorithm

The initial algorithm should be a deterministic greedy algorithm:

1. Flatten the event tasks in chronological event order and event-type task order.
2. Track each participant’s total assignment count.
3. Track how many times each participant has received each task.
4. For each task, select participants with the lowest total assignment count.
5. Among those participants, select the participant with the lowest count for that task.
6. Resolve remaining ties using stable participant order.
7. Assign the task and update both counters.
8. Continue until all tasks are assigned.

The algorithm must ensure that the difference between the highest and lowest total assignment counts is no greater than one whenever the number of assignments permits that result and no constraints exist.

## 9. User stories and acceptance criteria

### US-01: Manage participants

As an organizer, I want to maintain a participant list so that assignments go to the correct people.

Acceptance criteria:

- I can add a valid participant.
- Blank and duplicate names are rejected.
- I can remove a participant.
- Participants persist after refresh.

### US-02: Define event types

As an organizer, I want reusable event types with task lists so that recurring responsibilities do not need to be re-entered.

Acceptance criteria:

- I can create an event type.
- I can add and remove tasks.
- Blank and duplicate names are rejected.
- Event types and tasks persist after refresh.

### US-03: Add scheduled events

As an organizer, I want to create dated events from event types so that the app knows when tasks are required.

Acceptance criteria:

- I can select a date and event type.
- I can optionally enter an event description.
- Descriptions up to 500 characters are accepted.
- Descriptions over 500 characters are rejected or prevented.
- Multiple events can share a date.
- Events display in chronological order and persist after refresh.

### US-04: Generate a fair schedule

As an organizer, I want the app to assign tasks fairly so that responsibilities are not consistently concentrated on the same participant.

Acceptance criteria:

- Every task has exactly one assignment.
- Assignment totals differ by no more than one where mathematically possible.
- The generator avoids repeating the same task for the same participant when alternatives exist.
- Generation is deterministic for the same configuration.
- A successful generation replaces the previous schedule.

### US-05: Review the schedule

As an organizer, I want to review the generated schedule so that I can communicate responsibilities to participants.

Acceptance criteria:

- Events are grouped and sorted by date.
- Each event shows its date, event type, and description when present.
- Each task shows its assigned participant.
- Assignment totals are summarized by participant.
- Assignments cannot be edited directly.
- Configuration changes clearly indicate that regeneration is required.

## 10. Non-functional requirements

- Target .NET 10 and Blazor WebAssembly.
- Work without a backend after the app has loaded.
- Provide responsive layouts for desktop and mobile browsers.
- Use semantic labels and keyboard-accessible controls.
- Provide clear validation, loading, error, and empty states.
- Keep generation responsive for typical recreational-team workloads.
- Keep domain logic independent from UI components.

## 11. Testing requirements

Unit tests should cover:

- Required-field and duplicate-name validation.
- Description length validation.
- Empty and invalid configurations.
- One participant receiving all tasks.
- Even distributions.
- Uneven distributions with a maximum workload difference of one.
- Repeated tasks across events.
- Deterministic tie-breaking.
- Exactly one assignment per task.
- Persistence round-tripping.
- Missing and malformed local-storage data.

UI tests should cover adding and removing participants, creating event types and tasks, adding events with descriptions, generating a schedule, displaying descriptions, and marking schedules stale after configuration changes.

## 12. MVP delivery issues

The GitHub issues created from this PRD should be completed in dependency order:

1. Establish the application state and domain model.
2. Implement versioned browser local-storage persistence.
3. Implement configuration validation.
4. Implement and test the deterministic fair scheduling algorithm.
5. Build participant and event-type/task management UI.
6. Build dated-event management UI, including descriptions.
7. Build the read-only schedule view and stale-state behavior.
8. Apply responsive and accessible styling.
9. Add integration/UI coverage and complete MVP verification.

## 13. Future considerations

Potential post-MVP features include participant availability, task preferences and exclusions, manual assignment swaps, exports, authentication, server-side persistence, multiple teams or workspaces, calendar integration, and notifications. These features are intentionally excluded from the MVP and should not complicate the initial client-side design.
