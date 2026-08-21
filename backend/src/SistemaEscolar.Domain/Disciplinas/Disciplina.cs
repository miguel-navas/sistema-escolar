using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Disciplinas;

/// <summary>
/// Agregado raiz "Disciplina". Sem ciclo de vida além da criação nesta etapa
/// — por isso só tem a fábrica, nenhum método de transição de estado.
/// </summary>
public sealed class Disciplina : AggregateRoot
{
    public string Nome { get; private set; } = null!;
    public int CargaHoraria { get; private set; }
    public Guid AnoEscolarId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private Disciplina() { }

    private Disciplina(Guid id, string nome, int cargaHoraria, Guid anoEscolarId) : base(id)
    {
        Nome = nome;
        CargaHoraria = cargaHoraria;
        AnoEscolarId = anoEscolarId;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Mantém as invariantes de criação em um único
    /// lugar — nunca deixa uma Disciplina inválida ser instanciada.
    /// </summary>
    public static Result<Disciplina> Criar(string nome, int cargaHoraria, Guid anoEscolarId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Falha<Disciplina>("Nome da disciplina é obrigatório.");

        if (cargaHoraria <= 0)
            return Result.Falha<Disciplina>("Carga horária deve ser maior que zero.");

        if (anoEscolarId == Guid.Empty)
            return Result.Falha<Disciplina>("Disciplina precisa estar vinculada a um ano escolar.");

        var disciplina = new Disciplina(Guid.NewGuid(), nome.Trim(), cargaHoraria, anoEscolarId);

        disciplina.RaiseDomainEvent(new DisciplinaCriadaEvent(disciplina.Id, disciplina.Nome, DateTime.UtcNow));

        return Result.Ok(disciplina);
    }
}
