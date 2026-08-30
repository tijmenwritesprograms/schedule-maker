namespace ScheduleMaker.App.Application;

public interface IApplicationStatePersistence
{
    Task<ApplicationState> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ApplicationState state, CancellationToken cancellationToken = default);
}
