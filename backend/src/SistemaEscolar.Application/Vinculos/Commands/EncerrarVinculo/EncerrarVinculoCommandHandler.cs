using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Application.Vinculos.Commands.EncerrarVinculo;

public sealed class EncerrarVinculoCommandHandler
    : IRequestHandler<EncerrarVinculoCommand, Result<VinculoProfessorDisciplinaTurmaDto>>
{
    private readonly IProfessorDisciplinaTurmaRepository _vinculoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EncerrarVinculoCommandHandler(IProfessorDisciplinaTurmaRepository vinculoRepository, IUnitOfWork unitOfWork)
    {
        _vinculoRepository = vinculoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VinculoProfessorDisciplinaTurmaDto>> Handle(
        EncerrarVinculoCommand request, CancellationToken cancellationToken)
    {
        var vinculo = await _vinculoRepository.ObterPorIdAsync(request.VinculoId, cancellationToken);
        if (vinculo is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Vínculo não encontrado.");

        var encerrarResult = vinculo.Encerrar();
        if (!encerrarResult.Sucesso)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(encerrarResult.Erro!);

        _vinculoRepository.Atualizar(vinculo);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

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
