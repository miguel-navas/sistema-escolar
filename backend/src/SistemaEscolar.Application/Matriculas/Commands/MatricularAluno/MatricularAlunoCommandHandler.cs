using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Matriculas.DTOs;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.Matriculas;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;

/// <summary>
/// Handler = orquestrador entre os agregados Aluno, Turma e Matricula. Se a
/// turma informada estiver lotada, procura uma turma-irmã com vaga no mesmo
/// grupo (NomeBase/Turno/AnoLetivo/AnoEscolar) e, se nenhuma tiver vaga,
/// abre uma nova automaticamente (Turma.AbrirTurmaIrma). Toda regra de
/// negócio real está em Turma.cs e Matricula.cs (Domain).
/// </summary>
public sealed class MatricularAlunoCommandHandler
    : IRequestHandler<MatricularAlunoCommand, Result<MatriculaDto>>
{
    private readonly IAlunoRepository _alunoRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IMatriculaRepository _matriculaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MatricularAlunoCommandHandler(
        IAlunoRepository alunoRepository,
        ITurmaRepository turmaRepository,
        IMatriculaRepository matriculaRepository,
        IUnitOfWork unitOfWork)
    {
        _alunoRepository = alunoRepository;
        _turmaRepository = turmaRepository;
        _matriculaRepository = matriculaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MatriculaDto>> Handle(MatricularAlunoCommand request, CancellationToken cancellationToken)
    {
        var aluno = await _alunoRepository.ObterPorIdAsync(request.AlunoId, cancellationToken);
        if (aluno is null)
            return Result<MatriculaDto>.Falha("Aluno não encontrado.");

        var jaMatriculado = await _matriculaRepository.ExisteMatriculaAtivaAsync(
            request.AlunoId, request.AnoLetivoId, cancellationToken);
        if (jaMatriculado)
            return Result<MatriculaDto>.Falha("Aluno já possui matrícula ativa neste ano letivo.");

        var turma = await _turmaRepository.ObterPorIdAsync(request.TurmaId, cancellationToken);
        if (turma is null)
            return Result<MatriculaDto>.Falha("Turma não encontrada.");

        var (turmaAlvo, erroEscolhaTurma) = await EscolherTurmaAlvoAsync(turma, cancellationToken);
        if (turmaAlvo is null)
            return Result<MatriculaDto>.Falha(erroEscolhaTurma!);

        var ocuparVagaResult = turmaAlvo.OcuparVaga();
        if (!ocuparVagaResult.Sucesso)
            return Result<MatriculaDto>.Falha(ocuparVagaResult.Erro!);

        _turmaRepository.Atualizar(turmaAlvo);

        var matriculaResult = Matricula.Matricular(request.AlunoId, turmaAlvo.Id, request.AnoLetivoId);
        if (!matriculaResult.Sucesso)
            return Result<MatriculaDto>.Falha(matriculaResult.Erro!);

        var matricula = matriculaResult.Valor!;

        await _matriculaRepository.AdicionarAsync(matricula, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<MatriculaDto>.Ok(new MatriculaDto(
            matricula.Id,
            matricula.AlunoId,
            matricula.TurmaId,
            matricula.AnoLetivoId,
            matricula.Status.ToString(),
            matricula.MatriculadoEm));
    }

    /// <summary>
    /// Escolhe a turma que vai receber a matrícula: a turma informada, se
    /// tiver vaga; senão a primeira turma-irmã do grupo com vaga; senão uma
    /// turma-irmã nova, aberta automaticamente a partir da de maior sufixo.
    /// </summary>
    private async Task<(Turma? Turma, string? Erro)> EscolherTurmaAlvoAsync(
        Turma turmaInformada, CancellationToken cancellationToken)
    {
        if (turmaInformada.PodeReceberNovaMatricula())
            return (turmaInformada, null);

        var turmasDoGrupo = await _turmaRepository.ObterTurmasDoGrupoAsync(
            turmaInformada.NomeBase,
            turmaInformada.Turno,
            turmaInformada.AnoLetivoId,
            turmaInformada.AnoEscolarId,
            cancellationToken);

        var turmaComVaga = turmasDoGrupo.FirstOrDefault(t => t.PodeReceberNovaMatricula());
        if (turmaComVaga is not null)
            return (turmaComVaga, null);

        var ultimaTurmaDoGrupo = turmasDoGrupo.Count > 0 ? turmasDoGrupo[^1] : turmaInformada;

        var novaTurmaResult = ultimaTurmaDoGrupo.AbrirTurmaIrma();
        if (!novaTurmaResult.Sucesso)
            return (null, novaTurmaResult.Erro);

        var novaTurma = novaTurmaResult.Valor!;
        await _turmaRepository.AdicionarAsync(novaTurma, cancellationToken);

        return (novaTurma, null);
    }
}
