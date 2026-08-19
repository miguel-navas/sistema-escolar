using SistemaEscolar.Domain.Common;
using SistemaEscolar.Domain.SharedKernel.ValueObjects;

namespace SistemaEscolar.Domain.Alunos;

/// <summary>
/// Agregado raiz "Aluno". Toda regra de negócio referente ao ciclo de vida
/// do aluno (cadastro, consentimento biométrico, ativação/inativação) vive
/// aqui — nunca em Application ou Api. Application apenas orquestra chamadas
/// a estes métodos.
/// </summary>
public sealed class Aluno : AggregateRoot
{
    public string NomeCompleto { get; private set; } = null!;
    public DateOnly DataNascimento { get; private set; }
    public Cpf? Cpf { get; private set; }
    public Guid ResponsavelId { get; private set; }
    public StatusAluno Status { get; private set; }
    public bool ConsentimentoBiometricoRegistrado { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private Aluno() { }

    private Aluno(
        Guid id,
        string nomeCompleto,
        DateOnly dataNascimento,
        Cpf? cpf,
        Guid responsavelId) : base(id)
    {
        NomeCompleto = nomeCompleto;
        DataNascimento = dataNascimento;
        Cpf = cpf;
        ResponsavelId = responsavelId;
        Status = StatusAluno.PreCadastrado;
        ConsentimentoBiometricoRegistrado = false;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Mantém as invariantes de criação em um único
    /// lugar e nunca deixa um Aluno inválido ser instanciado.
    /// </summary>
    public static Result<Aluno> Cadastrar(
        string nomeCompleto,
        DateOnly dataNascimento,
        Cpf? cpf,
        Guid responsavelId)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            return Result.Falha<Aluno>("Nome completo é obrigatório.");

        if (responsavelId == Guid.Empty)
            return Result.Falha<Aluno>("Aluno precisa estar vinculado a um responsável.");

        var idade = CalcularIdade(dataNascimento);
        if (idade < 0 || idade > 100)
            return Result.Falha<Aluno>("Data de nascimento inválida.");

        var aluno = new Aluno(Guid.NewGuid(), nomeCompleto.Trim(), dataNascimento, cpf, responsavelId);

        aluno.RaiseDomainEvent(new AlunoCadastradoEvent(aluno.Id, aluno.NomeCompleto, DateTime.UtcNow));

        return Result.Ok(aluno);
    }

    /// <summary>
    /// Registra o consentimento formal do responsável para uso de
    /// reconhecimento facial. É pré-requisito obrigatório para que o
    /// contexto de Frequência/Biometria crie o PerfilBiometrico do aluno —
    /// nunca capturar/enrolar biometria sem este passo (LGPD).
    /// </summary>
    public Result RegistrarConsentimentoBiometrico()
    {
        if (Status == StatusAluno.Inativo)
            return Result.Falha("Não é possível registrar consentimento para aluno inativo.");

        if (ConsentimentoBiometricoRegistrado)
            return Result.Falha("Consentimento biométrico já foi registrado para este aluno.");

        ConsentimentoBiometricoRegistrado = true;
        RaiseDomainEvent(new ConsentimentoBiometricoRegistradoEvent(Id, DateTime.UtcNow));

        return Result.Ok();
    }

    public Result Ativar()
    {
        if (Status == StatusAluno.Ativo)
            return Result.Falha("Aluno já está ativo.");

        Status = StatusAluno.Ativo;
        return Result.Ok();
    }

    public Result Inativar()
    {
        if (Status == StatusAluno.Inativo)
            return Result.Falha("Aluno já está inativo.");

        Status = StatusAluno.Inativo;
        return Result.Ok();
    }

    /// <summary>
    /// Regra de negócio central do módulo de frequência: só é permitido
    /// registrar presença por reconhecimento facial se o aluno está ativo
    /// E já tem consentimento biométrico registrado. Este método é chamado
    /// pelo bounded context de Frequência antes de criar um RegistroReconhecimento.
    /// </summary>
    public bool PodeTerPresencaRegistradaPorReconhecimentoFacial() =>
        Status == StatusAluno.Ativo && ConsentimentoBiometricoRegistrado;

    private static int CalcularIdade(DateOnly nascimento)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var idade = hoje.Year - nascimento.Year;
        if (nascimento > hoje.AddYears(-idade)) idade--;
        return idade;
    }
}
