using MediatR;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de AnoLetivo.cs (Domain).
/// </summary>
public sealed class CriarAnoLetivoCommandHandler
    : IRequestHandler<CriarAnoLetivoCommand, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarAnoLetivoCommandHandler(IAnoLetivoRepository anoLetivoRepository, IUnitOfWork unitOfWork)
    {
        _anoLetivoRepository = anoLetivoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoLetivoDto>> Handle(CriarAnoLetivoCommand request, CancellationToken cancellationToken)
    {
        var anoLetivoResult = AnoLetivo.Criar(request.Ano, request.DataInicio, request.DataFim);

        if (!anoLetivoResult.Sucesso)
            return Result<AnoLetivoDto>.Falha(anoLetivoResult.Erro!);

        var anoLetivo = anoLetivoResult.Valor!;

        await _anoLetivoRepository.AdicionarAsync(anoLetivo, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<AnoLetivoDto>.Ok(new AnoLetivoDto(
            anoLetivo.Id,
            anoLetivo.Ano,
            anoLetivo.DataInicio,
            anoLetivo.DataFim,
            anoLetivo.Status.ToString()));
    }
}
