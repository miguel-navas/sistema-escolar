using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Turmas;

/// <summary>
/// Agregado raiz "Turma". Controla a formação de turmas de um grupo (mesmo
/// NomeBase/Turno/AnoLetivo/AnoEscolar) e o número de vagas ocupadas. A
/// orquestração entre turmas do mesmo grupo (procurar vaga, decidir quando
/// abrir uma turma-irmã) fica no caso de uso de Application
/// (MatricularAluno), nunca aqui — este agregado só sabe cuidar de si mesmo.
/// </summary>
public sealed class Turma : AggregateRoot
{
    public Guid AnoLetivoId { get; private set; }
    public Guid AnoEscolarId { get; private set; }
    public string NomeBase { get; private set; } = null!;
    public char Sufixo { get; private set; }
    public TurnoTurma Turno { get; private set; }
    public int VagasMaximas { get; private set; }
    public int VagasOcupadas { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public string Nome => $"{NomeBase} {Sufixo}";

    // Necessário para EF Core (Infrastructure) materializar a entidade.
    private Turma() { }

    private Turma(
        Guid id,
        Guid anoLetivoId,
        Guid anoEscolarId,
        string nomeBase,
        char sufixo,
        TurnoTurma turno,
        int vagasMaximas) : base(id)
    {
        AnoLetivoId = anoLetivoId;
        AnoEscolarId = anoEscolarId;
        NomeBase = nomeBase;
        Sufixo = sufixo;
        Turno = turno;
        VagasMaximas = vagasMaximas;
        VagasOcupadas = 0;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica do agregado. Sempre cria a primeira turma do grupo (Sufixo
    /// 'A') — turmas seguintes do mesmo grupo nascem via AbrirTurmaIrma.
    /// </summary>
    public static Result<Turma> Criar(
        string nomeBase,
        TurnoTurma turno,
        Guid anoLetivoId,
        Guid anoEscolarId,
        int vagasMaximas)
    {
        if (string.IsNullOrWhiteSpace(nomeBase))
            return Result.Falha<Turma>("Nome base da turma é obrigatório.");

        if (anoLetivoId == Guid.Empty)
            return Result.Falha<Turma>("Turma precisa estar vinculada a um ano letivo.");

        if (anoEscolarId == Guid.Empty)
            return Result.Falha<Turma>("Turma precisa estar vinculada a um ano escolar.");

        if (vagasMaximas <= 0)
            return Result.Falha<Turma>("Número de vagas máximas deve ser maior que zero.");

        var turma = new Turma(Guid.NewGuid(), anoLetivoId, anoEscolarId, nomeBase.Trim(), 'A', turno, vagasMaximas);

        turma.RaiseDomainEvent(new TurmaCriadaEvent(turma.Id, turma.Nome, DateTime.UtcNow));

        return Result.Ok(turma);
    }

    /// <summary>
    /// Abre a próxima turma do mesmo grupo (mesmo NomeBase/Turno/AnoLetivo/
    /// AnoEscolar), incrementando o sufixo. Chamado pela Application quando
    /// nenhuma turma do grupo tem vaga disponível para uma nova matrícula.
    /// </summary>
    public Result<Turma> AbrirTurmaIrma()
    {
        if (Sufixo == 'Z')
            return Result.Falha<Turma>("Limite de turmas irmãs atingido para este grupo.");

        var proximoSufixo = (char)(Sufixo + 1);

        var novaTurma = new Turma(
            Guid.NewGuid(),
            AnoLetivoId,
            AnoEscolarId,
            NomeBase,
            proximoSufixo,
            Turno,
            VagasMaximas);

        novaTurma.RaiseDomainEvent(new TurmaCriadaEvent(novaTurma.Id, novaTurma.Nome, DateTime.UtcNow));

        return Result.Ok(novaTurma);
    }

    /// <summary>
    /// Ocupa uma vaga da turma. Chamado pelo caso de uso de matrícula depois
    /// de escolher esta turma como alvo.
    /// </summary>
    public Result OcuparVaga()
    {
        if (VagasOcupadas >= VagasMaximas)
            return Result.Falha("Turma está lotada.");

        VagasOcupadas++;

        if (VagasOcupadas == VagasMaximas)
            RaiseDomainEvent(new TurmaLotadaEvent(Id, DateTime.UtcNow));

        return Result.Ok();
    }

    /// <summary>
    /// Libera uma vaga da turma. Chamado quando uma matrícula é cancelada ou
    /// trancada.
    /// </summary>
    public Result LiberarVaga()
    {
        if (VagasOcupadas == 0)
            return Result.Falha("Turma não possui vaga ocupada para liberar.");

        VagasOcupadas--;

        return Result.Ok();
    }

    public bool PodeReceberNovaMatricula() => VagasOcupadas < VagasMaximas;
}
