using Microsoft.JSInterop;

namespace ScheduleMaker.App.Application;

public sealed class BrowserLocalStorage(IJSRuntime jsRuntime) : ILocalStorage
{
    public ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);

    public ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, value);
}
