using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.AnosLetivos;

/// <summary>
/// Agregado raiz "AnoLetivo". Toda regra de negócio referente ao ciclo de
/// vida do ano letivo (criação, ativação, encerramento) vive aqui — nunca
/// em Application ou Api. A regra "só um ano letivo Ativo por vez" fica na
/// Application (AtivarAnoLetivoCommandHandler), pois exige consultar outros
/// agregados via repositório — este agregado só sabe cuidar de si mesmo.
/// </summary>
public sealed class AnoLetivo : AggregateRoot
{
    public int Ano { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public StatusAnoLetivo Status { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private AnoLetivo() { }

    private AnoLetivo(Guid id, int ano, DateOnly dataInicio, DateOnly dataFim) : base(id)
    {
        Ano = ano;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Status = StatusAnoLetivo.Planejado;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Mantém as invariantes de criação em um único
    /// lugar — nunca deixa um AnoLetivo inválido ser instanciado. Sempre
    /// começa como Planejado; precisa ser Ativado explicitamente.
    /// </summary>
    public static Result<AnoLetivo> Criar(int ano, DateOnly dataInicio, DateOnly dataFim)
    {
        if (ano < 2000 || ano > 2100)
            return Result.Falha<AnoLetivo>("Ano letivo deve estar entre 2000 e 2100.");

        if (dataFim <= dataInicio)
            return Result.Falha<AnoLetivo>("Data de fim deve ser posterior à data de início.");

        var anoLetivo = new AnoLetivo(Guid.NewGuid(), ano, dataInicio, dataFim);

        anoLetivo.RaiseDomainEvent(new AnoLetivoCriadoEvent(anoLetivo.Id, anoLetivo.Ano, DateTime.UtcNow));

        return Result.Ok(anoLetivo);
    }

    /// <summary>
    /// Torna este o ano letivo corrente. A regra "só um ano letivo Ativo por
    /// vez" é checada pela Application antes de chamar este método.
    /// </summary>
    public Result Ativar()
    {
        if (Status == StatusAnoLetivo.Ativo)
            return Result.Falha("Ano letivo já está ativo.");

        if (Status == StatusAnoLetivo.Encerrado)
            return Result.Falha("Não é possível ativar um ano letivo encerrado.");

        Status = StatusAnoLetivo.Ativo;
        RaiseDomainEvent(new AnoLetivoAtivadoEvent(Id, DateTime.UtcNow));

        return Result.Ok();
    }

    public Result Encerrar()
    {
        if (Status == StatusAnoLetivo.Encerrado)
            return Result.Falha("Ano letivo já está encerrado.");

        if (Status == StatusAnoLetivo.Planejado)
            return Result.Falha("Não é possível encerrar um ano letivo que ainda não foi ativado.");

        Status = StatusAnoLetivo.Encerrado;
        RaiseDomainEvent(new AnoLetivoEncerradoEvent(Id, DateTime.UtcNow));

        return Result.Ok();
    }
}
