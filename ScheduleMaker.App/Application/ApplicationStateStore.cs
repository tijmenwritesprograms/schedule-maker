namespace ScheduleMaker.App.Application;

public sealed class ApplicationStateStore
{
    public ApplicationState Current { get; private set; } = ApplicationState.Empty;

    public void Replace(ApplicationState state)
    {
        Current = state ?? throw new ArgumentNullException(nameof(state));
    }
}
