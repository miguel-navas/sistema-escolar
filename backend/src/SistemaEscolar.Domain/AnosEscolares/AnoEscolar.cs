using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.AnosEscolares;

/// <summary>
/// Agregado raiz "AnoEscolar" (série). Representa um nível/série do
/// currículo (ex: "3º Ano"), usado para agrupar Turma e Disciplina. Não tem
/// ciclo de vida próprio além da criação — por isso não tem métodos de
/// transição de estado, só a fábrica.
/// </summary>
public sealed class AnoEscolar : AggregateRoot
{
    public string Nome { get; private set; } = null!;
    public NivelEnsino NivelEnsino { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private AnoEscolar() { }

    private AnoEscolar(Guid id, string nome, NivelEnsino nivelEnsino) : base(id)
    {
        Nome = nome;
        NivelEnsino = nivelEnsino;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Mantém as invariantes de criação em um único
    /// lugar — nunca deixa um AnoEscolar inválido ser instanciado.
    /// </summary>
    public static Result<AnoEscolar> Criar(string nome, NivelEnsino nivelEnsino)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Falha<AnoEscolar>("Nome do ano escolar é obrigatório.");

        var anoEscolar = new AnoEscolar(Guid.NewGuid(), nome.Trim(), nivelEnsino);

        anoEscolar.RaiseDomainEvent(new AnoEscolarCriadoEvent(anoEscolar.Id, anoEscolar.Nome, DateTime.UtcNow));

        return Result.Ok(anoEscolar);
    }
}
