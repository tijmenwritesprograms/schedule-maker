namespace ScheduleMaker.App.Application;

public sealed class ApplicationStateStore
{
    private readonly IApplicationStatePersistence? persistence;

    public ApplicationStateStore(IApplicationStatePersistence? persistence = null)
    {
        this.persistence = persistence;
    }

    public ApplicationState Current { get; private set; } = ApplicationState.Empty;

    public string? PersistenceError { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (persistence is null)
        {
            return;
        }

        try
        {
            Current = await persistence.LoadAsync(cancellationToken);
            PersistenceError = null;
        }
        catch (Exception ex)
        {
            PersistenceError = ex.Message;
        }
    }

    public void Replace(ApplicationState state)
    {
        Current = state ?? throw new ArgumentNullException(nameof(state));
    }

    public async Task PersistAsync(CancellationToken cancellationToken = default)
    {
        if (persistence is null)
        {
            return;
        }

        try
        {
            await persistence.SaveAsync(Current, cancellationToken);
            PersistenceError = null;
        }
        catch (Exception ex)
        {
            PersistenceError = ex.Message;
        }
    }
}
