using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using SafetyScale.Api;
using Testcontainers.MsSql;

namespace SafetyScale.Tests.Api.Integration;

public sealed class TestWebApplicationFactory : WebApplicationFactory<ApiApplicationEntryPoint>, IDisposable
{
    private static readonly Lock ContainerLock = new();
    private static MsSqlContainer? _container;

    private readonly string _databaseName = $"SafetyScaleTest_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private bool _disposed;

    private static MsSqlContainer GetOrCreateContainer()
    {
        if (_container != null)
        {
            return _container;
        }

        lock (ContainerLock)
        {
            _container ??= StartContainer();
            return _container;
        }
    }

    private static MsSqlContainer StartContainer()
    {
        try
        {
            var container = new MsSqlBuilder().Build();
            container.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return container;
        }
        catch (DockerUnavailableException ex)
        {
            throw new InvalidOperationException(
                "Testes de integracao da API exigem Docker em execucao (engine acessivel em DOCKER_HOST / socket padrao). " +
                "Consulte README.md na secao `Testes`.",
                ex);
        }
    }

    public TestWebApplicationFactory()
    {
        var container = GetOrCreateContainer();

        EnsureDatabase(container, _databaseName);

        var builder = new SqlConnectionStringBuilder(container.GetConnectionString())
        {
            InitialCatalog = _databaseName
        };

        _connectionString = builder.ConnectionString;
    }

    private static void EnsureDatabase(MsSqlContainer container, string databaseName)
    {
        ValidateDatabaseIdentifier(databaseName);

        var builder = new SqlConnectionStringBuilder(container.GetConnectionString())
        {
            InitialCatalog = "master"
        };

        using var conn = new SqlConnection(builder.ConnectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE [{BracketEscape(databaseName)}]";
        cmd.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && !_disposed)
        {
            _disposed = true;
            DropDatabaseBestEffort();
        }
    }

    private static void ValidateDatabaseIdentifier(string name)
    {
        if (name.Length > 124 || string.IsNullOrWhiteSpace(name) || name.Trim() != name)
        {
            throw new ArgumentException($"Invalid database name '{name}'.", nameof(name));
        }

        foreach (var c in name)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
            {
                throw new ArgumentException($"Invalid character in database name: '{c}'.", nameof(name));
            }
        }
    }

    private static string BracketEscape(string identifier) =>
        identifier.Replace("]", "]]", StringComparison.Ordinal);

    private void DropDatabaseBestEffort()
    {
        try
        {
            var container = _container ?? GetOrCreateContainer();
            var builder = new SqlConnectionStringBuilder(container.GetConnectionString())
            {
                InitialCatalog = "master"
            };

            SqlConnection.ClearAllPools();

            using var conn = new SqlConnection(builder.ConnectionString);
            conn.Open();

            using var singleUserCmd = conn.CreateCommand();
            singleUserCmd.CommandText =
                $"""
                ALTER DATABASE [{BracketEscape(_databaseName)}]
                SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                """;
            singleUserCmd.ExecuteNonQuery();

            using var dropCmd = conn.CreateCommand();
            dropCmd.CommandText = $"DROP DATABASE [{BracketEscape(_databaseName)}]";
            dropCmd.ExecuteNonQuery();
        }
        catch
        {
            // best-effort cleanup of ephemeral integration databases
        }
    }
}
