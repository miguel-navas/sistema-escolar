using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Disciplinas.DTOs;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de Disciplina.cs (Domain).
/// </summary>
public sealed class CriarDisciplinaCommandHandler
    : IRequestHandler<CriarDisciplinaCommand, Result<DisciplinaDto>>
{
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarDisciplinaCommandHandler(IDisciplinaRepository disciplinaRepository, IUnitOfWork unitOfWork)
    {
        _disciplinaRepository = disciplinaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DisciplinaDto>> Handle(CriarDisciplinaCommand request, CancellationToken cancellationToken)
    {
        var disciplinaResult = Disciplina.Criar(request.Nome, request.CargaHoraria, request.AnoEscolarId);

        if (!disciplinaResult.Sucesso)
            return Result<DisciplinaDto>.Falha(disciplinaResult.Erro!);

        var disciplina = disciplinaResult.Valor!;

        await _disciplinaRepository.AdicionarAsync(disciplina, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<DisciplinaDto>.Ok(new DisciplinaDto(
            disciplina.Id,
            disciplina.Nome,
            disciplina.CargaHoraria,
            disciplina.AnoEscolarId));
    }
}
