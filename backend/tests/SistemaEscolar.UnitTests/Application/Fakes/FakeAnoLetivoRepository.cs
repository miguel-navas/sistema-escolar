using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeAnoLetivoRepository : IAnoLetivoRepository
{
    private readonly Dictionary<Guid, AnoLetivo> _anosLetivos = new();

    public void Semear(AnoLetivo anoLetivo) => _anosLetivos[anoLetivo.Id] = anoLetivo;

    public Task<AnoLetivo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_anosLetivos.GetValueOrDefault(id));

    public Task<bool> ExisteOutroAnoLetivoAtivoAsync(Guid idExcluido, CancellationToken cancellationToken) =>
        Task.FromResult(_anosLetivos.Values.Any(a => a.Id != idExcluido && a.Status == StatusAnoLetivo.Ativo));

    public Task AdicionarAsync(AnoLetivo anoLetivo, CancellationToken cancellationToken)
    {
        _anosLetivos[anoLetivo.Id] = anoLetivo;
        return Task.CompletedTask;
    }

    public void Atualizar(AnoLetivo anoLetivo) => _anosLetivos[anoLetivo.Id] = anoLetivo;
}
