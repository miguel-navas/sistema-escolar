using SistemaEscolar.Application.Common;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
