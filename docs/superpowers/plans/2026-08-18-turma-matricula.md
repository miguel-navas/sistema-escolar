# Turma & Matricula Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `Turma` and `Matricula` aggregates (Domain, Application, Infrastructure, Api, unit tests), mirroring the existing `Aluno` aggregate's layering exactly, including automatic sibling-turma creation when a turma reaches capacity.

**Architecture:** Clean Architecture / DDD, same 4 projects as `Aluno` (`SistemaEscolar.Domain`, `.Application`, `.Infrastructure`, `.Api`). `Turma` and `Matricula` are separate aggregate roots in the Academico bounded context; they reference each other only by Guid id, never by direct entity reference. Cross-aggregate orchestration (capacity check, opening a sibling turma, enrolling) lives in the `MatricularAlunoCommandHandler` (Application layer) — never inside the aggregates themselves.

**Tech Stack:** .NET 9, EF Core (Npgsql/Postgres), MediatR, FluentValidation, xUnit, FluentAssertions — all already referenced by the existing projects; no new packages needed.

## Global Constraints

- Domain project referencia nada (nem EF Core, nem outros projetos) — ver `backend/src/SistemaEscolar.Domain/SistemaEscolar.Domain.csproj`.
- Application depende só de Domain; Infrastructure implementa as interfaces definidas no Domain.
- Um agregado por arquivo. Toda regra de negócio fica no agregado (Domain); handlers da Application só orquestram.
- Repositórios expõem só métodos com significado de negócio, nunca `IQueryable` genérico.
- Mensagens de erro e nomes de métodos em português, no mesmo estilo do agregado `Aluno` (ver `backend/src/SistemaEscolar.Domain/Alunos/Aluno.cs`).
- Cada Command da Application define seu próprio `Result<T>` local (record, no mesmo namespace do Command) — nunca importar `SistemaEscolar.Domain.Common` no mesmo arquivo que usa esse `Result<T>` explicitamente, para não gerar ambiguidade de nomes (ver `CriarAlunoCommand.cs` como referência: o handler nunca declara o tipo `Result<T>` do Domain explicitamente, só usa `var` + acesso a propriedades).
- Todos os comandos rodam a partir do diretório `backend/` (`dotnet build`, `dotnet test`).
- Nenhum segredo (connection string, chaves) é commitado — não se aplica a este plano (nenhuma credencial é tocada).

---

## File Structure

```
backend/src/SistemaEscolar.Domain/Turmas/
  TurnoTurma.cs          (enum)
  TurmaEvents.cs         (TurmaCriadaEvent, TurmaLotadaEvent)
  Turma.cs               (aggregate root)
  ITurmaRepository.cs

backend/src/SistemaEscolar.Domain/Matriculas/
  StatusMatricula.cs     (enum)
  MatriculaEvents.cs     (AlunoMatriculadoEvent)
  Matricula.cs            (aggregate root)
  IMatriculaRepository.cs

backend/src/SistemaEscolar.Application/Turmas/
  DTOs/TurmaDto.cs
  Commands/CriarTurma/{CriarTurmaCommand,CriarTurmaCommandHandler,CriarTurmaCommandValidator}.cs
  Queries/ObterTurmaPorId/{ObterTurmaPorIdQuery,ObterTurmaPorIdQueryHandler}.cs

backend/src/SistemaEscolar.Application/Matriculas/
  DTOs/MatriculaDto.cs
  Commands/MatricularAluno/{MatricularAlunoCommand,MatricularAlunoCommandHandler,MatricularAlunoCommandValidator}.cs
  Queries/ObterMatriculaPorId/{ObterMatriculaPorIdQuery,ObterMatriculaPorIdQueryHandler}.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/
  TurmaConfiguration.cs
  MatriculaConfiguration.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/
  TurmaRepository.cs
  MatriculaRepository.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/AppDbContext.cs   (modify: add DbSets)
backend/src/SistemaEscolar.Infrastructure/DependencyInjection.cs        (modify: register repos)

backend/src/SistemaEscolar.Api/Controllers/
  TurmasController.cs
  MatriculasController.cs

backend/tests/SistemaEscolar.UnitTests/Domain/
  TurmaTests.cs
  MatriculaTests.cs

infra/supabase/
  002_create_turmas.sql
  003_create_matriculas.sql
```

---

### Task 1: Domain — agregado `Turma`

**Files:**
- Create: `backend/src/SistemaEscolar.Domain/Turmas/TurnoTurma.cs`
- Create: `backend/src/SistemaEscolar.Domain/Turmas/TurmaEvents.cs`
- Create: `backend/src/SistemaEscolar.Domain/Turmas/Turma.cs`
- Create: `backend/src/SistemaEscolar.Domain/Turmas/ITurmaRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Domain/TurmaTests.cs`

