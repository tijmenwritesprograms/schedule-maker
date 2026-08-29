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
    public void GeneratedSchedule_Snapshots_Are_Value_Based()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var task = new TaskDefinition(Guid.NewGuid(), "Drinks", 0);
        var sourceEventDescription = "Evening game";

        var assignment = new GeneratedTaskAssignment(
            task.Id,
            task.Name,
            participant.Id,
            participant.Name);

        var generatedEvent = new GeneratedScheduleEvent(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            Guid.NewGuid(),
            eventTypeNameSnapshot: "Game Night",
            eventDescriptionSnapshot: sourceEventDescription,
            [assignment]);

        sourceEventDescription = "Changed description";
        participant = new Participant(participant.Id, "Taylor", participant.SortOrder);

        Assert.Equal("Game Night", generatedEvent.EventTypeNameSnapshot);
        Assert.Equal("Evening game", generatedEvent.EventDescriptionSnapshot);
        Assert.Equal("Alex", generatedEvent.Assignments[0].ParticipantNameSnapshot);
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
}
