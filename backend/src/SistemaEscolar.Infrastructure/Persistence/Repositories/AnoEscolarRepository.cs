using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IAnoEscolarRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class AnoEscolarRepository : IAnoEscolarRepository
{
    private readonly AppDbContext _context;

    public AnoEscolarRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AnoEscolar?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.AnosEscolares
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AdicionarAsync(AnoEscolar anoEscolar, CancellationToken cancellationToken) =>
        await _context.AnosEscolares.AddAsync(anoEscolar, cancellationToken);
}
