using MediatR;
using SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;
using SistemaEscolar.Application.Disciplinas.DTOs;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Application.Disciplinas.Queries.ObterDisciplinaPorId;

public sealed class ObterDisciplinaPorIdQueryHandler
    : IRequestHandler<ObterDisciplinaPorIdQuery, Result<DisciplinaDto>>
{
    private readonly IDisciplinaRepository _disciplinaRepository;

    public ObterDisciplinaPorIdQueryHandler(IDisciplinaRepository disciplinaRepository)
    {
        _disciplinaRepository = disciplinaRepository;
    }

    public async Task<Result<DisciplinaDto>> Handle(ObterDisciplinaPorIdQuery request, CancellationToken cancellationToken)
    {
        var disciplina = await _disciplinaRepository.ObterPorIdAsync(request.DisciplinaId, cancellationToken);

        if (disciplina is null)
            return Result<DisciplinaDto>.Falha("Disciplina não encontrada.");

        return Result<DisciplinaDto>.Ok(new DisciplinaDto(
            disciplina.Id,
            disciplina.Nome,
            disciplina.CargaHoraria,
            disciplina.AnoEscolarId));
    }
}
