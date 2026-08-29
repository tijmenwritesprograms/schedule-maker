using ScheduleMaker.App.Domain;

namespace ScheduleMaker.App.Application;

public sealed class ApplicationState
{
    public const int CurrentSchemaVersion = 1;

    public ApplicationState(
        IEnumerable<Participant> participants,
        IEnumerable<EventType> eventTypes,
        IEnumerable<ScheduledEvent> scheduledEvents,
        GeneratedSchedule? latestSchedule,
        bool isScheduleStale,
        int schemaVersion = CurrentSchemaVersion)
    {
        Participants = (participants ?? throw new ArgumentNullException(nameof(participants))).ToList().AsReadOnly();
        EventTypes = (eventTypes ?? throw new ArgumentNullException(nameof(eventTypes))).ToList().AsReadOnly();
        ScheduledEvents = (scheduledEvents ?? throw new ArgumentNullException(nameof(scheduledEvents))).ToList().AsReadOnly();
        LatestSchedule = latestSchedule;
        IsScheduleStale = isScheduleStale;
        SchemaVersion = schemaVersion;
    }

    public IReadOnlyList<Participant> Participants { get; }

    public IReadOnlyList<EventType> EventTypes { get; }

    public IReadOnlyList<ScheduledEvent> ScheduledEvents { get; }

    public GeneratedSchedule? LatestSchedule { get; }

    public bool IsScheduleStale { get; }

    public int SchemaVersion { get; }

    public static ApplicationState Empty => new([], [], [], latestSchedule: null, isScheduleStale: false);
}
