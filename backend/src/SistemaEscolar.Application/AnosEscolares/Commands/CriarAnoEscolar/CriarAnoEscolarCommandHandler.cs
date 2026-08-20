using MediatR;
using SistemaEscolar.Application.AnosEscolares.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de AnoEscolar.cs (Domain).
/// </summary>
public sealed class CriarAnoEscolarCommandHandler
    : IRequestHandler<CriarAnoEscolarCommand, Result<AnoEscolarDto>>
{
    private readonly IAnoEscolarRepository _anoEscolarRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarAnoEscolarCommandHandler(IAnoEscolarRepository anoEscolarRepository, IUnitOfWork unitOfWork)
    {
        _anoEscolarRepository = anoEscolarRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoEscolarDto>> Handle(CriarAnoEscolarCommand request, CancellationToken cancellationToken)
    {
        var anoEscolarResult = AnoEscolar.Criar(request.Nome, request.NivelEnsino);

        if (!anoEscolarResult.Sucesso)
            return Result<AnoEscolarDto>.Falha(anoEscolarResult.Erro!);

        var anoEscolar = anoEscolarResult.Valor!;

        await _anoEscolarRepository.AdicionarAsync(anoEscolar, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<AnoEscolarDto>.Ok(new AnoEscolarDto(
            anoEscolar.Id,
            anoEscolar.Nome,
            anoEscolar.NivelEnsino.ToString()));
    }
}
