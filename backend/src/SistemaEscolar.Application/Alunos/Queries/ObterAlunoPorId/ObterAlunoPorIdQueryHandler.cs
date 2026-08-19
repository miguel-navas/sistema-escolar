using MediatR;
using SistemaEscolar.Application.Alunos.Commands.CriarAluno;
using SistemaEscolar.Application.Alunos.DTOs;
using SistemaEscolar.Domain.Alunos;

namespace SistemaEscolar.Application.Alunos.Queries.ObterAlunoPorId;

public sealed class ObterAlunoPorIdQueryHandler
    : IRequestHandler<ObterAlunoPorIdQuery, Result<AlunoDto>>
{
    private readonly IAlunoRepository _alunoRepository;

    public ObterAlunoPorIdQueryHandler(IAlunoRepository alunoRepository)
    {
        _alunoRepository = alunoRepository;
    }

    public async Task<Result<AlunoDto>> Handle(ObterAlunoPorIdQuery request, CancellationToken cancellationToken)
    {
        var aluno = await _alunoRepository.ObterPorIdAsync(request.AlunoId, cancellationToken);

        if (aluno is null)
            return Result<AlunoDto>.Falha("Aluno não encontrado.");

        return Result<AlunoDto>.Ok(new AlunoDto(
            aluno.Id,
            aluno.NomeCompleto,
            aluno.DataNascimento,
            aluno.Cpf?.Formatado(),
            aluno.ResponsavelId,
            aluno.Status.ToString(),
            aluno.ConsentimentoBiometricoRegistrado));
    }
}
