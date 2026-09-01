namespace ScheduleMaker.App.Domain;

public sealed class Participant
{
    public Participant(Guid id, string name, int sortOrder)
    {
        Id = id;
        Name = DomainValidation.RequireNonEmpty(name, nameof(name));
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public string Name { get; }

    public int SortOrder { get; }
}

public sealed class TaskDefinition
{
    public TaskDefinition(Guid id, string name, int sortOrder)
    {
        Id = id;
        Name = DomainValidation.RequireNonEmpty(name, nameof(name));
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public string Name { get; }

    public int SortOrder { get; }
}

public sealed class EventType
{
    public EventType(Guid id, string name, IEnumerable<TaskDefinition> tasks)
    {
        Id = id;
        Name = DomainValidation.RequireNonEmpty(name, nameof(name));

        var orderedTasks = (tasks ?? throw new ArgumentNullException(nameof(tasks))).ToList();
        Tasks = orderedTasks.AsReadOnly();
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<TaskDefinition> Tasks { get; }
}

internal static class DomainValidation
{
    public static string RequireNonEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value;
    }
}

public sealed class ScheduledEvent
{
    public ScheduledEvent(Guid id, DateOnly date, Guid eventTypeId, string? description)
    {
        Id = id;
        Date = date;
        EventTypeId = eventTypeId;
        Description = description;
    }

    public Guid Id { get; }

    public DateOnly Date { get; }

    public Guid EventTypeId { get; }

    public string? Description { get; }
}

public sealed class GeneratedSchedule
{
    public GeneratedSchedule(
        Guid id,
        DateTimeOffset generatedAtUtc,
        IEnumerable<GeneratedScheduleEvent> events,
        IEnumerable<ParticipantAssignmentTotal> participantTotals)
    {
        Id = id;
        GeneratedAtUtc = generatedAtUtc;
        Events = (events ?? throw new ArgumentNullException(nameof(events))).ToList().AsReadOnly();
        ParticipantTotals = (participantTotals ?? throw new ArgumentNullException(nameof(participantTotals))).ToList().AsReadOnly();
    }

    public Guid Id { get; }

    public DateTimeOffset GeneratedAtUtc { get; }

    public IReadOnlyList<GeneratedScheduleEvent> Events { get; }

    public IReadOnlyList<ParticipantAssignmentTotal> ParticipantTotals { get; }

    public bool HasManualChanges => Events.SelectMany(@event => @event.Assignments).Any(assignment => assignment.IsManuallyEdited);
}

public sealed class GeneratedScheduleEvent
{
    public GeneratedScheduleEvent(
        Guid scheduledEventId,
        DateOnly date,
        Guid eventTypeId,
        string eventTypeNameSnapshot,
        string? eventDescriptionSnapshot,
        IEnumerable<GeneratedTaskAssignment> assignments)
    {
        ScheduledEventId = scheduledEventId;
        Date = date;
        EventTypeId = eventTypeId;
        EventTypeNameSnapshot = DomainValidation.RequireNonEmpty(eventTypeNameSnapshot, nameof(eventTypeNameSnapshot));
        EventDescriptionSnapshot = eventDescriptionSnapshot;
        Assignments = (assignments ?? throw new ArgumentNullException(nameof(assignments))).ToList().AsReadOnly();
    }

    public Guid ScheduledEventId { get; }

    public DateOnly Date { get; }

    public Guid EventTypeId { get; }

    public string EventTypeNameSnapshot { get; }

    public string? EventDescriptionSnapshot { get; }

    public IReadOnlyList<GeneratedTaskAssignment> Assignments { get; }
}

public sealed class GeneratedTaskAssignment
{
    public GeneratedTaskAssignment(
        Guid taskDefinitionId,
        string taskNameSnapshot,
        Guid participantId,
        string participantNameSnapshot)
        : this(
            taskDefinitionId,
            taskNameSnapshot,
            participantId,
            participantNameSnapshot,
            participantId,
            participantNameSnapshot)
    {
    }

    public GeneratedTaskAssignment(
        Guid taskDefinitionId,
        string taskNameSnapshot,
        Guid originalParticipantId,
        string originalParticipantNameSnapshot,
        Guid participantId,
        string participantNameSnapshot)
    {
        TaskDefinitionId = taskDefinitionId;
        TaskNameSnapshot = DomainValidation.RequireNonEmpty(taskNameSnapshot, nameof(taskNameSnapshot));
        OriginalParticipantId = originalParticipantId;
        OriginalParticipantNameSnapshot = DomainValidation.RequireNonEmpty(originalParticipantNameSnapshot, nameof(originalParticipantNameSnapshot));
        ParticipantId = participantId;
        ParticipantNameSnapshot = DomainValidation.RequireNonEmpty(participantNameSnapshot, nameof(participantNameSnapshot));
    }

    public Guid TaskDefinitionId { get; }

    public string TaskNameSnapshot { get; }

    public Guid OriginalParticipantId { get; }

    public string OriginalParticipantNameSnapshot { get; }

    public Guid ParticipantId { get; }

    public string ParticipantNameSnapshot { get; }

    public bool IsManuallyEdited => ParticipantId != OriginalParticipantId;
}

public sealed class ParticipantAssignmentTotal
{
    public ParticipantAssignmentTotal(Guid participantId, string participantNameSnapshot, int assignmentCount)
    {
        if (assignmentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assignmentCount), "Assignment count cannot be negative.");
        }

        ParticipantId = participantId;
        ParticipantNameSnapshot = DomainValidation.RequireNonEmpty(participantNameSnapshot, nameof(participantNameSnapshot));
        AssignmentCount = assignmentCount;
    }

    public Guid ParticipantId { get; }

    public string ParticipantNameSnapshot { get; }

    public int AssignmentCount { get; }
}