**Interfaces:**
- Consumes: `SistemaEscolar.Domain.Common.{AggregateRoot, Result, Result<T>, IDomainEvent}` (already exist).
- Produces (used by later tasks): `TurnoTurma` enum (`Manha=1, Tarde=2, Noite=3, Integral=4`); `Turma` with public properties `Id, AnoLetivoId, AnoEscolarId, NomeBase, Sufixo (char), Turno, VagasMaximas, VagasOcupadas, CriadoEm, Nome (computed)`; static factory `Turma.Criar(string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, int vagasMaximas) -> Result<Turma>`; instance methods `Result<Turma> AbrirTurmaIrma()`, `Result OcuparVaga()`, `Result LiberarVaga()`, `bool PodeReceberNovaMatricula()`; `ITurmaRepository` with `Task<Turma?> ObterPorIdAsync(Guid, CancellationToken)`, `Task<List<Turma>> ObterTurmasDoGrupoAsync(string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken)`, `Task AdicionarAsync(Turma, CancellationToken)`, `void Atualizar(Turma)`.

- [ ] **Step 1: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Domain/TurmaTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Domain.Turmas;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class TurmaTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarTurmaComSufixoA()
    {
        var resultado = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Sufixo.Should().Be('A');
        resultado.Valor.VagasOcupadas.Should().Be(0);
        resultado.Valor.Nome.Should().Be("3º Ano A");
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is TurmaCriadaEvent);
    }

    [Fact]
    public void Criar_SemNomeBase_DeveFalhar()
    {
        var resultado = Turma.Criar("", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome base da turma é obrigatório.");
    }

    [Fact]
    public void Criar_ComVagasMaximasZeroOuNegativo_DeveFalhar()
    {
        var resultado = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 0);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Número de vagas máximas deve ser maior que zero.");
    }

    [Fact]
    public void OcuparVaga_AteAtingirLimite_DeveDispararTurmaLotadaEvent()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 2).Valor!;

        var primeiraOcupacao = turma.OcuparVaga();
        primeiraOcupacao.Sucesso.Should().BeTrue();
        turma.DomainEvents.Should().NotContain(e => e is TurmaLotadaEvent);

        var segundaOcupacao = turma.OcuparVaga();
        segundaOcupacao.Sucesso.Should().BeTrue();
        turma.VagasOcupadas.Should().Be(2);
        turma.DomainEvents.Should().ContainSingle(e => e is TurmaLotadaEvent);
    }

    [Fact]
    public void OcuparVaga_QuandoJaLotada_DeveFalhar()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 1).Valor!;
        turma.OcuparVaga();

        var resultado = turma.OcuparVaga();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Turma está lotada.");
    }

    [Fact]
    public void PodeReceberNovaMatricula_ComVagaDisponivel_DeveSerVerdadeiro()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 1).Valor!;

        turma.PodeReceberNovaMatricula().Should().BeTrue();
    }

    [Fact]
    public void PodeReceberNovaMatricula_Lotada_DeveSerFalso()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 1).Valor!;
        turma.OcuparVaga();

        turma.PodeReceberNovaMatricula().Should().BeFalse();
    }

    [Fact]
    public void AbrirTurmaIrma_DeveIncrementarSufixo()
    {
        var turmaA = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30).Valor!;

        var resultado = turmaA.AbrirTurmaIrma();

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Sufixo.Should().Be('B');
        resultado.Valor.Nome.Should().Be("3º Ano B");
        resultado.Valor.VagasMaximas.Should().Be(turmaA.VagasMaximas);
        resultado.Valor.Id.Should().NotBe(turmaA.Id);
    }

    [Fact]
    public void AbrirTurmaIrma_AlemDoLimiteZ_DeveFalhar()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30).Valor!;

        for (var letra = 'B'; letra <= 'Z'; letra++)
            turma = turma.AbrirTurmaIrma().Valor!;

        var resultado = turma.AbrirTurmaIrma();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Contain("Limite de turmas irmãs");
    }

    [Fact]
    public void LiberarVaga_ComVagaOcupada_DeveDecrementarContador()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 5).Valor!;
        turma.OcuparVaga();

        var resultado = turma.LiberarVaga();

        resultado.Sucesso.Should().BeTrue();
        turma.VagasOcupadas.Should().Be(0);
    }

    [Fact]
    public void LiberarVaga_SemVagaOcupada_DeveFalhar()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 5).Valor!;

        var resultado = turma.LiberarVaga();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Turma não possui vaga ocupada para liberar.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `SistemaEscolar.Domain.Turmas` namespace / `Turma` type does not exist yet.

- [ ] **Step 3: Create `TurnoTurma.cs`**

```csharp
namespace SistemaEscolar.Domain.Turmas;

public enum TurnoTurma
{
    Manha = 1,
    Tarde = 2,
    Noite = 3,
    Integral = 4
}
```

- [ ] **Step 4: Create `TurmaEvents.cs`**

```csharp
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Turmas;

/// <summary>
/// Disparado quando uma turma é criada — seja a primeira turma de um grupo
/// (Turma.Criar) ou uma turma-irmã aberta automaticamente por lotação
/// (Turma.AbrirTurmaIrma). Outros contextos podem reagir, ex: notificar a
/// coordenação pedagógica.
/// </summary>
public sealed record TurmaCriadaEvent(Guid TurmaId, string Nome, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando a turma atinge o número máximo de vagas ao ocupar a
/// última vaga disponível.
/// </summary>
public sealed record TurmaLotadaEvent(Guid TurmaId, DateTime OcorridoEm) : IDomainEvent;
```

