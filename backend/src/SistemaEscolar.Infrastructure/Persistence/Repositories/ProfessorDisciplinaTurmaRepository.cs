using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IProfessorDisciplinaTurmaRepository (definida
/// no Domain). Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class ProfessorDisciplinaTurmaRepository : IProfessorDisciplinaTurmaRepository
{
    private readonly AppDbContext _context;

    public ProfessorDisciplinaTurmaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ProfessorDisciplinaTurma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.VinculosProfessorDisciplinaTurma
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<bool> ExisteVinculoAtivoAsync(
        Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        _context.VinculosProfessorDisciplinaTurma
            .AnyAsync(v =>
                v.ProfessorId == professorId
                && v.DisciplinaId == disciplinaId
                && v.TurmaId == turmaId
                && v.AnoLetivoId == anoLetivoId
                && v.Status == StatusVinculo.Ativo,
                cancellationToken);

    public async Task AdicionarAsync(ProfessorDisciplinaTurma vinculo, CancellationToken cancellationToken) =>
        await _context.VinculosProfessorDisciplinaTurma.AddAsync(vinculo, cancellationToken);

    public void Atualizar(ProfessorDisciplinaTurma vinculo) =>
        _context.VinculosProfessorDisciplinaTurma.Update(vinculo);
}
