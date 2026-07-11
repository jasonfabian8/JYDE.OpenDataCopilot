using JYDE.OpenDataCopilot.Application.Figures;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Doble de prueba de <see cref="IDataQuery"/> que devuelve un resultado fijo o falla.</summary>
internal sealed class StubDataQuery : IDataQuery
{
    private readonly DataQueryResult? _result;
    private readonly Exception? _error;

    public StubDataQuery(DataQueryResult? result = null, Exception? error = null)
    {
        _result = result;
        _error = error;
    }

    public string? LastDatasetId { get; private set; }

    public string? LastSoql { get; private set; }

    public Task<DataQueryResult> QueryAsync(string datasetId, string soql, CancellationToken cancellationToken = default)
    {
        LastDatasetId = datasetId;
        LastSoql = soql;
        if (_error is not null)
        {
            throw _error;
        }

        return Task.FromResult(_result ?? new DataQueryResult([], []));
    }
}
