using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Matriculas;

/// <summary>
/// Agregado raiz "Matricula". Representa o vínculo de um aluno com uma
/// turma em um ano letivo. Toda regra de negócio referente ao ciclo de
/// vida da matrícula (ativação, trancamento, cancelamento, conclusão) vive
/// aqui — Application apenas orquestra chamadas a estes métodos.
/// </summary>
public sealed class Matricula : AggregateRoot
{
    public Guid AlunoId { get; private set; }
    public Guid TurmaId { get; private set; }
    public Guid AnoLetivoId { get; private set; }
    public StatusMatricula Status { get; private set; }
    public DateTime MatriculadoEm { get; private set; }

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private Matricula() { }

    private Matricula(Guid id, Guid alunoId, Guid turmaId, Guid anoLetivoId) : base(id)
    {
        AlunoId = alunoId;
        TurmaId = turmaId;
        AnoLetivoId = anoLetivoId;
        Status = StatusMatricula.Ativa;
        MatriculadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Mantém as invariantes de criação em um único
    /// lugar — nunca deixa uma Matricula inválida ser instanciada.
    /// </summary>
    public static Result<Matricula> Matricular(Guid alunoId, Guid turmaId, Guid anoLetivoId)
    {
        if (alunoId == Guid.Empty)
            return Result.Falha<Matricula>("Matrícula precisa estar vinculada a um aluno.");

        if (turmaId == Guid.Empty)
            return Result.Falha<Matricula>("Matrícula precisa estar vinculada a uma turma.");

        if (anoLetivoId == Guid.Empty)
            return Result.Falha<Matricula>("Matrícula precisa estar vinculada a um ano letivo.");

        var matricula = new Matricula(Guid.NewGuid(), alunoId, turmaId, anoLetivoId);

        matricula.RaiseDomainEvent(new AlunoMatriculadoEvent(matricula.Id, matricula.AlunoId, matricula.TurmaId, DateTime.UtcNow));

        return Result.Ok(matricula);
    }

    public Result Trancar()
    {
        if (Status != StatusMatricula.Ativa)
            return Result.Falha("Só é possível trancar uma matrícula ativa.");

        Status = StatusMatricula.Trancada;
        return Result.Ok();
    }

    public Result Cancelar()
    {
        if (Status == StatusMatricula.Concluida)
            return Result.Falha("Não é possível cancelar uma matrícula já concluída.");

        if (Status == StatusMatricula.Cancelada)
            return Result.Falha("Matrícula já está cancelada.");

        Status = StatusMatricula.Cancelada;
        return Result.Ok();
    }

    public Result Concluir()
    {
        if (Status != StatusMatricula.Ativa)
            return Result.Falha("Só é possível concluir uma matrícula ativa.");

        Status = StatusMatricula.Concluida;
        return Result.Ok();
    }
}
