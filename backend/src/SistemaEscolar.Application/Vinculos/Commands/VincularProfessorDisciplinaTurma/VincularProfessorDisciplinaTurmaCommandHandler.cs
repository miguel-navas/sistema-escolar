using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Vinculos.DTOs;
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;

/// <summary>
/// Handler = orquestrador entre os agregados Professor, Disciplina, Turma e
/// ProfessorDisciplinaTurma. As duas regras da etapa exigem consultar mais
/// de um agregado, por isso vivem aqui, não em ProfessorDisciplinaTurma.cs:
/// 1) a Disciplina só pode ser vinculada a uma Turma do mesmo AnoEscolar;
/// 2) o Professor não pode ter dois vínculos idênticos (mesma disciplina,
///    turma e ano letivo) ativos ao mesmo tempo.
/// </summary>
public sealed class VincularProfessorDisciplinaTurmaCommandHandler
    : IRequestHandler<VincularProfessorDisciplinaTurmaCommand, Result<VinculoProfessorDisciplinaTurmaDto>>
{
    private readonly IProfessorRepository _professorRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IProfessorDisciplinaTurmaRepository _vinculoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VincularProfessorDisciplinaTurmaCommandHandler(
        IProfessorRepository professorRepository,
        IDisciplinaRepository disciplinaRepository,
        ITurmaRepository turmaRepository,
        IProfessorDisciplinaTurmaRepository vinculoRepository,
        IUnitOfWork unitOfWork)
    {
        _professorRepository = professorRepository;
        _disciplinaRepository = disciplinaRepository;
        _turmaRepository = turmaRepository;
        _vinculoRepository = vinculoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VinculoProfessorDisciplinaTurmaDto>> Handle(
        VincularProfessorDisciplinaTurmaCommand request, CancellationToken cancellationToken)
    {
        var professor = await _professorRepository.ObterPorIdAsync(request.ProfessorId, cancellationToken);
        if (professor is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Professor não encontrado.");

        var disciplina = await _disciplinaRepository.ObterPorIdAsync(request.DisciplinaId, cancellationToken);
        if (disciplina is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Disciplina não encontrada.");

        var turma = await _turmaRepository.ObterPorIdAsync(request.TurmaId, cancellationToken);
        if (turma is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Turma não encontrada.");

        if (disciplina.AnoEscolarId != turma.AnoEscolarId)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(
                "Disciplina só pode ser vinculada a uma turma do mesmo ano escolar.");

        var existeVinculo = await _vinculoRepository.ExisteVinculoAtivoAsync(
            request.ProfessorId, request.DisciplinaId, request.TurmaId, request.AnoLetivoId, cancellationToken);
        if (existeVinculo)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(
                "Professor já possui um vínculo ativo idêntico (mesma disciplina, turma e ano letivo).");

        var vinculoResult = ProfessorDisciplinaTurma.Vincular(
            request.ProfessorId, request.DisciplinaId, request.TurmaId, request.AnoLetivoId);
        if (!vinculoResult.Sucesso)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(vinculoResult.Erro!);

        var vinculo = vinculoResult.Valor!;

        await _vinculoRepository.AdicionarAsync(vinculo, cancellationToken);
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