- [ ] **Step 5: Create `Turma.cs`**

```csharp
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
```

- [ ] **Step 6: Create `ITurmaRepository.cs`**

```csharp
namespace SistemaEscolar.Domain.Turmas;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// Expõe apenas operações com significado de negócio.
/// </summary>
public interface ITurmaRepository
{
    Task<Turma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Turma>> ObterTurmasDoGrupoAsync(
        string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken cancellationToken);

    Task AdicionarAsync(Turma turma, CancellationToken cancellationToken);
    void Atualizar(Turma turma);
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~TurmaTests`
Expected: PASS, 11 tests.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Domain/Turmas backend/tests/SistemaEscolar.UnitTests/Domain/TurmaTests.cs
git commit -m "feat(domain): adiciona agregado Turma com controle de vagas e abertura automática de turma-irmã"
```

---

### Task 2: Domain — agregado `Matricula`

**Files:**
- Create: `backend/src/SistemaEscolar.Domain/Matriculas/StatusMatricula.cs`
- Create: `backend/src/SistemaEscolar.Domain/Matriculas/MatriculaEvents.cs`
- Create: `backend/src/SistemaEscolar.Domain/Matriculas/Matricula.cs`
- Create: `backend/src/SistemaEscolar.Domain/Matriculas/IMatriculaRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Domain/MatriculaTests.cs`

**Interfaces:**
- Consumes: `SistemaEscolar.Domain.Common.{AggregateRoot, Result, Result<T>, IDomainEvent}`.
- Produces (used by later tasks): `StatusMatricula` enum (`Ativa=1, Trancada=2, Cancelada=3, Concluida=4`); `Matricula` with public properties `Id, AlunoId, TurmaId, AnoLetivoId, Status, MatriculadoEm`; static factory `Matricula.Matricular(Guid alunoId, Guid turmaId, Guid anoLetivoId) -> Result<Matricula>`; instance methods `Result Trancar()`, `Result Cancelar()`, `Result Concluir()`; `IMatriculaRepository` with `Task<Matricula?> ObterPorIdAsync(Guid, CancellationToken)`, `Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken)`, `Task AdicionarAsync(Matricula, CancellationToken)`, `void Atualizar(Matricula)`.

- [ ] **Step 1: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Domain/MatriculaTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Domain.Matriculas;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class MatriculaTests
{
    [Fact]
    public void Matricular_ComDadosValidos_DeveCriarMatriculaAtiva()
    {
        var resultado = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be(StatusMatricula.Ativa);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is AlunoMatriculadoEvent);
    }

    [Fact]
    public void Matricular_SemAluno_DeveFalhar()
    {
        var resultado = Matricula.Matricular(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Matrícula precisa estar vinculada a um aluno.");
    }

    [Fact]
    public void Matricular_SemTurma_DeveFalhar()
    {
        var resultado = Matricula.Matricular(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Matrícula precisa estar vinculada a uma turma.");
    }

    [Fact]
    public void Matricular_SemAnoLetivo_DeveFalhar()
    {
        var resultado = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Matrícula precisa estar vinculada a um ano letivo.");
    }

    [Fact]
    public void Trancar_MatriculaAtiva_DeveTrancar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = matricula.Trancar();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Trancada);
    }

    [Fact]
    public void Trancar_MatriculaCancelada_DeveFalhar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Cancelar();

        var resultado = matricula.Trancar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível trancar uma matrícula ativa.");
    }

    [Fact]
    public void Cancelar_MatriculaAtiva_DeveCancelar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = matricula.Cancelar();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Cancelada);
    }

    [Fact]
    public void Cancelar_MatriculaTrancada_DeveCancelar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Trancar();

        var resultado = matricula.Cancelar();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Cancelada);
    }

    [Fact]
    public void Cancelar_MatriculaConcluida_DeveFalhar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Concluir();

        var resultado = matricula.Cancelar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Não é possível cancelar uma matrícula já concluída.");
    }

    [Fact]
    public void Concluir_MatriculaAtiva_DeveConcluir()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = matricula.Concluir();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Concluida);
    }

    [Fact]
    public void Concluir_MatriculaCancelada_DeveFalhar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Cancelar();

        var resultado = matricula.Concluir();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível concluir uma matrícula ativa.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `SistemaEscolar.Domain.Matriculas` namespace / `Matricula` type does not exist yet.

- [ ] **Step 3: Create `StatusMatricula.cs`**

```csharp
namespace SistemaEscolar.Domain.Matriculas;

public enum StatusMatricula
{
    Ativa = 1,
    Trancada = 2,
    Cancelada = 3,
    Concluida = 4
}
```

- [ ] **Step 4: Create `MatriculaEvents.cs`**

```csharp
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Matriculas;

