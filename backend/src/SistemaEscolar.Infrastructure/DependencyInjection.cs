using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.AnosEscolares;
using SistemaEscolar.Domain.AnosLetivos;
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Matriculas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.Domain.Vinculos;
using SistemaEscolar.Infrastructure.Persistence;
using SistemaEscolar.Infrastructure.Persistence.Repositories;

namespace SistemaEscolar.Infrastructure;

/// <summary>
/// Ponto único de registro de todas as dependências de infraestrutura.
/// Api chama só isso — nunca registra DbContext/repositório diretamente
/// no Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Connection string vem de configuração/variável de ambiente.
        // Hoje aponta para Supabase; na migração para AWS RDS, só essa
        // string muda — nenhum código deste projeto muda.
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' não configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAlunoRepository, AlunoRepository>();
        services.AddScoped<ITurmaRepository, TurmaRepository>();
        services.AddScoped<IMatriculaRepository, MatriculaRepository>();
        services.AddScoped<IAnoLetivoRepository, AnoLetivoRepository>();
        services.AddScoped<IAnoEscolarRepository, AnoEscolarRepository>();
        services.AddScoped<IProfessorRepository, ProfessorRepository>();
        services.AddScoped<IDisciplinaRepository, DisciplinaRepository>();
        services.AddScoped<IProfessorDisciplinaTurmaRepository, ProfessorDisciplinaTurmaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Stripe, storage S3 e IFacialRecognitionService entram aqui conforme
        // os próximos agregados (Cobranca, PerfilBiometrico) forem implementados.

        return services;
    }
}
