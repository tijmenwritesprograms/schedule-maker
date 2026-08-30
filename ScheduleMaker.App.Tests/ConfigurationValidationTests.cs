using ScheduleMaker.App.Application;
using ScheduleMaker.App.Domain;
using Xunit;

namespace ScheduleMaker.App.Tests;

public sealed class ConfigurationValidationTests
{
    [Fact]
    public void Validate_Reports_Empty_Configuration_And_First_Next_Step()
    {
        var result = ConfigurationValidation.Validate(ApplicationState.Empty);

        Assert.False(result.CanGenerate);
        Assert.Equal("Add at least one participant to get started.", result.NextStepMessage);
        Assert.Contains(result.Issues, issue => issue.Message == "Add at least one participant before generating a schedule.");
        Assert.Contains(result.Issues, issue => issue.Message == "Add at least one event type before generating a schedule.");
        Assert.Contains(result.Issues, issue => issue.Message == "Add at least one event before generating a schedule.");
    }

    [Fact]
    public void Validate_Reports_Duplicate_Names_After_Trimming_And_Case_Insensitive_Comparison()
    {
        var participants = new[]
        {
            new Participant(Guid.NewGuid(), " Alex ", 0),
            new Participant(Guid.NewGuid(), "alex", 1)
        };
        var tasks = new[]
        {
            new TaskDefinition(Guid.NewGuid(), " Setup ", 0),
            new TaskDefinition(Guid.NewGuid(), "setup", 1)
        };
        var firstEventType = new EventType(Guid.NewGuid(), " Practice ", tasks);
        var secondEventType = new EventType(Guid.NewGuid(), "practice", [new TaskDefinition(Guid.NewGuid(), "Cleanup", 0)]);
        var scheduledEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 1), firstEventType.Id, null);
        var state = new ApplicationState(participants, [firstEventType, secondEventType], [scheduledEvent], null, false);

        var result = ConfigurationValidation.Validate(state);

        Assert.False(result.CanGenerate);
        Assert.Contains(result.Issues, issue => issue.Scope == ConfigurationValidationScope.Participant && issue.Message == "Participant names must be unique.");
        Assert.Contains(result.Issues, issue => issue.Scope == ConfigurationValidationScope.EventType && issue.Message == "Event type names must be unique.");
        Assert.Contains(result.Issues, issue => issue.Scope == ConfigurationValidationScope.Task && issue.Message == "Task names in event type \"Practice\" must be unique.");
    }

    [Fact]
    public void Validate_Reports_Invalid_Event_References_And_Default_Date()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [new TaskDefinition(Guid.NewGuid(), "Setup", 0)]);
        var missingReferenceEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 1), Guid.NewGuid(), null);
        var missingDateEvent = new ScheduledEvent(Guid.NewGuid(), default, eventType.Id, null);
        var state = new ApplicationState([participant], [eventType], [missingReferenceEvent, missingDateEvent], null, false);

        var result = ConfigurationValidation.Validate(state);

        Assert.False(result.CanGenerate);
        Assert.Equal("Fix the validation errors below before generating a schedule.", result.NextStepMessage);
        Assert.Contains(result.Issues, issue => issue.EntityId == missingReferenceEvent.Id && issue.Message == "The event on 2026-09-01 must reference an existing event type.");
        Assert.Contains(result.Issues, issue => issue.EntityId == missingDateEvent.Id && issue.Message == "Events must have a date.");
    }

    [Fact]
    public void Validate_Reports_Event_Description_Length()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [new TaskDefinition(Guid.NewGuid(), "Setup", 0)]);
        var scheduledEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 1), eventType.Id, new string('x', 501));
        var state = new ApplicationState([participant], [eventType], [scheduledEvent], null, false);

        var result = ConfigurationValidation.Validate(state);

        Assert.False(result.CanGenerate);
        Assert.Contains(result.Issues, issue => issue.EntityId == scheduledEvent.Id && issue.Message == "Event descriptions cannot be longer than 500 characters.");
    }

    [Fact]
    public void Validate_Allows_Generation_For_Valid_Configuration()
    {
        var participant = new Participant(Guid.NewGuid(), "Alex", 0);
        var eventType = new EventType(Guid.NewGuid(), "Practice", [new TaskDefinition(Guid.NewGuid(), "Setup", 0)]);
        var scheduledEvent = new ScheduledEvent(Guid.NewGuid(), new DateOnly(2026, 9, 1), eventType.Id, "Home");
        var state = new ApplicationState([participant], [eventType], [scheduledEvent], null, false);

        var result = ConfigurationValidation.Validate(state);

        Assert.True(result.CanGenerate);
        Assert.Equal("Your configuration is ready to generate a schedule.", result.NextStepMessage);
        Assert.Empty(result.Issues);
    }
}
