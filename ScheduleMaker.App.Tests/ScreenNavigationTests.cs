using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ScheduleMaker.App.Application;
using ScheduleMaker.App.Pages;
using Xunit;

namespace ScheduleMaker.App.Tests;

public sealed class ScreenNavigationTests
{
    [Fact]
    public void TeamScreen_Contains_TeamManagement_And_Schedule_Link()
    {
        using var context = CreateContext();

        var cut = context.Render<TeamManagementPage>();

        Assert.Contains("Set up your team", cut.Markup);
        Assert.Contains("Participants", cut.Markup);
        Assert.Contains("Event types", cut.Markup);
        Assert.DoesNotContain("Dated events", cut.Markup);
        Assert.Contains("href=\"/schedule\"", cut.Markup);
    }

    [Fact]
    public void ScheduleScreen_Contains_EventManagement_And_Team_Link()
    {
        using var context = CreateContext();

        var cut = context.Render<ScheduleManagementPage>();

        Assert.Contains("Build your schedule", cut.Markup);
        Assert.Contains("Dated events", cut.Markup);
        Assert.Contains("Generated schedule", cut.Markup);
        Assert.DoesNotContain("Add your first participant", cut.Markup);
        Assert.Contains("href=\"/team\"", cut.Markup);
    }

    private static BunitContext CreateContext()
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