/// <summary>
/// Disparado quando um aluno é matriculado em uma turma. O contexto
/// Financeiro pode ouvir este evento para gerar a cobrança de matrícula.
/// </summary>
public sealed record AlunoMatriculadoEvent(Guid MatriculaId, Guid AlunoId, Guid TurmaId, DateTime OcorridoEm) : IDomainEvent;
```

- [ ] **Step 5: Create `Matricula.cs`**

```csharp
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
```

- [ ] **Step 6: Create `IMatriculaRepository.cs`**

```csharp
namespace SistemaEscolar.Domain.Matriculas;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IMatriculaRepository
{
    Task<Matricula?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken cancellationToken);
    Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken);
    void Atualizar(Matricula matricula);
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~MatriculaTests`
Expected: PASS, 11 tests.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Domain/Matriculas backend/tests/SistemaEscolar.UnitTests/Domain/MatriculaTests.cs
git commit -m "feat(domain): adiciona agregado Matricula com ciclo de status Ativa/Trancada/Cancelada/Concluida"
```

---

### Task 3: Application — caso de uso `CriarTurma` e query `ObterTurmaPorId`

**Files:**
- Create: `backend/src/SistemaEscolar.Application/Turmas/DTOs/TurmaDto.cs`
- Create: `backend/src/SistemaEscolar.Application/Turmas/Commands/CriarTurma/CriarTurmaCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/Turmas/Commands/CriarTurma/CriarTurmaCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/Turmas/Commands/CriarTurma/CriarTurmaCommandValidator.cs`
- Create: `backend/src/SistemaEscolar.Application/Turmas/Queries/ObterTurmaPorId/ObterTurmaPorIdQuery.cs`
- Create: `backend/src/SistemaEscolar.Application/Turmas/Queries/ObterTurmaPorId/ObterTurmaPorIdQueryHandler.cs`

**Interfaces:**
- Consumes: `Turma`, `TurnoTurma`, `ITurmaRepository` (Task 1); `SistemaEscolar.Application.Common.IUnitOfWork` (existing).
- Produces (used by Task 6 - Api): `TurmaDto(Guid Id, string NomeBase, char Sufixo, string Nome, string Turno, Guid AnoLetivoId, Guid AnoEscolarId, int VagasMaximas, int VagasOcupadas)`; `CriarTurmaCommand(string NomeBase, TurnoTurma Turno, Guid AnoLetivoId, Guid AnoEscolarId, int VagasMaximas) : IRequest<Result<TurmaDto>>`; `ObterTurmaPorIdQuery(Guid TurmaId) : IRequest<Result<TurmaDto>>`. Note: `Result<T>` here is the Application-level record defined in `CriarTurmaCommand.cs`.

- [ ] **Step 1: Create `TurmaDto.cs`**

```csharp
namespace SistemaEscolar.Application.Turmas.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record TurmaDto(
    Guid Id,
    string NomeBase,
    char Sufixo,
    string Nome,
    string Turno,
    Guid AnoLetivoId,
    Guid AnoEscolarId,
    int VagasMaximas,
    int VagasOcupadas
);
```

- [ ] **Step 2: Create `CriarTurmaCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Turmas.DTOs;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Application.Turmas.Commands.CriarTurma;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record CriarTurmaCommand(
    string NomeBase,
    TurnoTurma Turno,
    Guid AnoLetivoId,
    Guid AnoEscolarId,
    int VagasMaximas
) : IRequest<Result<TurmaDto>>;

/// <summary>
/// Envelope simples de resultado para a camada de Application (distinto do
/// Result do Domain, para não vazar tipo de domínio até a Api).
/// </summary>
public sealed record Result<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);
}
```

- [ ] **Step 3: Create `CriarTurmaCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Turmas.DTOs;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Application.Turmas.Commands.CriarTurma;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de Turma.cs (Domain).
/// </summary>
public sealed class CriarTurmaCommandHandler
    : IRequestHandler<CriarTurmaCommand, Result<TurmaDto>>
{
    private readonly ITurmaRepository _turmaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarTurmaCommandHandler(ITurmaRepository turmaRepository, IUnitOfWork unitOfWork)
    {
        _turmaRepository = turmaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TurmaDto>> Handle(CriarTurmaCommand request, CancellationToken cancellationToken)
    {
        var turmaResult = Turma.Criar(
            request.NomeBase,
            request.Turno,
            request.AnoLetivoId,
            request.AnoEscolarId,
            request.VagasMaximas);

        if (!turmaResult.Sucesso)
            return Result<TurmaDto>.Falha(turmaResult.Erro!);

        var turma = turmaResult.Valor!;

        await _turmaRepository.AdicionarAsync(turma, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<TurmaDto>.Ok(new TurmaDto(
            turma.Id,
            turma.NomeBase,
            turma.Sufixo,
            turma.Nome,
            turma.Turno.ToString(),
            turma.AnoLetivoId,
            turma.AnoEscolarId,
            turma.VagasMaximas,
            turma.VagasOcupadas));
    }
}
```

- [ ] **Step 4: Create `CriarTurmaCommandValidator.cs`**

