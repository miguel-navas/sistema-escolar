namespace SistemaEscolar.Domain.AnosLetivos;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IAnoLetivoRepository
{
    Task<AnoLetivo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Usado pela Application para reforçar "só um ano letivo Ativo por vez"
    /// antes de ativar um novo — exclui o próprio id da checagem.
    /// </summary>
    Task<bool> ExisteOutroAnoLetivoAtivoAsync(Guid idExcluido, CancellationToken cancellationToken);

    Task AdicionarAsync(AnoLetivo anoLetivo, CancellationToken cancellationToken);
    void Atualizar(AnoLetivo anoLetivo);
}
