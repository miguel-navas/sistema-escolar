using MediatR;
using SistemaEscolar.Application.Professores.Commands.CriarProfessor;
using SistemaEscolar.Application.Professores.DTOs;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Application.Professores.Queries.ObterProfessorPorId;

public sealed class ObterProfessorPorIdQueryHandler
    : IRequestHandler<ObterProfessorPorIdQuery, Result<ProfessorDto>>
{
    private readonly IProfessorRepository _professorRepository;

    public ObterProfessorPorIdQueryHandler(IProfessorRepository professorRepository)
    {
        _professorRepository = professorRepository;
    }

    public async Task<Result<ProfessorDto>> Handle(ObterProfessorPorIdQuery request, CancellationToken cancellationToken)
    {
        var professor = await _professorRepository.ObterPorIdAsync(request.ProfessorId, cancellationToken);

        if (professor is null)
            return Result<ProfessorDto>.Falha("Professor não encontrado.");

        return Result<ProfessorDto>.Ok(new ProfessorDto(
            professor.Id,
            professor.NomeCompleto,
            professor.Email.Endereco,
            professor.Formacao));
    }
}
