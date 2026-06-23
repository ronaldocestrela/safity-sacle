using SafetyScale.Web.Blazor.Models.UnavailableDays;

namespace SafetyScale.Web.Blazor.Services.UnavailableDays;

/// <summary>Draft pending state for unavailable days. Parity with React toggle helpers.</summary>
public static class UnavailableDayPendingState
{
    public enum PendingAction
    {
        Add,
        Remove,
    }

    public static Dictionary<string, Guid> BaselineMap(IEnumerable<UnavailableDayDto> items)
    {
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            map[item.Date.ToString("yyyy-MM-dd")] = item.Id;
        }

        return map;
    }

    public static bool EffectiveUnavailable(
        string key,
        IReadOnlyDictionary<string, Guid> baseline,
        IReadOnlyDictionary<string, PendingAction> pending)
    {
        if (pending.TryGetValue(key, out var action))
        {
            return action switch
            {
                PendingAction.Add => true,
                PendingAction.Remove => false,
                _ => baseline.ContainsKey(key),
            };
        }

        return baseline.ContainsKey(key);
    }

    public static Dictionary<string, PendingAction> TogglePending(
        string key,
        IReadOnlyDictionary<string, Guid> baseline,
        IReadOnlyDictionary<string, PendingAction> pending)
    {
        var effective = EffectiveUnavailable(key, baseline, pending);
        var next = pending.ToDictionary(static pair => pair.Key, static pair => pair.Value);

        if (effective)
        {
            if (baseline.ContainsKey(key))
            {
                next[key] = PendingAction.Remove;
            }
            else
            {
                next.Remove(key);
            }
        }
        else if (baseline.ContainsKey(key) && next.TryGetValue(key, out var existing) && existing == PendingAction.Remove)
        {
            next.Remove(key);
        }
        else
        {
            next[key] = PendingAction.Add;
        }

        return next;
    }
}
