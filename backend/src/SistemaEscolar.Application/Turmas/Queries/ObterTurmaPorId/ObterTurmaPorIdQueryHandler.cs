using MediatR;
using SistemaEscolar.Application.Turmas.Commands.CriarTurma;
using SistemaEscolar.Application.Turmas.DTOs;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Application.Turmas.Queries.ObterTurmaPorId;

public sealed class ObterTurmaPorIdQueryHandler
    : IRequestHandler<ObterTurmaPorIdQuery, Result<TurmaDto>>
{
    private readonly ITurmaRepository _turmaRepository;

    public ObterTurmaPorIdQueryHandler(ITurmaRepository turmaRepository)
    {
        _turmaRepository = turmaRepository;
    }

    public async Task<Result<TurmaDto>> Handle(ObterTurmaPorIdQuery request, CancellationToken cancellationToken)
    {
        var turma = await _turmaRepository.ObterPorIdAsync(request.TurmaId, cancellationToken);

        if (turma is null)
            return Result<TurmaDto>.Falha("Turma não encontrada.");

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
