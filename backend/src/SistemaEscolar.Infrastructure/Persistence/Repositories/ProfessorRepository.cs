using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IProfessorRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class ProfessorRepository : IProfessorRepository
{
    private readonly AppDbContext _context;

    public ProfessorRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Professor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Professores
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AdicionarAsync(Professor professor, CancellationToken cancellationToken) =>
        await _context.Professores.AddAsync(professor, cancellationToken);
}
