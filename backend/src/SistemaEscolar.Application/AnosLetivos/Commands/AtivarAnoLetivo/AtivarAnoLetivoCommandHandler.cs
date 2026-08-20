using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Commands.AtivarAnoLetivo;

/// <summary>
/// Handler = orquestrador. A regra "só um ano letivo Ativo por vez" exige
/// consultar outros agregados AnoLetivo via repositório — por isso vive
/// aqui, não em AnoLetivo.cs (que só sabe cuidar de si mesmo).
/// </summary>
public sealed class AtivarAnoLetivoCommandHandler
    : IRequestHandler<AtivarAnoLetivoCommand, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AtivarAnoLetivoCommandHandler(IAnoLetivoRepository anoLetivoRepository, IUnitOfWork unitOfWork)
    {
        _anoLetivoRepository = anoLetivoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoLetivoDto>> Handle(AtivarAnoLetivoCommand request, CancellationToken cancellationToken)
    {
        var anoLetivo = await _anoLetivoRepository.ObterPorIdAsync(request.AnoLetivoId, cancellationToken);
        if (anoLetivo is null)
            return Result<AnoLetivoDto>.Falha("Ano letivo não encontrado.");

        var existeOutroAtivo = await _anoLetivoRepository.ExisteOutroAnoLetivoAtivoAsync(
            request.AnoLetivoId, cancellationToken);
        if (existeOutroAtivo)
            return Result<AnoLetivoDto>.Falha("Já existe um ano letivo ativo. Encerre-o antes de ativar outro.");

        var ativarResult = anoLetivo.Ativar();
        if (!ativarResult.Sucesso)
            return Result<AnoLetivoDto>.Falha(ativarResult.Erro!);

        _anoLetivoRepository.Atualizar(anoLetivo);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<AnoLetivoDto>.Ok(new AnoLetivoDto(
            anoLetivo.Id,
            anoLetivo.Ano,
            anoLetivo.DataInicio,
            anoLetivo.DataFim,
            anoLetivo.Status.ToString()));
    }
}
