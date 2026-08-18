namespace SistemaEscolar.Application.Common;

/// <summary>
/// Abstrai o "salvar tudo em uma transação" — implementado pela Infrastructure
/// (ex: chamando SaveChangesAsync do EF Core / DbContext do Supabase-Postgres).
/// Casos de uso dependem só desta interface, nunca de DbContext diretamente.
/// </summary>
public interface IUnitOfWork
{
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
