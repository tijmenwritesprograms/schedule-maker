namespace ScheduleMaker.App.Application;

public interface ILocalStorage
{
    ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default);

    ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default);
}
