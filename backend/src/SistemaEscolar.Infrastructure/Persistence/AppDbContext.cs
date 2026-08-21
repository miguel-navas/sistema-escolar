using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.AnosEscolares;
using SistemaEscolar.Domain.AnosLetivos;
using SistemaEscolar.Domain.Common;
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Matriculas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Infrastructure.Persistence;

/// <summary>
/// DbContext único, apontando para Postgres (hoje via Supabase, amanhã via
/// AWS RDS — a connection string é o único ponto que muda, vinda de
/// configuração/variável de ambiente, nunca hardcoded).
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly IPublisher? _publisher;

    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<AnoLetivo> AnosLetivos => Set<AnoLetivo>();
    public DbSet<AnoEscolar> AnosEscolares => Set<AnoEscolar>();
    public DbSet<Professor> Professores => Set<Professor>();
    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<ProfessorDisciplinaTurma> VinculosProfessorDisciplinaTurma => Set<ProfessorDisciplinaTurma>();

    public AppDbContext(DbContextOptions<AppDbContext> options, IPublisher? publisher = null)
        : base(options)
    {
        _publisher = publisher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Publica os eventos de domínio acumulados nos agregados SOMENTE depois
    /// que a transação foi persistida com sucesso — evita publicar evento
    /// de algo que acabou não sendo salvo.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var agregadosComEventos = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Any())
            .ToList();

        var resultado = await base.SaveChangesAsync(cancellationToken);

        if (_publisher is not null)
        {
            foreach (var agregado in agregadosComEventos)
            {
                var eventos = agregado.DomainEvents.ToList();
                agregado.ClearDomainEvents();

                foreach (var evento in eventos)
                    await _publisher.Publish(evento, cancellationToken);
            }
        }

        return resultado;
    }
}
