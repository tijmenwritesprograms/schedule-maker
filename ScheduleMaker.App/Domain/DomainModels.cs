namespace ScheduleMaker.App.Domain;

public sealed class Participant
{
    public Participant(Guid id, string name, int sortOrder)
    {
        Id = id;
        Name = name;
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
        Name = name;
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
        Name = name;
        Tasks = (tasks ?? throw new ArgumentNullException(nameof(tasks))).ToList().AsReadOnly();
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<TaskDefinition> Tasks { get; }
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
        EventTypeNameSnapshot = eventTypeNameSnapshot;
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
    {
        TaskDefinitionId = taskDefinitionId;
        TaskNameSnapshot = taskNameSnapshot;
        ParticipantId = participantId;
        ParticipantNameSnapshot = participantNameSnapshot;
    }

    public Guid TaskDefinitionId { get; }

    public string TaskNameSnapshot { get; }

    public Guid ParticipantId { get; }

    public string ParticipantNameSnapshot { get; }
}

public sealed class ParticipantAssignmentTotal
{
    public ParticipantAssignmentTotal(Guid participantId, string participantNameSnapshot, int assignmentCount)
    {
        ParticipantId = participantId;
        ParticipantNameSnapshot = participantNameSnapshot;
        AssignmentCount = assignmentCount;
    }

    public Guid ParticipantId { get; }

    public string ParticipantNameSnapshot { get; }

    public int AssignmentCount { get; }
}
