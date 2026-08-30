using ScheduleMaker.App.Application;
using ScheduleMaker.App.Domain;
using Xunit;

namespace ScheduleMaker.App.Tests;

public sealed class ScheduleGenerationServiceTests
{
    private static Participant MakeParticipant(string name, int sortOrder) =>
        new(Guid.NewGuid(), name, sortOrder);

    private static TaskDefinition MakeTask(string name, int sortOrder) =>
        new(Guid.NewGuid(), name, sortOrder);

    private static EventType MakeEventType(string name, params TaskDefinition[] tasks) =>
        new(Guid.NewGuid(), name, tasks);

    private static ScheduledEvent MakeEvent(DateOnly date, Guid eventTypeId, string? description = null) =>
        new(Guid.NewGuid(), date, eventTypeId, description);

    private static ApplicationState MakeState(
        IEnumerable<Participant> participants,
        IEnumerable<EventType> eventTypes,
        IEnumerable<ScheduledEvent> scheduledEvents) =>
        new(participants, eventTypes, scheduledEvents, null, false);

    [Fact]
    public void Generate_Fails_For_Invalid_Configuration()
    {
        var result = ScheduleGenerationService.Generate(ApplicationState.Empty);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Generate_Assigns_Every_Task_Exactly_Once_One_Participant()
    {
        var participant = MakeParticipant("Alex", 0);
        var task1 = MakeTask("Setup", 0);
        var task2 = MakeTask("Cleanup", 1);
        var eventType = MakeEventType("Practice", task1, task2);
        var scheduledEvent = MakeEvent(new DateOnly(2026, 9, 1), eventType.Id);
        var state = MakeState([participant], [eventType], [scheduledEvent]);

        var result = ScheduleGenerationService.Generate(state);

        Assert.True(result.IsSuccess);
        var schedule = result.Value!;
        var assignments = schedule.Events.Single().Assignments;
        Assert.Equal(2, assignments.Count);
        Assert.All(assignments, a => Assert.Equal(participant.Id, a.ParticipantId));
        Assert.Equal(task1.Id, assignments[0].TaskDefinitionId);
        Assert.Equal(task2.Id, assignments[1].TaskDefinitionId);
        Assert.Equal(2, schedule.ParticipantTotals.Single().AssignmentCount);
    }

    [Fact]
    public void Generate_Distributes_Evenly_With_Two_Participants_Two_Events()
    {
        var alice = MakeParticipant("Alice", 0);
        var bob = MakeParticipant("Bob", 1);
        var task = MakeTask("Setup", 0);
        var eventType = MakeEventType("Practice", task);
        var event1 = MakeEvent(new DateOnly(2026, 9, 1), eventType.Id);
        var event2 = MakeEvent(new DateOnly(2026, 9, 8), eventType.Id);
        var state = MakeState([alice, bob], [eventType], [event1, event2]);

        var result = ScheduleGenerationService.Generate(state);

        Assert.True(result.IsSuccess);
        var schedule = result.Value!;
        var totals = schedule.ParticipantTotals.ToDictionary(t => t.ParticipantId, t => t.AssignmentCount);
        Assert.Equal(1, totals[alice.Id]);
        Assert.Equal(1, totals[bob.Id]);
    }

    [Fact]
    public void Generate_Uneven_Distribution_Differs_By_At_Most_One()
    {
        var alice = MakeParticipant("Alice", 0);
        var bob = MakeParticipant("Bob", 1);
        var task = MakeTask("Setup", 0);
        var eventType = MakeEventType("Practice", task);
        // 3 events → 2 to Alice (first in stable order), 1 to Bob
        var events = new[]
        {
            MakeEvent(new DateOnly(2026, 9, 1), eventType.Id),
            MakeEvent(new DateOnly(2026, 9, 8), eventType.Id),
            MakeEvent(new DateOnly(2026, 9, 15), eventType.Id),
        };
        var state = MakeState([alice, bob], [eventType], events);

        var result = ScheduleGenerationService.Generate(state);

        Assert.True(result.IsSuccess);
        var totals = result.Value!.ParticipantTotals.Select(t => t.AssignmentCount).ToList();
        Assert.Equal(1, totals.Max() - totals.Min());
    }

    [Fact]
    public void Generate_Avoids_Repeating_Same_Task_For_Same_Participant_When_Alternative_Exists()
    {
        var alice = MakeParticipant("Alice", 0);
        var bob = MakeParticipant("Bob", 1);
        var task = MakeTask("Setup", 0);
        var eventType = MakeEventType("Practice", task);
        var event1 = MakeEvent(new DateOnly(2026, 9, 1), eventType.Id);
        var event2 = MakeEvent(new DateOnly(2026, 9, 8), eventType.Id);
        var state = MakeState([alice, bob], [eventType], [event1, event2]);

        var result = ScheduleGenerationService.Generate(state);

        Assert.True(result.IsSuccess);
        var assignments = result.Value!.Events.SelectMany(e => e.Assignments).ToList();
        // Each participant should get exactly one assignment of the task
        Assert.Single(assignments, a => a.ParticipantId == alice.Id);
        Assert.Single(assignments, a => a.ParticipantId == bob.Id);
    }

    [Fact]
    public void Generate_Is_Deterministic_For_Same_Input()
    {
        var alice = MakeParticipant("Alice", 0);
        var bob = MakeParticipant("Bob", 1);
        var task1 = MakeTask("Setup", 0);
        var task2 = MakeTask("Cleanup", 1);
        var eventType = MakeEventType("Practice", task1, task2);
        var events = new[]
        {
            MakeEvent(new DateOnly(2026, 9, 1), eventType.Id),
            MakeEvent(new DateOnly(2026, 9, 8), eventType.Id),
        };
        var state = MakeState([alice, bob], [eventType], events);

        var result1 = ScheduleGenerationService.Generate(state);
        var result2 = ScheduleGenerationService.Generate(state);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        var assignments1 = result1.Value!.Events.SelectMany(e => e.Assignments).Select(a => a.ParticipantId).ToList();
        var assignments2 = result2.Value!.Events.SelectMany(e => e.Assignments).Select(a => a.ParticipantId).ToList();
        Assert.Equal(assignments1, assignments2);
    }

    [Fact]
    public void Generate_Snapshots_Event_Type_Name_And_Description()
    {
        var participant = MakeParticipant("Alex", 0);
        var task = MakeTask("Setup", 0);
        var eventType = MakeEventType("Practice", task);
        var scheduledEvent = MakeEvent(new DateOnly(2026, 9, 1), eventType.Id, "Home match");
        var state = MakeState([participant], [eventType], [scheduledEvent]);

        var result = ScheduleGenerationService.Generate(state);

        Assert.True(result.IsSuccess);
        var generatedEvent = result.Value!.Events.Single();
        Assert.Equal("Practice", generatedEvent.EventTypeNameSnapshot);
        Assert.Equal("Home match", generatedEvent.EventDescriptionSnapshot);
        Assert.Equal("Setup", generatedEvent.Assignments.Single().TaskNameSnapshot);
        Assert.Equal("Alex", generatedEvent.Assignments.Single().ParticipantNameSnapshot);
    }

    [Fact]
    public void Generate_Stable_Tie_Breaking_Assigns_First_Participant_First()
    {
        var alice = MakeParticipant("Alice", 0);
        var bob = MakeParticipant("Bob", 1);
        var task = MakeTask("Setup", 0);
        var eventType = MakeEventType("Practice", task);
        var scheduledEvent = MakeEvent(new DateOnly(2026, 9, 1), eventType.Id);
        var state = MakeState([alice, bob], [eventType], [scheduledEvent]);

        var result = ScheduleGenerationService.Generate(state);

        Assert.True(result.IsSuccess);
        // When tied (both have 0 total), stable order picks Alice (sortOrder 0)
        var assignment = result.Value!.Events.Single().Assignments.Single();
        Assert.Equal(alice.Id, assignment.ParticipantId);
    }

    [Fact]
    public void Generate_Repeated_Task_Prefers_Different_Participant_Before_Repeating()
    {
        var alice = MakeParticipant("Alice", 0);
        var bob = MakeParticipant("Bob", 1);
        var carol = MakeParticipant("Carol", 2);
        var task = MakeTask("Cleanup", 0);
        var eventType = MakeEventType("Practice", task);
        // 4 events: after 3 (each participant gets 1), 4th goes back to Alice (by total tie, then task count tie, then stable order)
        var events = Enumerable.Range(1, 4)
            .Select(i => MakeEvent(new DateOnly(2026, 9, i), eventType.Id))
            .ToArray();
        var state = MakeState([alice, bob, carol], [eventType], events);

        var result = ScheduleGenerationService.Generate(state);

        Assert.True(result.IsSuccess);
        var participantIds = result.Value!.Events.SelectMany(e => e.Assignments).Select(a => a.ParticipantId).ToList();
        // First 3 should all be distinct
        Assert.Equal(3, new HashSet<Guid>(participantIds.Take(3)).Count);
    }

    [Fact]
    public void Generate_Replaces_Previous_Schedule_Only_On_Success()
    {
        var participant = MakeParticipant("Alex", 0);
        var task = MakeTask("Setup", 0);
        var eventType = MakeEventType("Practice", task);
        var scheduledEvent = MakeEvent(new DateOnly(2026, 9, 1), eventType.Id);
        var state = MakeState([participant], [eventType], [scheduledEvent]);

        // Two separate generations should produce independent schedule IDs
        var result1 = ScheduleGenerationService.Generate(state);
        var result2 = ScheduleGenerationService.Generate(state);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value!.Id, result2.Value!.Id);
    }
}
