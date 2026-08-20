using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Queries.ObterAnoLetivoPorId;

public sealed class ObterAnoLetivoPorIdQueryHandler
    : IRequestHandler<ObterAnoLetivoPorIdQuery, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;

    public ObterAnoLetivoPorIdQueryHandler(IAnoLetivoRepository anoLetivoRepository)
    {
        _anoLetivoRepository = anoLetivoRepository;
    }

    public async Task<Result<AnoLetivoDto>> Handle(ObterAnoLetivoPorIdQuery request, CancellationToken cancellationToken)
    {
        var anoLetivo = await _anoLetivoRepository.ObterPorIdAsync(request.AnoLetivoId, cancellationToken);

        if (anoLetivo is null)
            return Result<AnoLetivoDto>.Falha("Ano letivo não encontrado.");

        return Result<AnoLetivoDto>.Ok(new AnoLetivoDto(
            anoLetivo.Id,
            anoLetivo.Ano,
            anoLetivo.DataInicio,
            anoLetivo.DataFim,
            anoLetivo.Status.ToString()));
    }
}