```csharp
using FluentValidation;

namespace SistemaEscolar.Application.Turmas.Commands.CriarTurma;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarTurmaCommandValidator : AbstractValidator<CriarTurmaCommand>
{
    public CriarTurmaCommandValidator()
    {
        RuleFor(c => c.NomeBase)
            .NotEmpty().WithMessage("Nome base da turma é obrigatório.")
            .MaximumLength(100);

        RuleFor(c => c.Turno)
            .IsInEnum().WithMessage("Turno inválido.");

        RuleFor(c => c.AnoLetivoId)
            .NotEmpty().WithMessage("Ano letivo é obrigatório.");

        RuleFor(c => c.AnoEscolarId)
            .NotEmpty().WithMessage("Ano escolar é obrigatório.");

        RuleFor(c => c.VagasMaximas)
            .GreaterThan(0).WithMessage("Número de vagas máximas deve ser maior que zero.");
    }
}
```

- [ ] **Step 5: Create `ObterTurmaPorIdQuery.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Turmas.Commands.CriarTurma;
using SistemaEscolar.Application.Turmas.DTOs;

namespace SistemaEscolar.Application.Turmas.Queries.ObterTurmaPorId;

public sealed record ObterTurmaPorIdQuery(Guid TurmaId) : IRequest<Result<TurmaDto>>;
```

- [ ] **Step 6: Create `ObterTurmaPorIdQueryHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Turmas.Commands.CriarTurma;
using SistemaEscolar.Application.Turmas.DTOs;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Application.Turmas.Queries.ObterTurmaPorId;

public sealed class ObterTurmaPorIdQueryHandler
    : IRequestHandler<ObterTurmaPorIdQuery, Result<TurmaDto>>
{
    private readonly ITurmaRepository _turmaRepository;

    public ObterTurmaPorIdQueryHandler(ITurmaRepository turmaRepository)
    {
        _turmaRepository = turmaRepository;
    }

    public async Task<Result<TurmaDto>> Handle(ObterTurmaPorIdQuery request, CancellationToken cancellationToken)
    {
        var turma = await _turmaRepository.ObterPorIdAsync(request.TurmaId, cancellationToken);

        if (turma is null)
            return Result<TurmaDto>.Falha("Turma não encontrada.");

        return Result<TurmaDto>.Ok(new TurmaDto(
            turma.Id,
            turma.NomeBase,
            turma.Sufixo,
            turma.Nome,
            turma.Turno.ToString(),
            turma.AnoLetivoId,
            turma.AnoEscolarId,
            turma.VagasMaximas,
            turma.VagasOcupadas));
    }
}
```

- [ ] **Step 7: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded (Application project only depends on Domain, which already compiles from Task 1).

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Application/Turmas
git commit -m "feat(application): adiciona casos de uso CriarTurma e ObterTurmaPorId"
```

---

### Task 4: Application — caso de uso `MatricularAluno` (orquestração) e query `ObterMatriculaPorId`

**Files:**
- Create: `backend/src/SistemaEscolar.Application/Matriculas/DTOs/MatriculaDto.cs`
- Create: `backend/src/SistemaEscolar.Application/Matriculas/Commands/MatricularAluno/MatricularAlunoCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/Matriculas/Commands/MatricularAluno/MatricularAlunoCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/Matriculas/Commands/MatricularAluno/MatricularAlunoCommandValidator.cs`
- Create: `backend/src/SistemaEscolar.Application/Matriculas/Queries/ObterMatriculaPorId/ObterMatriculaPorIdQuery.cs`
- Create: `backend/src/SistemaEscolar.Application/Matriculas/Queries/ObterMatriculaPorId/ObterMatriculaPorIdQueryHandler.cs`

**Interfaces:**
- Consumes: `Matricula`, `StatusMatricula`, `IMatriculaRepository` (Task 2); `Turma`, `TurnoTurma`, `ITurmaRepository` (Task 1); `SistemaEscolar.Domain.Alunos.IAlunoRepository` (existing, from `Aluno`); `IUnitOfWork` (existing).
- Produces (used by Task 6 - Api): `MatriculaDto(Guid Id, Guid AlunoId, Guid TurmaId, Guid AnoLetivoId, string Status, DateTime MatriculadoEm)`; `MatricularAlunoCommand(Guid AlunoId, Guid TurmaId, Guid AnoLetivoId) : IRequest<Result<MatriculaDto>>`; `ObterMatriculaPorIdQuery(Guid MatriculaId) : IRequest<Result<MatriculaDto>>`.

- [ ] **Step 1: Create `MatriculaDto.cs`**

```csharp
namespace SistemaEscolar.Application.Matriculas.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record MatriculaDto(
    Guid Id,
    Guid AlunoId,
    Guid TurmaId,
    Guid AnoLetivoId,
    string Status,
    DateTime MatriculadoEm
);
```

- [ ] **Step 2: Create `MatricularAlunoCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Matriculas.DTOs;

namespace SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;

/// <summary>
/// Comando = intenção do usuário. TurmaId é a turma pretendida; se estiver
/// lotada, o handler procura ou abre automaticamente uma turma-irmã (ver
/// MatricularAlunoCommandHandler).
/// </summary>
public sealed record MatricularAlunoCommand(
    Guid AlunoId,
    Guid TurmaId,
    Guid AnoLetivoId
) : IRequest<Result<MatriculaDto>>;

