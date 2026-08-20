using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Professores.DTOs;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Application.Professores.Commands.CriarProfessor;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de Professor.cs (Domain).
/// </summary>
public sealed class CriarProfessorCommandHandler
    : IRequestHandler<CriarProfessorCommand, Result<ProfessorDto>>
{
    private readonly IProfessorRepository _professorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarProfessorCommandHandler(IProfessorRepository professorRepository, IUnitOfWork unitOfWork)
    {
        _professorRepository = professorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfessorDto>> Handle(CriarProfessorCommand request, CancellationToken cancellationToken)
    {
        var professorResult = Professor.Cadastrar(request.NomeCompleto, request.Email, request.Formacao);

        if (!professorResult.Sucesso)
            return Result<ProfessorDto>.Falha(professorResult.Erro!);

        var professor = professorResult.Valor!;

        await _professorRepository.AdicionarAsync(professor, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<ProfessorDto>.Ok(new ProfessorDto(
            professor.Id,
            professor.NomeCompleto,
            professor.Email.Endereco,
            professor.Formacao));
    }
}
