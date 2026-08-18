using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IMatriculaRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class MatriculaRepository : IMatriculaRepository
{
    private readonly AppDbContext _context;

    public MatriculaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Matricula?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Matriculas
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        _context.Matriculas
            .AnyAsync(m => m.AlunoId == alunoId
                && m.AnoLetivoId == anoLetivoId
                && m.Status == StatusMatricula.Ativa, cancellationToken);

    public async Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken) =>
        await _context.Matriculas.AddAsync(matricula, cancellationToken);

    public void Atualizar(Matricula matricula) =>
        _context.Matriculas.Update(matricula);
}
