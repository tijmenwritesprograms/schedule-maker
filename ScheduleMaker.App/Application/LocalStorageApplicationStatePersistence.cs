using System.Text.Json;
using ScheduleMaker.App.Domain;

namespace ScheduleMaker.App.Application;

public sealed class LocalStorageApplicationStatePersistence(ILocalStorage storage) : IApplicationStatePersistence
{
    public const string StorageKey = "schedule-maker.state";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task<ApplicationState> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await storage.GetItemAsync(StorageKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return ApplicationState.Empty;
            }

            var data = JsonSerializer.Deserialize<PersistedState>(json, JsonOptions);
            return data is null || data.SchemaVersion != ApplicationState.CurrentSchemaVersion
                ? ApplicationState.Empty
                : ToApplicationState(data);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException or FormatException)
        {
            return ApplicationState.Empty;
        }
    }

    public async Task SaveAsync(ApplicationState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var json = JsonSerializer.Serialize(FromApplicationState(state), JsonOptions);
        await storage.SetItemAsync(StorageKey, json, cancellationToken);
    }

    private static ApplicationState ToApplicationState(PersistedState data)
    {
        var participants = (data.Participants ?? throw new InvalidDataException()).Select(p =>
            new Participant(RequiredId(p.Id), RequiredText(p.Name), p.SortOrder)).ToList();
        var eventTypes = (data.EventTypes ?? throw new InvalidDataException()).Select(e =>
            new EventType(RequiredId(e.Id), RequiredText(e.Name),
                (e.Tasks ?? throw new InvalidDataException()).Select(t =>
                    new TaskDefinition(RequiredId(t.Id), RequiredText(t.Name), t.SortOrder)))).ToList();

        var scheduledEvents = (data.ScheduledEvents ?? throw new InvalidDataException()).Select(e =>
        {
            var eventTypeId = RequiredId(e.EventTypeId);
            return new ScheduledEvent(RequiredId(e.Id), e.Date, eventTypeId, e.Description);
        }).ToList();

        var schedule = data.LatestSchedule is null ? null : ToGeneratedSchedule(data.LatestSchedule);

        return new ApplicationState(
            participants,
            eventTypes,
            scheduledEvents,
            schedule,
            data.IsScheduleStale,
            data.SchemaVersion);
    }

    private static GeneratedSchedule ToGeneratedSchedule(PersistedGeneratedSchedule data)
    {
        var events = (data.Events ?? throw new InvalidDataException()).Select(e =>
        {
            var eventTypeId = RequiredId(e.EventTypeId);

            return new GeneratedScheduleEvent(
                RequiredId(e.ScheduledEventId),
                e.Date,
                eventTypeId,
                RequiredText(e.EventTypeNameSnapshot),
                e.EventDescriptionSnapshot,
                (e.Assignments ?? throw new InvalidDataException()).Select(a =>
                {
                    var participantId = RequiredId(a.ParticipantId);
                    var taskId = RequiredId(a.TaskDefinitionId);

                    return new GeneratedTaskAssignment(
                        taskId,
                        RequiredText(a.TaskNameSnapshot),
                        a.OriginalParticipantId == Guid.Empty ? participantId : RequiredId(a.OriginalParticipantId),
                        string.IsNullOrWhiteSpace(a.OriginalParticipantNameSnapshot)
                            ? RequiredText(a.ParticipantNameSnapshot)
                            : RequiredText(a.OriginalParticipantNameSnapshot),
                        participantId,
                        RequiredText(a.ParticipantNameSnapshot));
                }));
        }).ToList();
        var totals = (data.ParticipantTotals ?? throw new InvalidDataException()).Select(t =>
        {
            var participantId = RequiredId(t.ParticipantId);

            return new ParticipantAssignmentTotal(
                participantId,
                RequiredText(t.ParticipantNameSnapshot),
                t.AssignmentCount);
        }).ToList();

        return new GeneratedSchedule(RequiredId(data.Id), data.GeneratedAtUtc, events, totals);
    }

    private static Guid RequiredId(Guid id) =>
        id == Guid.Empty ? throw new InvalidDataException() : id;

    private static string RequiredText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidDataException() : value;

    private static PersistedState FromApplicationState(ApplicationState state) => new()
    {
        SchemaVersion = ApplicationState.CurrentSchemaVersion,
        Participants = state.Participants.Select(p => new PersistedParticipant(p.Id, p.Name, p.SortOrder)).ToList(),
        EventTypes = state.EventTypes.Select(e => new PersistedEventType(
            e.Id, e.Name, e.Tasks.Select(t => new PersistedTask(t.Id, t.Name, t.SortOrder)).ToList())).ToList(),
        ScheduledEvents = state.ScheduledEvents.Select(e =>
            new PersistedScheduledEvent(e.Id, e.Date, e.EventTypeId, e.Description)).ToList(),
        LatestSchedule = state.LatestSchedule is null ? null : FromGeneratedSchedule(state.LatestSchedule),
        IsScheduleStale = state.IsScheduleStale
    };

    private static PersistedGeneratedSchedule FromGeneratedSchedule(GeneratedSchedule schedule) => new(
        schedule.Id,
        schedule.GeneratedAtUtc,
        schedule.Events.Select(e => new PersistedGeneratedScheduleEvent(
            e.ScheduledEventId,
            e.Date,
            e.EventTypeId,
            e.EventTypeNameSnapshot,
            e.EventDescriptionSnapshot,
            e.Assignments.Select(a => new PersistedAssignment(
                a.TaskDefinitionId,
                a.TaskNameSnapshot,
                a.OriginalParticipantId,
                a.OriginalParticipantNameSnapshot,
                a.ParticipantId,
                a.ParticipantNameSnapshot)).ToList())).ToList(),
        schedule.ParticipantTotals.Select(t =>
            new PersistedParticipantTotal(t.ParticipantId, t.ParticipantNameSnapshot, t.AssignmentCount)).ToList());

    private sealed class PersistedState
    {
        public int SchemaVersion { get; set; }
        public List<PersistedParticipant>? Participants { get; set; }
        public List<PersistedEventType>? EventTypes { get; set; }
        public List<PersistedScheduledEvent>? ScheduledEvents { get; set; }
        public PersistedGeneratedSchedule? LatestSchedule { get; set; }
        public bool IsScheduleStale { get; set; }
    }

    private sealed record PersistedParticipant(Guid Id, string? Name, int SortOrder);
    private sealed record PersistedTask(Guid Id, string? Name, int SortOrder);
    private sealed record PersistedEventType(Guid Id, string? Name, List<PersistedTask>? Tasks);
    private sealed record PersistedScheduledEvent(Guid Id, DateOnly Date, Guid EventTypeId, string? Description);
    private sealed record PersistedGeneratedSchedule(
        Guid Id,
        DateTimeOffset GeneratedAtUtc,
        List<PersistedGeneratedScheduleEvent>? Events,
        List<PersistedParticipantTotal>? ParticipantTotals);
    private sealed record PersistedGeneratedScheduleEvent(
        Guid ScheduledEventId,
        DateOnly Date,
        Guid EventTypeId,
        string? EventTypeNameSnapshot,
        string? EventDescriptionSnapshot,
        List<PersistedAssignment>? Assignments);
    private sealed record PersistedAssignment(
        Guid TaskDefinitionId,
        string? TaskNameSnapshot,
        Guid OriginalParticipantId,
        string? OriginalParticipantNameSnapshot,
        Guid ParticipantId,
        string? ParticipantNameSnapshot);
    private sealed record PersistedParticipantTotal(Guid ParticipantId, string? ParticipantNameSnapshot, int AssignmentCount);
}
