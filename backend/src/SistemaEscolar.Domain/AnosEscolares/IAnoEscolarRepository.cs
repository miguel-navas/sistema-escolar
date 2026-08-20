namespace SistemaEscolar.Domain.AnosEscolares;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IAnoEscolarRepository
{
    Task<AnoEscolar?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task AdicionarAsync(AnoEscolar anoEscolar, CancellationToken cancellationToken);
}
