using ScheduleMaker.App.Application;
using ScheduleMaker.App.Domain;
using Xunit;

namespace ScheduleMaker.App.Tests;

public sealed class ConfigurationStateServiceTests
{
    [Fact]
    public async Task AddParticipant_Trims_Name_And_Persists_State()
    {
        var persistence = new FakePersistence(ApplicationState.Empty);
        var stateStore = new ApplicationStateStore(persistence);
        await stateStore.InitializeAsync();
        var service = new ConfigurationStateService(stateStore);

        var result = await service.AddParticipantAsync("  Alex  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Alex", stateStore.Current.Participants.Single().Name);
        Assert.Equal(1, persistence.SaveCount);
    }

    [Fact]
    public async Task AddParticipant_Rejects_Duplicate_Names_Case_Insensitive()
    {
        var existing = new Participant(Guid.NewGuid(), "Alex", 0);
        var persistence = new FakePersistence(new ApplicationState([existing], [], [], null, false));
        var stateStore = new ApplicationStateStore(persistence);
        await stateStore.InitializeAsync();
        var service = new ConfigurationStateService(stateStore);

        var result = await service.AddParticipantAsync("  alex ");

        Assert.False(result.IsSuccess);
        Assert.Equal("Participant names must be unique.", result.ErrorMessage);
        Assert.Equal(0, persistence.SaveCount);
    }

