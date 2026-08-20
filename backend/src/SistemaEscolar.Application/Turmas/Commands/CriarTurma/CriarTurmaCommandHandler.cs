using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Turmas.DTOs;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Application.Turmas.Commands.CriarTurma;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de Turma.cs (Domain).
/// </summary>
public sealed class CriarTurmaCommandHandler
    : IRequestHandler<CriarTurmaCommand, Result<TurmaDto>>
{
    private readonly ITurmaRepository _turmaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarTurmaCommandHandler(ITurmaRepository turmaRepository, IUnitOfWork unitOfWork)
    {
        _turmaRepository = turmaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TurmaDto>> Handle(CriarTurmaCommand request, CancellationToken cancellationToken)
    {
        var turmaResult = Turma.Criar(
            request.NomeBase,
            request.Turno,
            request.AnoLetivoId,
            request.AnoEscolarId,
            request.VagasMaximas);

        if (!turmaResult.Sucesso)
            return Result<TurmaDto>.Falha(turmaResult.Erro!);

        var turma = turmaResult.Valor!;

        await _turmaRepository.AdicionarAsync(turma, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<TurmaDto>.Ok(new TurmaDto(
            turma.Id,
            turma.NomeBase,
            turma.Sufixo,
            turma.Nome,
            turma.Turno.ToString(),
            turma.AnoLetivoId,
            turma.AnoEscolarId,
            turma.VagasMaximas,
            turma.VagasOcupadas));
    }
}
