using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Vinculos;

/// <summary>
/// Agregado raiz "ProfessorDisciplinaTurma" (vínculo). Representa que um
/// professor leciona uma disciplina em uma turma, em um ano letivo — sempre
/// referenciando os outros agregados só por Guid (DDD). A regra "disciplina
/// só pode ser vinculada a turma do mesmo ano escolar" e a regra "sem
/// vínculo duplicado" exigem consultar outros agregados via repositório —
/// por isso vivem na Application (VincularProfessorDisciplinaTurmaCommandHandler),
/// nunca aqui, que só sabe cuidar do próprio estado.
/// </summary>
public sealed class ProfessorDisciplinaTurma : AggregateRoot
{
    public Guid ProfessorId { get; private set; }
    public Guid DisciplinaId { get; private set; }
    public Guid TurmaId { get; private set; }
    public Guid AnoLetivoId { get; private set; }
    public StatusVinculo Status { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private ProfessorDisciplinaTurma() { }

    private ProfessorDisciplinaTurma(Guid id, Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId) : base(id)
    {
        ProfessorId = professorId;
        DisciplinaId = disciplinaId;
        TurmaId = turmaId;
        AnoLetivoId = anoLetivoId;
        Status = StatusVinculo.Ativo;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Mantém as invariantes de criação em um único
    /// lugar — nunca deixa um vínculo inválido ser instanciado. Sempre
    /// começa Ativo.
    /// </summary>
    public static Result<ProfessorDisciplinaTurma> Vincular(Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId)
    {
        if (professorId == Guid.Empty)
            return Result.Falha<ProfessorDisciplinaTurma>("Vínculo precisa estar associado a um professor.");

        if (disciplinaId == Guid.Empty)
            return Result.Falha<ProfessorDisciplinaTurma>("Vínculo precisa estar associado a uma disciplina.");

        if (turmaId == Guid.Empty)
            return Result.Falha<ProfessorDisciplinaTurma>("Vínculo precisa estar associado a uma turma.");

        if (anoLetivoId == Guid.Empty)
            return Result.Falha<ProfessorDisciplinaTurma>("Vínculo precisa estar associado a um ano letivo.");

        var vinculo = new ProfessorDisciplinaTurma(Guid.NewGuid(), professorId, disciplinaId, turmaId, anoLetivoId);

        vinculo.RaiseDomainEvent(new VinculoProfessorDisciplinaTurmaCriadoEvent(
            vinculo.Id, vinculo.ProfessorId, vinculo.DisciplinaId, vinculo.TurmaId, DateTime.UtcNow));

        return Result.Ok(vinculo);
    }

    /// <summary>
    /// Encerra o vínculo. A Etapa 3 (Aula) só permite registrar aula quando
    /// existe um vínculo Ativo para a combinação professor/disciplina/turma.
    /// </summary>
    public Result Encerrar()
    {
        if (Status == StatusVinculo.Encerrado)
            return Result.Falha("Vínculo já está encerrado.");

        Status = StatusVinculo.Encerrado;
        RaiseDomainEvent(new VinculoProfessorDisciplinaTurmaEncerradoEvent(Id, DateTime.UtcNow));

        return Result.Ok();
    }
}
