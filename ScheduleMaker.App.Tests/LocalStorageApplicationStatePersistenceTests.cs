using ScheduleMaker.App.Application;
using ScheduleMaker.App.Domain;
using Xunit;

namespace ScheduleMaker.App.Tests;

public sealed class LocalStorageApplicationStatePersistenceTests
{
    [Fact]
    public async Task Save_And_Load_RoundTrips_Complete_State()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var replacementParticipantId = Guid.NewGuid();
        var task = new TaskDefinition(Guid.NewGuid(), "Set up", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [task]);
        var scheduledEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 1), eventType.Id, "Home");
        var schedule = new GeneratedSchedule(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [new GeneratedScheduleEvent(
                scheduledEvent.Id, scheduledEvent.Date, eventType.Id, eventType.Name, scheduledEvent.Description,
                [new GeneratedTaskAssignment(
                    task.Id,
                    task.Name,
                    participant.Id,
                    participant.Name,
                    replacementParticipantId,
                    "Jamie")])],
            [
                new ParticipantAssignmentTotal(participant.Id, participant.Name, 0),
                new ParticipantAssignmentTotal(replacementParticipantId, "Jamie", 1)
            ]);
        var state = new ApplicationState([participant], [eventType], [scheduledEvent], schedule, true);
        var storage = new FakeLocalStorage();
        var persistence = new LocalStorageApplicationStatePersistence(storage);

        await persistence.SaveAsync(state);
        var restored = await persistence.LoadAsync();

        Assert.Equal(state.SchemaVersion, restored.SchemaVersion);
        Assert.Equal(participant.Name, restored.Participants[0].Name);
        Assert.Equal(task.Name, restored.EventTypes[0].Tasks[0].Name);
        Assert.Equal(scheduledEvent.Description, restored.ScheduledEvents[0].Description);
        Assert.Equal(schedule.Id, restored.LatestSchedule!.Id);
        Assert.True(restored.LatestSchedule.Events[0].Assignments[0].IsManuallyEdited);
        Assert.Equal("Alex", restored.LatestSchedule.Events[0].Assignments[0].OriginalParticipantNameSnapshot);
        Assert.Equal("Jamie", restored.LatestSchedule.Events[0].Assignments[0].ParticipantNameSnapshot);
        Assert.Equal("Alex", restored.LatestSchedule.ParticipantTotals[0].ParticipantNameSnapshot);
        Assert.True(restored.IsScheduleStale);
    }

    [Fact]
    public async Task Load_Returns_Empty_State_When_Storage_Is_Missing_Malformed_Or_Incompatible()
    {
        var storage = new FakeLocalStorage();
        var persistence = new LocalStorageApplicationStatePersistence(storage);

        Assert.Empty((await persistence.LoadAsync()).Participants);

        storage.Value = "{ not json";
        Assert.Empty((await persistence.LoadAsync()).EventTypes);

        storage.Value = """{"schemaVersion":999,"participants":[],"eventTypes":[],"scheduledEvents":[]}""";
        Assert.Empty((await persistence.LoadAsync()).ScheduledEvents);

        storage.Value = """{"schemaVersion":1,"participants":[],"eventTypes":[],"scheduledEvents":[{"id":"00000000-0000-0000-0000-000000000001","date":"2026-09-01","eventTypeId":"00000000-0000-0000-0000-000000000000"}]}""";
        Assert.Empty((await persistence.LoadAsync()).ScheduledEvents);
    }

    [Fact]
    public async Task Load_Preserves_Events_With_Missing_Event_Type_References()
    {
        var storage = new FakeLocalStorage
        {
            Value = """
                {"schemaVersion":1,"participants":[{"id":"00000000-0000-0000-0000-000000000010","name":"Alex","sortOrder":0}],"eventTypes":[],"scheduledEvents":[{"id":"00000000-0000-0000-0000-000000000001","date":"2026-09-01","eventTypeId":"00000000-0000-0000-0000-000000000002","description":"Still here"}],"isScheduleStale":true}
                """
        };
        var persistence = new LocalStorageApplicationStatePersistence(storage);

        var restored = await persistence.LoadAsync();

        Assert.Single(restored.Participants);
        Assert.Single(restored.ScheduledEvents);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), restored.ScheduledEvents[0].EventTypeId);
        Assert.Equal("Still here", restored.ScheduledEvents[0].Description);
    }

    [Fact]
    public async Task Separate_Persistence_Instances_Restore_State_After_Application_Reload()
    {
        var storage = new FakeLocalStorage();
        var firstPersistence = new LocalStorageApplicationStatePersistence(storage);
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var state = new ApplicationState([participant], [], [], null, false);

        await firstPersistence.SaveAsync(state);

        var reloadedState = await new LocalStorageApplicationStatePersistence(storage).LoadAsync();

        Assert.Equal("Alex", Assert.Single(reloadedState.Participants).Name);
    }

    private sealed class FakeLocalStorage : ILocalStorage
    {
        public string? Value { get; set; }

        public ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default) => ValueTask.FromResult(Value);

        public ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Value = value;
            return ValueTask.CompletedTask;
        }
    }
}
