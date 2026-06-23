using Microsoft.JSInterop;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>In-memory mock for <c>sessionStorageInterop</c> JS calls.</summary>
public sealed class FakeJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string?> _store = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string?> Store => _store;

    public void SetRaw(string key, string? value) => _store[key] = value;

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (identifier)
        {
            case "sessionStorageInterop.getItem":
                return ValueTask.FromResult(ReadItem<TValue>(args));
            case "sessionStorageInterop.setItem":
                WriteItem(args);
                return ValueTask.FromResult(default(TValue)!);
            case "sessionStorageInterop.removeItem":
                RemoveItem(args);
                return ValueTask.FromResult(default(TValue)!);
            default:
                throw new NotSupportedException($"Unexpected JS interop call: {identifier}");
        }
    }

    private TValue ReadItem<TValue>(object?[]? args)
    {
        var key = (string)args![0]!;
        _store.TryGetValue(key, out var value);
        return (TValue)(object?)value!;
    }

    private void WriteItem(object?[]? args)
    {
        var key = (string)args![0]!;
        var value = (string)args[1]!;
        _store[key] = value;
    }

    private void RemoveItem(object?[]? args)
    {
        var key = (string)args![0]!;
        _store.Remove(key);
    }
}
