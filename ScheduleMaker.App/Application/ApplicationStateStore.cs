namespace ScheduleMaker.App.Application;

public sealed class ApplicationStateStore
{
    private readonly IApplicationStatePersistence? persistence;
    private readonly ILogger<ApplicationStateStore>? logger;

    public ApplicationStateStore(
        IApplicationStatePersistence? persistence = null,
        ILogger<ApplicationStateStore>? logger = null)
    {
        this.persistence = persistence;
        this.logger = logger;
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
            logger?.LogError(ex, "Unable to load application state from browser storage.");
            PersistenceError = "Saved schedule data could not be accessed.";
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
            logger?.LogError(ex, "Unable to save application state to browser storage.");
            PersistenceError = "Your changes could not be saved.";
        }
    }
}