    [Fact]
    public async Task Participant_Mutations_Mark_Existing_Schedule_As_Stale_And_Preserve_Sort_Order()
    {
        var participant1 = new Participant(Guid.NewGuid(), "Alex", 0);
        var participant2 = new Participant(Guid.NewGuid(), "Jamie", 1);
        var task = new TaskDefinition(Guid.NewGuid(), "Setup", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [task]);
        var scheduledEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 1), eventType.Id, null);
        var schedule = new GeneratedSchedule(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [new GeneratedScheduleEvent(
                scheduledEvent.Id,
                scheduledEvent.Date,
                eventType.Id,
                eventType.Name,
                null,
                [new GeneratedTaskAssignment(task.Id, task.Name, participant1.Id, participant1.Name)])],
            [new ParticipantAssignmentTotal(participant1.Id, participant1.Name, 1)]);

        var persistence = new FakePersistence(
            new ApplicationState([participant1, participant2], [eventType], [scheduledEvent], schedule, false));
        var stateStore = new ApplicationStateStore(persistence);
        await stateStore.InitializeAsync();
        var service = new ConfigurationStateService(stateStore);

        var removeResult = await service.RemoveParticipantAsync(participant1.Id);
        var addResult = await service.AddParticipantAsync("Taylor");

        Assert.True(removeResult.IsSuccess);
        Assert.True(addResult.IsSuccess);
        Assert.True(stateStore.Current.IsScheduleStale);
        Assert.Equal(["Jamie", "Taylor"], stateStore.Current.Participants.Select(participant => participant.Name));
        Assert.Equal([1, 2], stateStore.Current.Participants.Select(participant => participant.SortOrder));
    }

    [Fact]
    public async Task AddScheduledEvent_Keeps_Chronological_Order_And_Stable_Same_Date_Order()
    {
        var task = new TaskDefinition(Guid.NewGuid(), "Setup", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [task]);
        var existingLater = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 5), eventType.Id, "Later");
        var sameDateFirst = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 10), eventType.Id, "First on date");
        var sameDateSecond = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 10), eventType.Id, "Second on date");

        var persistence = new FakePersistence(
            new ApplicationState([], [eventType], [existingLater, sameDateFirst, sameDateSecond], null, false));
        var stateStore = new ApplicationStateStore(persistence);
        await stateStore.InitializeAsync();
        var service = new ConfigurationStateService(stateStore);

        var result = await service.AddScheduledEventAsync(new DateOnly(2026, 9, 8), eventType.Id, "Middle");
        var sameDateResult = await service.AddScheduledEventAsync(new DateOnly(2026, 9, 10), eventType.Id, "Third on date");

        Assert.True(result.IsSuccess);
        Assert.True(sameDateResult.IsSuccess);
        Assert.Equal(
            ["Later", "Middle", "First on date", "Second on date", "Third on date"],
            stateStore.Current.ScheduledEvents.Select(@event => @event.Description));
    }

    [Fact]
    public async Task RemoveTask_Rejects_Last_Task_In_Event_Type()
    {
        var task = new TaskDefinition(Guid.NewGuid(), "Only task", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [task]);
        var persistence = new FakePersistence(new ApplicationState([], [eventType], [], null, false));
        var stateStore = new ApplicationStateStore(persistence);
        await stateStore.InitializeAsync();
        var service = new ConfigurationStateService(stateStore);

        var result = await service.RemoveTaskAsync(eventType.Id, task.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("An event type must contain at least one task.", result.ErrorMessage);
        Assert.Single(stateStore.Current.EventTypes[0].Tasks);
    }

    [Fact]
    public async Task RemoveEventType_Removes_Referencing_Events_And_Marks_Schedule_Stale()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var task = new TaskDefinition(Guid.NewGuid(), "Setup", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [task]);
        var eventTypeToKeep = new EventType(Guid.NewGuid(), "Match", [new TaskDefinition(Guid.NewGuid(), "Cleanup", 0)]);
        var removedEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 10), eventType.Id, "Practice");
        var keptEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 11), eventTypeToKeep.Id, "Match");
        var schedule = new GeneratedSchedule(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [],
            [new ParticipantAssignmentTotal(participant.Id, participant.Name, 0)]);

        var persistence = new FakePersistence(new ApplicationState(
            [participant],
            [eventType, eventTypeToKeep],
            [removedEvent, keptEvent],
            schedule,
            false));
        var stateStore = new ApplicationStateStore(persistence);
        await stateStore.InitializeAsync();
        var service = new ConfigurationStateService(stateStore);

        var result = await service.RemoveEventTypeAsync(eventType.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(stateStore.Current.EventTypes);
        Assert.Single(stateStore.Current.ScheduledEvents);
        Assert.Equal(keptEvent.Id, stateStore.Current.ScheduledEvents[0].Id);
        Assert.True(stateStore.Current.IsScheduleStale);
    }

    [Fact]
    public async Task ApplyGeneratedSchedule_Updates_Schedule_Only_On_Success()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var task = new TaskDefinition(Guid.NewGuid(), "Setup", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [task]);
        var scheduledEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 10), eventType.Id, null);
        var oldSchedule = new GeneratedSchedule(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), [], []);
        var newSchedule = new GeneratedSchedule(Guid.NewGuid(), DateTimeOffset.UtcNow, [], []);

        var persistence = new FakePersistence(new ApplicationState(
            [participant],
            [eventType],
            [scheduledEvent],
            oldSchedule,
            true));
        var stateStore = new ApplicationStateStore(persistence);
        await stateStore.InitializeAsync();
        var service = new ConfigurationStateService(stateStore);

        var failedResult = await service.ApplyGeneratedScheduleAsync(
            ApplicationOperationResult<GeneratedSchedule>.Failure("No participants."));

        Assert.False(failedResult.IsSuccess);
        Assert.Equal(oldSchedule.Id, stateStore.Current.LatestSchedule!.Id);
        Assert.True(stateStore.Current.IsScheduleStale);

        var successResult = await service.ApplyGeneratedScheduleAsync(
            ApplicationOperationResult<GeneratedSchedule>.Success(newSchedule));

        Assert.True(successResult.IsSuccess);
        Assert.Equal(newSchedule.Id, stateStore.Current.LatestSchedule!.Id);
        Assert.False(stateStore.Current.IsScheduleStale);
    }

    private sealed class FakePersistence(ApplicationState initialState) : IApplicationStatePersistence
    {
        public int SaveCount { get; private set; }

        public Task<ApplicationState> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(initialState);

        public Task SaveAsync(ApplicationState state, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
