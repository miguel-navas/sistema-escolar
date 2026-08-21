using SistemaEscolar.Domain.Common;
using SistemaEscolar.Domain.SharedKernel.ValueObjects;

namespace SistemaEscolar.Domain.Professores;

/// <summary>
/// Agregado raiz "Professor". Sem ciclo de vida além do cadastro nesta etapa
/// — por isso só tem a fábrica, nenhum método de transição de estado.
/// </summary>
public sealed class Professor : AggregateRoot
{
    public string NomeCompleto { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string Formacao { get; private set; } = null!;
    public DateTime CriadoEm { get; private set; }

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private Professor() { }

    private Professor(Guid id, string nomeCompleto, Email email, string formacao) : base(id)
    {
        NomeCompleto = nomeCompleto;
        Email = email;
        Formacao = formacao;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Mantém as invariantes de criação em um único
    /// lugar — nunca deixa um Professor inválido ser instanciado.
    /// </summary>
    public static Result<Professor> Cadastrar(string nomeCompleto, string emailInformado, string formacao)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            return Result.Falha<Professor>("Nome completo é obrigatório.");

        var emailResult = Email.Criar(emailInformado);
        if (!emailResult.Sucesso)
            return Result.Falha<Professor>(emailResult.Erro!);

        if (string.IsNullOrWhiteSpace(formacao))
            return Result.Falha<Professor>("Formação é obrigatória.");

        var professor = new Professor(Guid.NewGuid(), nomeCompleto.Trim(), emailResult.Valor!, formacao.Trim());

        professor.RaiseDomainEvent(new ProfessorCadastradoEvent(professor.Id, professor.NomeCompleto, DateTime.UtcNow));

        return Result.Ok(professor);
    }
}