/// <summary>
/// Envelope simples de resultado para a camada de Application (distinto do
/// Result do Domain, para não vazar tipo de domínio até a Api).
/// </summary>
public sealed record Result<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);
}
```

- [ ] **Step 3: Create `MatricularAlunoCommandHandler.cs`**

```csharp
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
```

- [ ] **Step 4: Create `MatricularAlunoCommandValidator.cs`**

```csharp
using FluentValidation;

namespace SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain e no
/// handler (orquestração entre agregados).
/// </summary>
public sealed class MatricularAlunoCommandValidator : AbstractValidator<MatricularAlunoCommand>
{
    public MatricularAlunoCommandValidator()
    {
        RuleFor(c => c.AlunoId)
            .NotEmpty().WithMessage("Aluno é obrigatório.");

        RuleFor(c => c.TurmaId)
            .NotEmpty().WithMessage("Turma é obrigatória.");

        RuleFor(c => c.AnoLetivoId)
            .NotEmpty().WithMessage("Ano letivo é obrigatório.");
    }
}
```

- [ ] **Step 5: Create `ObterMatriculaPorIdQuery.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Application.Matriculas.DTOs;

namespace SistemaEscolar.Application.Matriculas.Queries.ObterMatriculaPorId;

public sealed record ObterMatriculaPorIdQuery(Guid MatriculaId) : IRequest<Result<MatriculaDto>>;
```

- [ ] **Step 6: Create `ObterMatriculaPorIdQueryHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Application.Matriculas.DTOs;
using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.Application.Matriculas.Queries.ObterMatriculaPorId;

public sealed class ObterMatriculaPorIdQueryHandler
    : IRequestHandler<ObterMatriculaPorIdQuery, Result<MatriculaDto>>
{
    private readonly IMatriculaRepository _matriculaRepository;

    public ObterMatriculaPorIdQueryHandler(IMatriculaRepository matriculaRepository)
    {
        _matriculaRepository = matriculaRepository;
    }

    public async Task<Result<MatriculaDto>> Handle(ObterMatriculaPorIdQuery request, CancellationToken cancellationToken)
    {
        var matricula = await _matriculaRepository.ObterPorIdAsync(request.MatriculaId, cancellationToken);

        if (matricula is null)
            return Result<MatriculaDto>.Falha("Matrícula não encontrada.");

        return Result<MatriculaDto>.Ok(new MatriculaDto(
            matricula.Id,
            matricula.AlunoId,
            matricula.TurmaId,
            matricula.AnoLetivoId,
            matricula.Status.ToString(),
            matricula.MatriculadoEm));
    }
}
```

- [ ] **Step 7: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Application/Matriculas
git commit -m "feat(application): adiciona caso de uso MatricularAluno com abertura automatica de turma-irma"
```

---

### Task 5: Infrastructure — persistência (EF Core + SQL) para `Turma` e `Matricula`

