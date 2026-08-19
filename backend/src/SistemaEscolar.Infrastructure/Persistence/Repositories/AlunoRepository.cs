using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Alunos;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IAlunoRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class AlunoRepository : IAlunoRepository
{
    private readonly AppDbContext _context;

    public AlunoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Aluno?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Alunos
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExisteComCpfAsync(string cpfNumero, CancellationToken cancellationToken) =>
        _context.Alunos
            .AnyAsync(a => a.Cpf != null && a.Cpf.Numero == cpfNumero, cancellationToken);

    public async Task AdicionarAsync(Aluno aluno, CancellationToken cancellationToken) =>
        await _context.Alunos.AddAsync(aluno, cancellationToken);

    public void Atualizar(Aluno aluno) =>
        _context.Alunos.Update(aluno);
}
