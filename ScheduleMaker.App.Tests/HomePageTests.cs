using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ScheduleMaker.App.Application;
using ScheduleMaker.App.Domain;
using ScheduleMaker.App.Pages;
using Xunit;

namespace ScheduleMaker.App.Tests;

public sealed class HomePageTests
{
    [Fact]
    public void HomePage_Shows_Empty_State_And_Disables_Generation()
    {
        using var context = CreateContext();

        var cut = context.Render<Home>();

        Assert.Contains("Add your first participant", cut.Markup);
        Assert.Contains("No event types yet", cut.Markup);
        Assert.Contains("No dated events yet", cut.Markup);
        Assert.Contains("Add at least one participant before generating a schedule.", cut.Markup);
        Assert.True(cut.Find("#generate-schedule").HasAttribute("disabled"));
    }

    [Fact]
    public void HomePage_Completes_Organizer_Workflow_And_Marks_Schedule_Stale_After_Change()
    {
        using var context = CreateContext();
        var cut = context.Render<Home>();

        AddParticipant(cut, "Alex");
        AddParticipant(cut, "Jamie");
        Assert.Equal(
            ["Alex", "Jamie"],
            cut.FindAll("[aria-label='Participants'] li").Select(item => item.QuerySelector("span")!.TextContent.Trim()));

        var eventTypeForm = cut.FindAll("form.participant-form")[1];
        eventTypeForm.QuerySelector("#event-type-name")!.Input("Practice");
        eventTypeForm.Submit();
        var eventType = context.Services.GetRequiredService<ApplicationStateStore>().Current.EventTypes.Single();

        AddTask(cut, eventType.Id, "Setup");
        AddTask(cut, eventType.Id, "Cleanup");
        Assert.Equal(["Setup", "Cleanup"], cut.FindAll(".event-type-card .task-list li span").Select(item => item.TextContent.Trim()));

        cut.Find("#scheduled-event-date").Change("2026-09-01");
        cut.Find("#scheduled-event-type").Change(eventType.Id.ToString());
        cut.Find("#scheduled-event-description").Change("Home match");
        cut.Find("form.event-form").Submit();
        cut.Find("#generate-schedule").Click();

        Assert.Contains("Current schedule", cut.Markup);
        Assert.Contains("September 1, 2026", cut.Markup);
        Assert.Contains("Home match", cut.Markup);
        Assert.Contains("Setup", cut.Markup);
        Assert.Contains("Cleanup", cut.Markup);
        Assert.Contains("Alex", cut.Markup);
        Assert.Contains("Jamie", cut.Markup);
        Assert.Equal(2, cut.FindAll(".assignment-total-list li").Count);

        cut.Find("button[aria-label='Remove Alex']").Click();

        Assert.Contains("Stale schedule", cut.Markup);
        Assert.Contains("Regenerate to refresh the assignments.", cut.Markup);
        Assert.DoesNotContain("Alex", cut.Find("[aria-label='Participants']").TextContent);
    }

    [Fact]
    public void HomePage_Removes_Tasks_And_Events()
    {
        using var context = CreateContext();
        var cut = context.Render<Home>();

        AddParticipant(cut, "Alex");
        var eventTypeForm = cut.FindAll("form.participant-form")[1];
        eventTypeForm.QuerySelector("#event-type-name")!.Input("Practice");
        eventTypeForm.Submit();
        var eventType = context.Services.GetRequiredService<ApplicationStateStore>().Current.EventTypes.Single();
        AddTask(cut, eventType.Id, "Setup");
        cut.Find("#scheduled-event-date").Change("2026-09-01");
        cut.Find("#scheduled-event-type").Change(eventType.Id.ToString());
        cut.Find("#scheduled-event-description").Change("Home match");
        cut.Find("form.event-form").Submit();

        cut.Find("button[aria-label='Remove task Setup from Practice']").Click();
        Assert.Contains("Add a task before using this event type", cut.Markup);

        cut.Find("button[aria-label^='Remove Practice on']").Click();
        Assert.Contains("No dated events yet", cut.Markup);
    }