**Files:**
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/TurmaConfiguration.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/MatriculaConfiguration.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/TurmaRepository.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/MatriculaRepository.cs`
- Modify: `backend/src/SistemaEscolar.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/src/SistemaEscolar.Infrastructure/DependencyInjection.cs`
- Create: `infra/supabase/002_create_turmas.sql`
- Create: `infra/supabase/003_create_matriculas.sql`

**Interfaces:**
- Consumes: `Turma`, `TurnoTurma`, `ITurmaRepository` (Task 1); `Matricula`, `StatusMatricula`, `IMatriculaRepository` (Task 2); existing `AppDbContext`, `DependencyInjection.AdicionarInfrastructure`.
- Produces (used by Task 6 - Api, via DI): `ITurmaRepository` and `IMatriculaRepository` resolvable from the DI container; `AppDbContext.Turmas` and `AppDbContext.Matriculas` DbSets.

- [ ] **Step 1: Create `TurmaConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class TurmaConfiguration : IEntityTypeConfiguration<Turma>
{
    public void Configure(EntityTypeBuilder<Turma> builder)
    {
        builder.ToTable("turmas");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.AnoLetivoId)
            .HasColumnName("ano_letivo_id")
            .IsRequired();

        builder.Property(t => t.AnoEscolarId)
            .HasColumnName("ano_escolar_id")
            .IsRequired();

        builder.Property(t => t.NomeBase)
            .HasColumnName("nome_base")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Sufixo)
            .HasColumnName("sufixo")
            .HasConversion<string>()
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(t => t.Turno)
            .HasColumnName("turno")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.VagasMaximas)
            .HasColumnName("vagas_maximas")
            .IsRequired();

        builder.Property(t => t.VagasOcupadas)
            .HasColumnName("vagas_ocupadas")
            .IsRequired();

        builder.Property(t => t.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        // Nome é propriedade computada em memória (NomeBase + Sufixo), não persistida.
        builder.Ignore(t => t.Nome);

        // Defesa em profundidade: garante no banco que não existem duas
        // turmas com o mesmo sufixo no mesmo grupo (mesma checagem que
        // ObterTurmasDoGrupoAsync usa para procurar vaga).
        builder.HasIndex(t => new { t.NomeBase, t.Turno, t.AnoLetivoId, t.AnoEscolarId, t.Sufixo })
            .IsUnique();
    }
}
```

- [ ] **Step 2: Create `MatriculaConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("matriculas");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.AlunoId)
            .HasColumnName("aluno_id")
            .IsRequired();

        builder.Property(m => m.TurmaId)
            .HasColumnName("turma_id")
            .IsRequired();

        builder.Property(m => m.AnoLetivoId)
            .HasColumnName("ano_letivo_id")
            .IsRequired();

        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.MatriculadoEm)
            .HasColumnName("matriculado_em")
            .IsRequired();

        // Defesa em profundidade: reforça no banco a regra de negócio "no
        // máximo uma matrícula Ativa por aluno/ano letivo" (índice único
        // parcial), a mesma que ExisteMatriculaAtivaAsync já checa na Application.
        builder.HasIndex(m => new { m.AlunoId, m.AnoLetivoId })
            .IsUnique()
            .HasFilter("status = 'Ativa'");
    }
}
```

- [ ] **Step 3: Create `TurmaRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de ITurmaRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class TurmaRepository : ITurmaRepository
{
    private readonly AppDbContext _context;

    public TurmaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Turma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Turmas
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<List<Turma>> ObterTurmasDoGrupoAsync(
        string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken cancellationToken) =>
        _context.Turmas
            .Where(t => t.NomeBase == nomeBase
                && t.Turno == turno
                && t.AnoLetivoId == anoLetivoId
                && t.AnoEscolarId == anoEscolarId)
            .OrderBy(t => t.Sufixo)
            .ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Turma turma, CancellationToken cancellationToken) =>
        await _context.Turmas.AddAsync(turma, cancellationToken);

    public void Atualizar(Turma turma) =>
        _context.Turmas.Update(turma);
}
```

- [ ] **Step 4: Create `MatriculaRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IMatriculaRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class MatriculaRepository : IMatriculaRepository
{
    private readonly AppDbContext _context;

    public MatriculaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Matricula?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Matriculas
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        _context.Matriculas
            .AnyAsync(m => m.AlunoId == alunoId
                && m.AnoLetivoId == anoLetivoId
                && m.Status == StatusMatricula.Ativa, cancellationToken);

    public async Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken) =>
        await _context.Matriculas.AddAsync(matricula, cancellationToken);

    public void Atualizar(Matricula matricula) =>
        _context.Matriculas.Update(matricula);
}
```

- [ ] **Step 5: Modify `AppDbContext.cs` to add the new DbSets**

In `backend/src/SistemaEscolar.Infrastructure/Persistence/AppDbContext.cs`, change the top of the file from:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IPublisher? _publisher;

    public DbSet<Aluno> Alunos => Set<Aluno>();
```

to:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.Common;
using SistemaEscolar.Domain.Matriculas;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IPublisher? _publisher;

    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
```

Leave the rest of the file (constructor, `OnModelCreating`, `SaveChangesAsync`) unchanged — `ApplyConfigurationsFromAssembly` already picks up the two new `IEntityTypeConfiguration<>` classes automatically.

- [ ] **Step 6: Modify `DependencyInjection.cs` to register the new repositories**

In `backend/src/SistemaEscolar.Infrastructure/DependencyInjection.cs`, change:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Infrastructure.Persistence;
using SistemaEscolar.Infrastructure.Persistence.Repositories;
```

to:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.Matriculas;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.Infrastructure.Persistence;
using SistemaEscolar.Infrastructure.Persistence.Repositories;
```

And change:

```csharp
        services.AddScoped<IAlunoRepository, AlunoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
```

to:

```csharp
        services.AddScoped<IAlunoRepository, AlunoRepository>();
        services.AddScoped<ITurmaRepository, TurmaRepository>();
        services.AddScoped<IMatriculaRepository, MatriculaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
```

- [ ] **Step 7: Create `infra/supabase/002_create_turmas.sql`**

```sql
-- Migração: tabela "turmas" (agregado Turma)
-- Compatível com o mapeamento EF Core em TurmaConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 001_create_alunos.sql.

create table if not exists turmas (
    id uuid primary key,
    ano_letivo_id uuid not null,
    ano_escolar_id uuid not null,
    nome_base varchar(100) not null,
    sufixo char(1) not null,
    turno varchar(20) not null,
    vagas_maximas integer not null,
    vagas_ocupadas integer not null default 0,
    criado_em timestamp not null default now()
);

create unique index if not exists ux_turmas_grupo_sufixo
    on turmas (nome_base, turno, ano_letivo_id, ano_escolar_id, sufixo);

comment on table turmas is 'Agregado raiz Turma (bounded context Acadêmico).';
comment on column turmas.sufixo is
    'Diferencia turmas-irmãs do mesmo grupo (mesmo nome_base/turno/ano), aberta automaticamente quando a anterior lota.';
