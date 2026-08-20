using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IAnoLetivoRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class AnoLetivoRepository : IAnoLetivoRepository
{
    private readonly AppDbContext _context;

    public AnoLetivoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AnoLetivo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.AnosLetivos
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExisteOutroAnoLetivoAtivoAsync(Guid idExcluido, CancellationToken cancellationToken) =>
        _context.AnosLetivos
            .AnyAsync(a => a.Id != idExcluido && a.Status == StatusAnoLetivo.Ativo, cancellationToken);

    public async Task AdicionarAsync(AnoLetivo anoLetivo, CancellationToken cancellationToken) =>
        await _context.AnosLetivos.AddAsync(anoLetivo, cancellationToken);

    public void Atualizar(AnoLetivo anoLetivo) =>
        _context.AnosLetivos.Update(anoLetivo);
}