    [Fact]
    public void TeamManagementPage_Can_Cancel_And_Save_Event_Type_Edits()
    {
        using var context = CreateTeamContext();
        var cut = context.Render<TeamManagementPage>();

        var eventTypeForm = cut.FindAll("form.participant-form")[1];
        eventTypeForm.QuerySelector("#event-type-name")!.Input("Practice");
        eventTypeForm.Submit();
        var eventType = context.Services.GetRequiredService<ApplicationStateStore>().Current.EventTypes.Single();
        AddTeamTask(cut, eventType.Id, "Setup");
        AddTeamTask(cut, eventType.Id, "Cleanup");

        cut.FindAll(".event-type-card button").Single(button => button.TextContent.Trim() == "Edit").Click();
        cut.Find($"#edit-event-type-name-{eventType.Id}").Input("Changed");
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Cancel").Click();
        Assert.Contains("Practice", cut.Find(".event-type-card").TextContent);
        Assert.DoesNotContain("Changed", cut.Find(".event-type-card").TextContent);

        cut.FindAll(".event-type-card button").Single(button => button.TextContent.Trim() == "Edit").Click();
        cut.Find($"#edit-event-type-name-{eventType.Id}").Input("  Match  ");
        cut.FindAll("input[aria-label='Task name']")[0].Input("  Warm up ");
        cut.FindAll("button").First(button => button.TextContent.Trim() == "Move down").Click();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Save changes").Click();

        var updated = context.Services.GetRequiredService<ApplicationStateStore>().Current.EventTypes.Single();
        Assert.Equal("Match", updated.Name);
        Assert.Equal(["Cleanup", "Warm up"], updated.Tasks.Select(task => task.Name));
        Assert.DoesNotContain("Save changes", cut.Markup);
    }

    [Fact]
    public void HomePage_Shows_Actionable_Validation_For_Duplicate_Participants_And_Missing_Event_Fields()
    {
        using var context = CreateContext();
        var cut = context.Render<Home>();

        AddParticipant(cut, "Alex");
        AddParticipant(cut, " alex ");
        Assert.Contains("Participant names must be unique.", cut.Markup);

        var eventTypeForm = cut.FindAll("form.participant-form")[1];
        eventTypeForm.QuerySelector("#event-type-name")!.Input("Practice");
        eventTypeForm.Submit();
        cut.Find("form.event-form").Submit();

        Assert.Contains("Choose a date.", cut.Markup);

        cut.Find("#scheduled-event-date").Change("2026-09-01");
        cut.Find("form.event-form").Submit();

        Assert.Contains("Select an event type.", cut.Markup);
    }

    private static void AddParticipant(IRenderedComponent<Home> cut, string name)
    {
        cut.Find("#participant-name").Input(name);
        cut.FindAll("form.participant-form")[0].Submit();
    }

    private static void AddTask(IRenderedComponent<Home> cut, Guid eventTypeId, string name)
    {
        var input = cut.Find($"#task-name-{eventTypeId}");
        input.Input(name);
        cut.FindAll("form.task-form")
            .Single(form => form.QuerySelector($"#task-name-{eventTypeId}") is not null)
            .Submit();
    }

    private static void AddTeamTask(IRenderedComponent<TeamManagementPage> cut, Guid eventTypeId, string name)
    {
        var input = cut.Find($"#task-name-{eventTypeId}");
        input.Input(name);
        cut.FindAll("form.task-form")
            .Single(form => form.QuerySelector($"#task-name-{eventTypeId}") is not null)
            .Submit();
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        var stateStore = new ApplicationStateStore(new InMemoryPersistence());
        context.Services.AddSingleton(stateStore);
        context.Services.AddSingleton(new ConfigurationStateService(stateStore));
        return context;
    }

    private static BunitContext CreateTeamContext()
    {
        var context = new BunitContext();
        var stateStore = new ApplicationStateStore(new InMemoryPersistence());
        context.Services.AddSingleton(stateStore);
        context.Services.AddSingleton(new ConfigurationStateService(stateStore));
        return context;
    }

    private sealed class InMemoryPersistence : IApplicationStatePersistence
    {
        public Task<ApplicationState> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationState.Empty);

        public Task SaveAsync(ApplicationState state, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
