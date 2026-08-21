using MediatR;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Application.Vinculos.Queries.ObterVinculoPorId;

public sealed class ObterVinculoPorIdQueryHandler
    : IRequestHandler<ObterVinculoPorIdQuery, Result<VinculoProfessorDisciplinaTurmaDto>>
{
    private readonly IProfessorDisciplinaTurmaRepository _vinculoRepository;

    public ObterVinculoPorIdQueryHandler(IProfessorDisciplinaTurmaRepository vinculoRepository)
    {
        _vinculoRepository = vinculoRepository;
    }

    public async Task<Result<VinculoProfessorDisciplinaTurmaDto>> Handle(
        ObterVinculoPorIdQuery request, CancellationToken cancellationToken)
    {
        var vinculo = await _vinculoRepository.ObterPorIdAsync(request.VinculoId, cancellationToken);

        if (vinculo is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Vínculo não encontrado.");

        return Result<VinculoProfessorDisciplinaTurmaDto>.Ok(new VinculoProfessorDisciplinaTurmaDto(
            vinculo.Id,
            vinculo.ProfessorId,
            vinculo.DisciplinaId,
            vinculo.TurmaId,
            vinculo.AnoLetivoId,
            vinculo.Status.ToString(),
            vinculo.CriadoEm));
    }
}
