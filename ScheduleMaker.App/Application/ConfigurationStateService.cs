using ScheduleMaker.App.Domain;

namespace ScheduleMaker.App.Application;

public sealed record EventTypeTaskUpdate(Guid? Id, string Name);

public enum RecurrenceInterval
{
    Weekly = 1,
    Biweekly = 2
}

public sealed class ConfigurationStateService(ApplicationStateStore stateStore)
{
    public ConfigurationValidationResult ValidateCurrentConfiguration() =>
        ConfigurationValidation.Validate(stateStore.Current);

    public async Task<ApplicationOperationResult> AddParticipantAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeRequiredName(name);
        if (normalizedName is null)
        {
            return ApplicationOperationResult.Failure("Participant name is required.");
        }

        if (stateStore.Current.Participants.Any(participant => NameEquals(participant.Name, normalizedName)))
        {
            return ApplicationOperationResult.Failure("Participant names must be unique.");
        }

        var participants = stateStore.Current.Participants.ToList();
        participants.Add(new Participant(Guid.NewGuid(), normalizedName, NextSortOrder(participants.Select(participant => participant.SortOrder))));

        await ReplaceStateAsync(
            participants: participants,
            eventTypes: stateStore.Current.EventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> RemoveParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var participants = stateStore.Current.Participants.ToList();
        var removed = participants.RemoveAll(participant => participant.Id == participantId) > 0;
        if (!removed)
        {
            return ApplicationOperationResult.Failure("Participant was not found.");
        }

        await ReplaceStateAsync(
            participants: participants,
            eventTypes: stateStore.Current.EventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> AddEventTypeAsync(
        string eventTypeName,
        string initialTaskName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEventTypeName = NormalizeRequiredName(eventTypeName);
        if (normalizedEventTypeName is null)
        {
            return ApplicationOperationResult.Failure("Event type name is required.");
        }

        var normalizedTaskName = NormalizeRequiredName(initialTaskName);
        if (normalizedTaskName is null)
        {
            return ApplicationOperationResult.Failure("Task name is required.");
        }

        if (stateStore.Current.EventTypes.Any(eventType => NameEquals(eventType.Name, normalizedEventTypeName)))
        {
            return ApplicationOperationResult.Failure("Event type names must be unique.");
        }

        var eventTypes = stateStore.Current.EventTypes.ToList();
        eventTypes.Add(new EventType(
            Guid.NewGuid(),
            normalizedEventTypeName,
            [new TaskDefinition(Guid.NewGuid(), normalizedTaskName, sortOrder: 0)]));

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: eventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> AddEventTypeAsync(
        string eventTypeName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEventTypeName = NormalizeRequiredName(eventTypeName);
        if (normalizedEventTypeName is null)
        {
            return ApplicationOperationResult.Failure("Event type name is required.");
        }

        if (stateStore.Current.EventTypes.Any(eventType => NameEquals(eventType.Name, normalizedEventTypeName)))
        {
            return ApplicationOperationResult.Failure("Event type names must be unique.");
        }

        var eventTypes = stateStore.Current.EventTypes.ToList();
        eventTypes.Add(new EventType(Guid.NewGuid(), normalizedEventTypeName, []));

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: eventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> RemoveEventTypeAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        var eventTypes = stateStore.Current.EventTypes.ToList();
        var removed = eventTypes.RemoveAll(eventType => eventType.Id == eventTypeId) > 0;
        if (!removed)
        {
            return ApplicationOperationResult.Failure("Event type was not found.");
        }

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: eventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> EditEventTypeAsync(
        Guid eventTypeId,
        string eventTypeName,
        IEnumerable<EventTypeTaskUpdate>? taskUpdates,
        CancellationToken cancellationToken = default)
    {
        var normalizedEventTypeName = NormalizeRequiredName(eventTypeName);
        if (normalizedEventTypeName is null)
        {
            return ApplicationOperationResult.Failure("Event type name is required.");
        }

        var eventTypes = stateStore.Current.EventTypes.ToList();
        var eventTypeIndex = eventTypes.FindIndex(eventType => eventType.Id == eventTypeId);
        if (eventTypeIndex < 0)
        {
            return ApplicationOperationResult.Failure("Event type was not found.");
        }

        if (eventTypes.Any(eventType =>
            eventType.Id != eventTypeId && NameEquals(eventType.Name, normalizedEventTypeName)))
        {
            return ApplicationOperationResult.Failure("Event type names must be unique.");
        }

        if (taskUpdates is null)
        {
            return ApplicationOperationResult.Failure("Tasks are required.");
        }

        var existingEventType = eventTypes[eventTypeIndex];
        var existingTasks = existingEventType.Tasks.ToDictionary(task => task.Id);
        var updates = taskUpdates.ToList();
        var normalizedTaskNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tasks = new List<TaskDefinition>(updates.Count);

        foreach (var update in updates)
        {
            var normalizedTaskName = NormalizeRequiredName(update.Name);
            if (normalizedTaskName is null)
            {
                return ApplicationOperationResult.Failure("Task name is required.");
            }

            if (!normalizedTaskNames.Add(normalizedTaskName))
            {
                return ApplicationOperationResult.Failure("Task names must be unique per event type.");
            }

            var taskId = update.Id is { } requestedId && existingTasks.ContainsKey(requestedId)
                ? requestedId
                : Guid.NewGuid();
            tasks.Add(new TaskDefinition(taskId, normalizedTaskName, tasks.Count));
        }

        eventTypes[eventTypeIndex] = new EventType(eventTypeId, normalizedEventTypeName, tasks);

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: eventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> AddTaskAsync(Guid eventTypeId, string taskName, CancellationToken cancellationToken = default)
    {
        var normalizedTaskName = NormalizeRequiredName(taskName);
        if (normalizedTaskName is null)
        {
            return ApplicationOperationResult.Failure("Task name is required.");
        }

        var eventTypes = stateStore.Current.EventTypes.ToList();
        var eventTypeIndex = eventTypes.FindIndex(eventType => eventType.Id == eventTypeId);
        if (eventTypeIndex < 0)
        {
            return ApplicationOperationResult.Failure("Event type was not found.");
        }

        var eventType = eventTypes[eventTypeIndex];
        if (eventType.Tasks.Any(task => NameEquals(task.Name, normalizedTaskName)))
        {
            return ApplicationOperationResult.Failure("Task names must be unique per event type.");
        }

        var tasks = eventType.Tasks.ToList();
        tasks.Add(new TaskDefinition(Guid.NewGuid(), normalizedTaskName, NextSortOrder(tasks.Select(task => task.SortOrder))));
        eventTypes[eventTypeIndex] = new EventType(eventType.Id, eventType.Name, tasks);

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: eventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> RemoveTaskAsync(Guid eventTypeId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var eventTypes = stateStore.Current.EventTypes.ToList();
        var eventTypeIndex = eventTypes.FindIndex(eventType => eventType.Id == eventTypeId);
        if (eventTypeIndex < 0)
        {
            return ApplicationOperationResult.Failure("Event type was not found.");
        }

        var eventType = eventTypes[eventTypeIndex];
        var tasks = eventType.Tasks.ToList();
        var removed = tasks.RemoveAll(task => task.Id == taskId) > 0;
        if (!removed)
        {
            return ApplicationOperationResult.Failure("Task was not found.");
        }

        eventTypes[eventTypeIndex] = new EventType(eventType.Id, eventType.Name, tasks);

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: eventTypes,
            scheduledEvents: stateStore.Current.ScheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> AddScheduledEventAsync(
        DateOnly date,
        Guid eventTypeId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (!stateStore.Current.EventTypes.Any(eventType => eventType.Id == eventTypeId))
        {
            return ApplicationOperationResult.Failure("Event type was not found.");
        }

        var normalizedDescription = NormalizeOptionalText(description);

        if (normalizedDescription is not null && normalizedDescription.Length > 500)
        {
            return ApplicationOperationResult.Failure("Description cannot be longer than 500 characters.");
        }

        var scheduledEvents = stateStore.Current.ScheduledEvents
            .OrderBy(existingEvent => existingEvent.Date)
            .ToList();
        var scheduledEvent = new ScheduledEvent(
            Guid.NewGuid(),
            date,
            eventTypeId,
            normalizedDescription);

        var insertIndex = scheduledEvents.FindLastIndex(existingEvent => existingEvent.Date <= date) + 1;
        scheduledEvents.Insert(insertIndex, scheduledEvent);

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: stateStore.Current.EventTypes,
            scheduledEvents: scheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public Task<ApplicationOperationResult> AddRecurringScheduledEventsAsync(
        DateOnly? startDate,
        DateOnly? endDate,
        Guid eventTypeId,
        int intervalWeeks,
        string? description,
        CancellationToken cancellationToken = default) =>
        intervalWeeks is 1 or 2
            ? AddRecurringScheduledEventsAsync(
                startDate,
                endDate,
                eventTypeId,
                (RecurrenceInterval)intervalWeeks,
                description,
                cancellationToken)
            : Task.FromResult(ApplicationOperationResult.Failure("Choose a weekly or biweekly recurrence interval."));

    public async Task<ApplicationOperationResult> AddRecurringScheduledEventsAsync(
        DateOnly? startDate,
        DateOnly? endDate,
        Guid eventTypeId,
        RecurrenceInterval interval,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (startDate is null || endDate is null)
        {
            return ApplicationOperationResult.Failure("Choose a start and end date.");
        }

        if (endDate < startDate)
        {
            return ApplicationOperationResult.Failure("End date cannot be earlier than the start date.");
        }

        if (interval is not (RecurrenceInterval.Weekly or RecurrenceInterval.Biweekly))
        {
            return ApplicationOperationResult.Failure("Choose a weekly or biweekly recurrence interval.");
        }

        if (!stateStore.Current.EventTypes.Any(eventType => eventType.Id == eventTypeId))
        {
            return ApplicationOperationResult.Failure("Event type was not found.");
        }

        var normalizedDescription = NormalizeOptionalText(description);
        if (normalizedDescription is not null && normalizedDescription.Length > 500)
        {
            return ApplicationOperationResult.Failure("Description cannot be longer than 500 characters.");
        }

        var existingEvents = stateStore.Current.ScheduledEvents.ToList();
        var existingDefinitions = existingEvents
            .Select(scheduledEvent => (scheduledEvent.EventTypeId, scheduledEvent.Date))
            .ToHashSet();
        var occurrences = new List<ScheduledEvent>();
        var step = interval == RecurrenceInterval.Weekly ? 7 : 14;

        for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(step))
        {
            if (existingDefinitions.Add((eventTypeId, date)))
            {
                occurrences.Add(new ScheduledEvent(Guid.NewGuid(), date, eventTypeId, normalizedDescription));
            }
        }

        if (occurrences.Count == 0)
        {
            return ApplicationOperationResult.Success();
        }

        var scheduledEvents = existingEvents
            .Concat(occurrences)
            .OrderBy(scheduledEvent => scheduledEvent.Date)
            .ToList();

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: stateStore.Current.EventTypes,
            scheduledEvents: scheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> RemoveScheduledEventAsync(Guid scheduledEventId, CancellationToken cancellationToken = default)
    {
        var scheduledEvents = stateStore.Current.ScheduledEvents.ToList();
        var removed = scheduledEvents.RemoveAll(@event => @event.Id == scheduledEventId) > 0;
        if (!removed)
        {
            return ApplicationOperationResult.Failure("Event was not found.");
        }

        await ReplaceStateAsync(
            participants: stateStore.Current.Participants,
            eventTypes: stateStore.Current.EventTypes,
            scheduledEvents: scheduledEvents,
            scheduleChanged: true,
            cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult> ApplyGeneratedScheduleAsync(
        ApplicationOperationResult<GeneratedSchedule> generationResult,
        CancellationToken cancellationToken = default)
    {
        if (!generationResult.IsSuccess || generationResult.Value is null)
        {
            return ApplicationOperationResult.Failure(generationResult.ErrorMessage ?? "Schedule generation failed.");
        }

        stateStore.Replace(new ApplicationState(
            stateStore.Current.Participants,
            stateStore.Current.EventTypes,
            stateStore.Current.ScheduledEvents,
            generationResult.Value,
            isScheduleStale: false,
            stateStore.Current.SchemaVersion));

        await stateStore.PersistAsync(cancellationToken);
        return ApplicationOperationResult.Success();
    }

    public async Task<ApplicationOperationResult<GeneratedSchedule>> GenerateScheduleAsync(
        CancellationToken cancellationToken = default)
    {
        var generationResult = ScheduleGenerationService.Generate(stateStore.Current);
        if (!generationResult.IsSuccess || generationResult.Value is null)
        {
            return ApplicationOperationResult<GeneratedSchedule>.Failure(
                generationResult.ErrorMessage ?? "Schedule generation failed.");
        }

        await ApplyGeneratedScheduleAsync(generationResult, cancellationToken);
        return generationResult;
    }

    private async Task ReplaceStateAsync(
        IEnumerable<Participant> participants,
        IEnumerable<EventType> eventTypes,
        IEnumerable<ScheduledEvent> scheduledEvents,
        bool scheduleChanged,
        CancellationToken cancellationToken)
    {
        var shouldMarkScheduleStale = scheduleChanged && stateStore.Current.LatestSchedule is not null;
        stateStore.Replace(new ApplicationState(
            participants,
            eventTypes,
            scheduledEvents,
            stateStore.Current.LatestSchedule,
            isScheduleStale: shouldMarkScheduleStale,
            stateStore.Current.SchemaVersion));

        await stateStore.PersistAsync(cancellationToken);
    }

    private static int NextSortOrder(IEnumerable<int> sortOrders)
    {
        var maxSortOrder = -1;
        foreach (var sortOrder in sortOrders)
        {
            if (sortOrder > maxSortOrder)
            {
                maxSortOrder = sortOrder;
            }
        }

        return maxSortOrder + 1;
    }

    private static bool NameEquals(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeRequiredName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return name.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
