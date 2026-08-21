using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IDisciplinaRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class DisciplinaRepository : IDisciplinaRepository
{
    private readonly AppDbContext _context;

    public DisciplinaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Disciplina?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Disciplinas
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AdicionarAsync(Disciplina disciplina, CancellationToken cancellationToken) =>
        await _context.Disciplinas.AddAsync(disciplina, cancellationToken);
}
