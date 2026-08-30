using ScheduleMaker.App.Domain;

namespace ScheduleMaker.App.Application;

public enum ConfigurationValidationScope
{
    Configuration,
    Participant,
    EventType,
    Task,
    ScheduledEvent
}

public sealed record ConfigurationValidationIssue(
    ConfigurationValidationScope Scope,
    Guid? EntityId,
    string Message);

public sealed class ConfigurationValidationResult
{
    public ConfigurationValidationResult(IEnumerable<ConfigurationValidationIssue> issues, string nextStepMessage)
    {
        Issues = (issues ?? throw new ArgumentNullException(nameof(issues))).ToList().AsReadOnly();
        NextStepMessage = string.IsNullOrWhiteSpace(nextStepMessage)
            ? throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(nextStepMessage))
            : nextStepMessage;
    }

    public IReadOnlyList<ConfigurationValidationIssue> Issues { get; }

    public string NextStepMessage { get; }

    public bool CanGenerate => Issues.Count == 0;
}

public static class ConfigurationValidation
{
    public static ConfigurationValidationResult Validate(ApplicationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var issues = new List<ConfigurationValidationIssue>();

        ValidateParticipants(state.Participants, issues);
        ValidateEventTypes(state.EventTypes, issues);
        ValidateScheduledEvents(state.ScheduledEvents, state.EventTypes, issues);
        var nextStepMessage = DetermineNextStepMessage(state, issues);
        ValidateGenerationRequirements(state, issues);

        return new ConfigurationValidationResult(issues, nextStepMessage);
    }

    private static void ValidateParticipants(
        IReadOnlyList<Participant> participants,
        ICollection<ConfigurationValidationIssue> issues)
    {
        foreach (var participant in participants)
        {
            if (NormalizeRequiredName(participant.Name) is null)
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationScope.Participant,
                    participant.Id,
                    "Participant names cannot be blank."));
            }
        }

        AddDuplicateNameIssues(
            participants,
            participant => participant.Name,
            participant => participant.Id,
            ConfigurationValidationScope.Participant,
            "Participant names must be unique.",
            issues);
    }

    private static void ValidateEventTypes(
        IReadOnlyList<EventType> eventTypes,
        ICollection<ConfigurationValidationIssue> issues)
    {
        foreach (var eventType in eventTypes)
        {
            var normalizedEventTypeName = NormalizeRequiredName(eventType.Name);
            if (normalizedEventTypeName is null)
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationScope.EventType,
                    eventType.Id,
                    "Event type names cannot be blank."));
            }

            if (eventType.Tasks.Count == 0)
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationScope.EventType,
                    eventType.Id,
                    $"Event type {FormatName(eventType.Name, "without a name")} must contain at least one task."));
            }

            foreach (var task in eventType.Tasks)
            {
                if (NormalizeRequiredName(task.Name) is null)
                {
                    issues.Add(new ConfigurationValidationIssue(
                        ConfigurationValidationScope.Task,
                        task.Id,
                        $"Task names in event type {FormatName(eventType.Name, "without a name")} cannot be blank."));
                }
            }

            AddDuplicateNameIssues(
                eventType.Tasks,
                task => task.Name,
                task => task.Id,
                ConfigurationValidationScope.Task,
                $"Task names in event type {FormatName(eventType.Name, "without a name")} must be unique.",
                issues);
        }

        AddDuplicateNameIssues(
            eventTypes,
            eventType => eventType.Name,
            eventType => eventType.Id,
            ConfigurationValidationScope.EventType,
            "Event type names must be unique.",
            issues);
    }

    private static void ValidateScheduledEvents(
        IReadOnlyList<ScheduledEvent> scheduledEvents,
        IReadOnlyList<EventType> eventTypes,
        ICollection<ConfigurationValidationIssue> issues)
    {
        var eventTypeIds = eventTypes.Select(eventType => eventType.Id).ToHashSet();

        foreach (var scheduledEvent in scheduledEvents)
        {
            if (scheduledEvent.Date == default)
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationScope.ScheduledEvent,
                    scheduledEvent.Id,
                    "Events must have a date."));
            }

            if (scheduledEvent.EventTypeId == Guid.Empty || !eventTypeIds.Contains(scheduledEvent.EventTypeId))
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationScope.ScheduledEvent,
                    scheduledEvent.Id,
                    $"{DescribeEvent(scheduledEvent)} must reference an existing event type."));
            }

            if (scheduledEvent.Description is { Length: > 500 })
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationScope.ScheduledEvent,
                    scheduledEvent.Id,
                    "Event descriptions cannot be longer than 500 characters."));
            }
        }
    }

    private static void ValidateGenerationRequirements(
        ApplicationState state,
        ICollection<ConfigurationValidationIssue> issues)
    {
        if (state.Participants.Count == 0)
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationScope.Configuration,
                null,
                "Add at least one participant before generating a schedule."));
        }

        if (state.EventTypes.Count == 0)
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationScope.Configuration,
                null,
                "Add at least one event type before generating a schedule."));
        }
        else if (!state.EventTypes.Any(eventType => eventType.Tasks.Count > 0))
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationScope.Configuration,
                null,
                "Add at least one usable event type with a task before generating a schedule."));
        }

        if (state.ScheduledEvents.Count == 0)
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationScope.Configuration,
                null,
                "Add at least one event before generating a schedule."));
        }
    }

    private static string DetermineNextStepMessage(
        ApplicationState state,
        IReadOnlyCollection<ConfigurationValidationIssue> issues)
    {
        if (state.Participants.Count == 0)
        {
            return "Add at least one participant to get started.";
        }

        if (state.EventTypes.Count == 0)
        {
            return "Add an event type and at least one task next.";
        }

        if (!state.EventTypes.Any(eventType => eventType.Tasks.Count > 0))
        {
            return "Add at least one task to an event type before generating a schedule.";
        }

        if (state.ScheduledEvents.Count == 0)
        {
            return "Add at least one dated event to continue.";
        }

        if (issues.Count > 0)
        {
            return "Fix the validation errors below before generating a schedule.";
        }

        return "Your configuration is ready to generate a schedule.";
    }

    private static void AddDuplicateNameIssues<T>(
        IEnumerable<T> items,
        Func<T, string> nameSelector,
        Func<T, Guid> idSelector,
        ConfigurationValidationScope scope,
        string message,
        ICollection<ConfigurationValidationIssue> issues)
    {
        foreach (var group in items
            .Select(item => new { Item = item, Name = NormalizeRequiredName(nameSelector(item)) })
            .Where(entry => entry.Name is not null)
            .GroupBy(entry => entry.Name!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            foreach (var entry in group)
            {
                issues.Add(new ConfigurationValidationIssue(scope, idSelector(entry.Item), message));
            }
        }
    }

    private static string? NormalizeRequiredName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string FormatName(string value, string fallback)
    {
        var normalizedName = NormalizeRequiredName(value);
        return normalizedName is null ? fallback : $"\"{normalizedName}\"";
    }

    private static string DescribeEvent(ScheduledEvent scheduledEvent) =>
        scheduledEvent.Date == default
            ? "An event"
            : $"The event on {scheduledEvent.Date:yyyy-MM-dd}";
}
