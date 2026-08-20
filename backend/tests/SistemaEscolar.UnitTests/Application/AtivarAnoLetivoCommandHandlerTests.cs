using FluentAssertions;
using SistemaEscolar.Application.AnosLetivos.Commands.AtivarAnoLetivo;
using SistemaEscolar.Domain.AnosLetivos;
using SistemaEscolar.UnitTests.Application.Fakes;
using Xunit;

namespace SistemaEscolar.UnitTests.Application;

public sealed class AtivarAnoLetivoCommandHandlerTests
{
    private readonly FakeAnoLetivoRepository _anoLetivoRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AtivarAnoLetivoCommandHandler _handler;

    public AtivarAnoLetivoCommandHandlerTests()
    {
        _handler = new AtivarAnoLetivoCommandHandler(_anoLetivoRepository, _unitOfWork);
    }

    private AnoLetivo CriarAnoLetivoSemeado(int ano = 2026, bool ativo = false)
    {
        var anoLetivo = AnoLetivo.Criar(ano, new DateOnly(ano, 2, 1), new DateOnly(ano, 12, 15)).Valor!;
        if (ativo)
            anoLetivo.Ativar();
        _anoLetivoRepository.Semear(anoLetivo);
        return anoLetivo;
    }

    /// <summary>
    /// Happy path: Ativar um AnoLetivo Planejado quando nenhum outro Ativo existe.
    /// </summary>
    [Fact]
    public async Task Handle_ComNenhumOutroAtivo_DeveAtivar()
    {
        // Arrange
        var anoLetivo = CriarAnoLetivoSemeado(ativo: false);

        // Act
        var resultado = await _handler.Handle(
            new AtivarAnoLetivoCommand(anoLetivo.Id), CancellationToken.None);

        // Assert
        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be("Ativo");
        resultado.Valor.Id.Should().Be(anoLetivo.Id);
        resultado.Valor.Ano.Should().Be(2026);
    }

    /// <summary>
    /// Error case: Não é possível ativar quando já existe outro AnoLetivo Ativo.
    /// </summary>
    [Fact]
    public async Task Handle_ComOutroAtivo_DeveFalhar()
    {
        // Arrange
        var anoLetivoAtivo = CriarAnoLetivoSemeado(ano: 2025, ativo: true);
        var anoLetivoPlanejado = CriarAnoLetivoSemeado(ano: 2026, ativo: false);

        // Act
        var resultado = await _handler.Handle(
            new AtivarAnoLetivoCommand(anoLetivoPlanejado.Id), CancellationToken.None);

        // Assert
        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Já existe um ano letivo ativo. Encerre-o antes de ativar outro.");
    }

    /// <summary>
    /// Error case: Não existe um AnoLetivo com o Id fornecido.
    /// </summary>
    [Fact]
    public async Task Handle_ComAnoLetivoInexistente_DeveFalhar()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var resultado = await _handler.Handle(
            new AtivarAnoLetivoCommand(idInexistente), CancellationToken.None);

        // Assert
        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo não encontrado.");
    }
}
