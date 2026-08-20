using MediatR;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Application.Matriculas.DTOs;
using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.Application.Matriculas.Queries.ObterMatriculaPorId;

public sealed class ObterMatriculaPorIdQueryHandler
    : IRequestHandler<ObterMatriculaPorIdQuery, Result<MatriculaDto>>
{
    private readonly IMatriculaRepository _matriculaRepository;

    public ObterMatriculaPorIdQueryHandler(IMatriculaRepository matriculaRepository)
    {
        _matriculaRepository = matriculaRepository;
    }

    public async Task<Result<MatriculaDto>> Handle(ObterMatriculaPorIdQuery request, CancellationToken cancellationToken)
    {
        var matricula = await _matriculaRepository.ObterPorIdAsync(request.MatriculaId, cancellationToken);

        if (matricula is null)
            return Result<MatriculaDto>.Falha("Matrícula não encontrada.");

        return Result<MatriculaDto>.Ok(new MatriculaDto(
            matricula.Id,
            matricula.AlunoId,
            matricula.TurmaId,
            matricula.AnoLetivoId,
            matricula.Status.ToString(),
            matricula.MatriculadoEm));
    }
}
