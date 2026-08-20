using MediatR;
using SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;
using SistemaEscolar.Application.AnosEscolares.DTOs;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Application.AnosEscolares.Queries.ObterAnoEscolarPorId;

public sealed class ObterAnoEscolarPorIdQueryHandler
    : IRequestHandler<ObterAnoEscolarPorIdQuery, Result<AnoEscolarDto>>
{
    private readonly IAnoEscolarRepository _anoEscolarRepository;

    public ObterAnoEscolarPorIdQueryHandler(IAnoEscolarRepository anoEscolarRepository)
    {
        _anoEscolarRepository = anoEscolarRepository;
    }

    public async Task<Result<AnoEscolarDto>> Handle(ObterAnoEscolarPorIdQuery request, CancellationToken cancellationToken)
    {
        var anoEscolar = await _anoEscolarRepository.ObterPorIdAsync(request.AnoEscolarId, cancellationToken);

        if (anoEscolar is null)
            return Result<AnoEscolarDto>.Falha("Ano escolar não encontrado.");

        return Result<AnoEscolarDto>.Ok(new AnoEscolarDto(
            anoEscolar.Id,
            anoEscolar.Nome,
            anoEscolar.NivelEnsino.ToString()));
    }
}
