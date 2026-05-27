using Microsoft.Extensions.Configuration;

namespace SafetyScale.Api.Extensions;

internal static class CorsConfigurationHelper
{
    /// <summary>
    /// Lê Cors:Origins (array típico de appsettings) e Cors:OriginsCsv (lista separada por vírgula,
    /// útil no Docker / variáveis de ambiente Cors__OriginsCsv).
    /// </summary>
    public static string[] ResolveAllowedOrigins(IConfiguration configuration)
    {
        var fromArray =
            configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? Array.Empty<string>();
        var list = fromArray
            .Where(static o => !string.IsNullOrWhiteSpace(o))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var csv = configuration["Cors:OriginsCsv"];
        if (!string.IsNullOrWhiteSpace(csv))
        {
            foreach (var o in csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                if (!list.Contains(o, StringComparer.Ordinal))
                    list.Add(o);
        }

        return list.ToArray();
    }
}
