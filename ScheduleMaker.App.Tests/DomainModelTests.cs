using ScheduleMaker.App.Application;
using ScheduleMaker.App.Domain;
using Xunit;

namespace ScheduleMaker.App.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void EventType_Preserves_Task_Order()
    {
        var firstTask = new TaskDefinition(Guid.NewGuid(), "Set Up", 1);
        var secondTask = new TaskDefinition(Guid.NewGuid(), "Clean Up", 2);

        var eventType = new EventType(Guid.NewGuid(), "Practice", [firstTask, secondTask]);

        Assert.Equal([firstTask, secondTask], eventType.Tasks);
    }

    [Fact]
    public void EventType_Allows_Empty_Task_List_Until_Tasks_Are_Added()
    {
        var eventType = new EventType(Guid.NewGuid(), "Practice", []);

        Assert.Empty(eventType.Tasks);
    }

    [Fact]
    public void EventType_Requires_Task_Collection()
    {
        Assert.Throws<ArgumentNullException>(() => new EventType(Guid.NewGuid(), "Practice", null!));
    }

    [Fact]
    public void Participant_Requires_Non_Empty_Name()
    {
        Assert.Throws<ArgumentException>(() => new Participant(Guid.NewGuid(), "   ", 0));
    }

    [Fact]
    public void GeneratedSchedule_Snapshots_Are_Value_Based()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var replacement = new Participant(Guid.NewGuid(), "Taylor", 1);
        var task = new TaskDefinition(Guid.NewGuid(), "Drinks", 0);
        var sourceEventDescription = "Evening game";

        var assignment = new GeneratedTaskAssignment(
            task.Id,
            task.Name,
            participant.Id,
            participant.Name,
            replacement.Id,
            replacement.Name);

        var generatedEvent = new GeneratedScheduleEvent(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            Guid.NewGuid(),
            eventTypeNameSnapshot: "Game Night",
            eventDescriptionSnapshot: sourceEventDescription,
            [assignment]);

        sourceEventDescription = "Changed description";
        participant = new Participant(participant.Id, "Jordan", participant.SortOrder);
        replacement = new Participant(replacement.Id, "Casey", replacement.SortOrder);
        task = new TaskDefinition(task.Id, "Equipment", task.SortOrder);

        Assert.Equal("Game Night", generatedEvent.EventTypeNameSnapshot);
        Assert.Equal("Evening game", generatedEvent.EventDescriptionSnapshot);
        Assert.Equal("Drinks", generatedEvent.Assignments[0].TaskNameSnapshot);
        Assert.Equal("Alex", generatedEvent.Assignments[0].OriginalParticipantNameSnapshot);
        Assert.Equal("Taylor", generatedEvent.Assignments[0].ParticipantNameSnapshot);
        Assert.True(generatedEvent.Assignments[0].IsManuallyEdited);
    }

    [Fact]
    public void GeneratedSchedule_Has_Manual_Changes_When_Any_Assignment_Was_Replaced()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var replacement = new Participant(Guid.NewGuid(), "Taylor", 1);
        var task = new TaskDefinition(Guid.NewGuid(), "Drinks", 0);
        var generatedSchedule = new GeneratedSchedule(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [
                new GeneratedScheduleEvent(
                    Guid.NewGuid(),
                    new DateOnly(2026, 9, 1),
                    Guid.NewGuid(),
                    "Game Night",
                    null,
                    [new GeneratedTaskAssignment(
                        task.Id,
                        task.Name,
                        participant.Id,
                        participant.Name,
                        replacement.Id,
                        replacement.Name)])
            ],
            [
                new ParticipantAssignmentTotal(participant.Id, participant.Name, 0),
                new ParticipantAssignmentTotal(replacement.Id, replacement.Name, 1)
            ]);

        Assert.True(generatedSchedule.HasManualChanges);
    }

    [Fact]
    public void ApplicationState_Represents_Current_Configuration_And_Schedule_Status()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var task = new TaskDefinition(Guid.NewGuid(), "Snacks", 0);
        var eventType = new EventType(Guid.NewGuid(), "Match Day", [task]);
        var scheduledEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 2), eventType.Id, "Home");
        var generatedSchedule = new GeneratedSchedule(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [
                new GeneratedScheduleEvent(
                    scheduledEvent.Id,
                    scheduledEvent.Date,
                    eventType.Id,
                    eventType.Name,
                    scheduledEvent.Description,
                    [new GeneratedTaskAssignment(task.Id, task.Name, participant.Id, participant.Name)])
            ],
            [new ParticipantAssignmentTotal(participant.Id, participant.Name, 1)]);

        var state = new ApplicationState(
            [participant],
            [eventType],
            [scheduledEvent],
            generatedSchedule,
            isScheduleStale: true);

        Assert.Single(state.Participants);
        Assert.Single(state.EventTypes);
        Assert.Single(state.ScheduledEvents);
        Assert.Same(generatedSchedule, state.LatestSchedule);
        Assert.True(state.IsScheduleStale);
    }

    [Fact]
    public void ParticipantAssignmentTotal_Requires_Non_Negative_Count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParticipantAssignmentTotal(Guid.NewGuid(), "Alex", -1));
    }

    [Fact]
    public void ApplicationStateStore_Starts_With_Empty_State()
    {
        var store = new ApplicationStateStore();

        Assert.Empty(store.Current.Participants);
        Assert.Empty(store.Current.EventTypes);
        Assert.Empty(store.Current.ScheduledEvents);
        Assert.Null(store.Current.LatestSchedule);
        Assert.False(store.Current.IsScheduleStale);
    }
}