```

- [ ] **Step 8: Create `infra/supabase/003_create_matriculas.sql`**

```sql
-- Migração: tabela "matriculas" (agregado Matricula)
-- Compatível com o mapeamento EF Core em MatriculaConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 002_create_turmas.sql.

create table if not exists matriculas (
    id uuid primary key,
    aluno_id uuid not null,
    turma_id uuid not null,
    ano_letivo_id uuid not null,
    status varchar(20) not null default 'Ativa',
    matriculado_em timestamp not null default now()
);

create unique index if not exists ux_matriculas_aluno_ativa
    on matriculas (aluno_id, ano_letivo_id)
    where status = 'Ativa';

comment on table matriculas is 'Agregado raiz Matricula (bounded context Acadêmico).';
comment on column matriculas.status is
    'Índice único parcial garante no máximo uma matrícula Ativa por aluno/ano letivo.';
```

- [ ] **Step 9: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 10: Commit**

```bash
git add backend/src/SistemaEscolar.Infrastructure infra/supabase/002_create_turmas.sql infra/supabase/003_create_matriculas.sql
git commit -m "feat(infrastructure): mapeamento EF Core, repositorios e migracoes SQL para Turma e Matricula"
```

---

### Task 6: Api — `TurmasController` e `MatriculasController`

**Files:**
- Create: `backend/src/SistemaEscolar.Api/Controllers/TurmasController.cs`
- Create: `backend/src/SistemaEscolar.Api/Controllers/MatriculasController.cs`

**Interfaces:**
- Consumes: `CriarTurmaCommand`, `ObterTurmaPorIdQuery`, `TurmaDto` (Task 3); `MatricularAlunoCommand`, `ObterMatriculaPorIdQuery`, `MatriculaDto` (Task 4); `TurnoTurma` (Task 1). `Program.cs` already registers MediatR/FluentValidation from `typeof(CriarAlunoCommand).Assembly` — since `CriarTurmaCommand` and `MatricularAlunoCommand` live in the same `SistemaEscolar.Application` assembly, no changes to `Program.cs` are needed.
- Produces: `POST /api/turmas`, `GET /api/turmas/{id}`, `POST /api/matriculas`, `GET /api/matriculas/{id}` HTTP endpoints.

- [ ] **Step 1: Create `TurmasController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Turmas.Commands.CriarTurma;
using SistemaEscolar.Application.Turmas.Queries.ObterTurmaPorId;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/turmas")]
public sealed class TurmasController : ControllerBase
{
    private readonly ISender _sender;

    public TurmasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarTurmaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarTurmaCommand(
            request.NomeBase,
            request.Turno,
            request.AnoLetivoId,
            request.AnoEscolarId,
            request.VagasMaximas);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterTurmaPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarTurmaRequest(
    string NomeBase,
    TurnoTurma Turno,
    Guid AnoLetivoId,
    Guid AnoEscolarId,
    int VagasMaximas);
```

- [ ] **Step 2: Create `MatriculasController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Application.Matriculas.Queries.ObterMatriculaPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/matriculas")]
public sealed class MatriculasController : ControllerBase
{
    private readonly ISender _sender;

    public MatriculasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Matricular(
        [FromBody] MatricularAlunoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MatricularAlunoCommand(
            request.AlunoId,
            request.TurmaId,
            request.AnoLetivoId);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterMatriculaPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record MatricularAlunoRequest(
    Guid AlunoId,
    Guid TurmaId,
    Guid AnoLetivoId);
```

- [ ] **Step 3: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded — all 4 projects (Domain, Application, Infrastructure, Api) compile together.

- [ ] **Step 4: Commit**

```bash
git add backend/src/SistemaEscolar.Api/Controllers/TurmasController.cs backend/src/SistemaEscolar.Api/Controllers/MatriculasController.cs
git commit -m "feat(api): adiciona TurmasController e MatriculasController"
```

---

### Task 7: Verificação final — build e suite de testes completa

**Files:** none (verification only).

**Interfaces:** none — this task only runs the full solution build and test suite to confirm all previous tasks integrate correctly.

- [ ] **Step 1: Full solution build**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded, 0 errors, across `SistemaEscolar.Domain`, `SistemaEscolar.Application`, `SistemaEscolar.Infrastructure`, `SistemaEscolar.Api`, `SistemaEscolar.UnitTests`.

- [ ] **Step 2: Full test suite**

Run (from `backend/`): `dotnet test`
Expected: PASS — all `AlunoTests` (existing) + all `TurmaTests` + all `MatriculaTests` (new, ~22 tests) pass, 0 failed.

- [ ] **Step 3: If everything passes, no further action needed — this task has no commit (verification only)**

If any test fails or build breaks, fix the issue in the task that introduced it (do not patch forward from Task 7) and re-run Steps 1–2.

---

## Fora de escopo (ver spec)

- Validar `Status` do `Aluno` antes de matricular.
- Reativar uma `Matricula` trancada/cancelada.
- Query dedicada de turmas com vaga disponível.
- Integração com Financeiro (cobrança de matrícula ao ouvir `AlunoMatriculadoEvent`).
