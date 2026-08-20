using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Commands.EncerrarAnoLetivo;

public sealed class EncerrarAnoLetivoCommandHandler
    : IRequestHandler<EncerrarAnoLetivoCommand, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EncerrarAnoLetivoCommandHandler(IAnoLetivoRepository anoLetivoRepository, IUnitOfWork unitOfWork)
    {
        _anoLetivoRepository = anoLetivoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoLetivoDto>> Handle(EncerrarAnoLetivoCommand request, CancellationToken cancellationToken)
    {
        var anoLetivo = await _anoLetivoRepository.ObterPorIdAsync(request.AnoLetivoId, cancellationToken);
        if (anoLetivo is null)
            return Result<AnoLetivoDto>.Falha("Ano letivo não encontrado.");

        var encerrarResult = anoLetivo.Encerrar();
        if (!encerrarResult.Sucesso)
            return Result<AnoLetivoDto>.Falha(encerrarResult.Erro!);

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
