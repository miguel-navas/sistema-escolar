using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de ITurmaRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class TurmaRepository : ITurmaRepository
{
    private readonly AppDbContext _context;

    public TurmaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Turma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Turmas
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<List<Turma>> ObterTurmasDoGrupoAsync(
        string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken cancellationToken) =>
        _context.Turmas
            .Where(t => t.NomeBase == nomeBase
                && t.Turno == turno
                && t.AnoLetivoId == anoLetivoId
                && t.AnoEscolarId == anoEscolarId)
            .OrderBy(t => t.Sufixo)
            .ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Turma turma, CancellationToken cancellationToken) =>
        await _context.Turmas.AddAsync(turma, cancellationToken);

    public void Atualizar(Turma turma) =>
        _context.Turmas.Update(turma);
}
