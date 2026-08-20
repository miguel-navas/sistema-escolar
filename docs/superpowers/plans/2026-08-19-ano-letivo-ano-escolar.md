# AnoLetivo & AnoEscolar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `AnoLetivo` and `AnoEscolar` aggregates (Domain, Application, Infrastructure, Api, unit tests) to complete Etapa 1 of `docs/ROTEIRO.md`, and enforce the etapa's remaining business rule — "Uma Turma só aceita matrícula se pertencer a um AnoLetivo com status Ativo" — in the already-shipped `MatricularAlunoCommandHandler`.

**Architecture:** Clean Architecture / DDD, mirroring the existing `Aluno`/`Turma`/`Matricula` aggregates exactly. `AnoLetivo` and `AnoEscolar` are separate aggregate roots in the Academico bounded context. `Turma`/`Matricula` already reference them only by Guid (`AnoLetivoId`, `AnoEscolarId`) — this plan does not change that. The "one AnoLetivo Ativo at a time" rule requires cross-aggregate lookup, so it lives in `AtivarAnoLetivoCommandHandler` (Application), never inside `AnoLetivo` itself. The "Turma only accepts enrollment in an Ativo AnoLetivo" rule is added to the existing `MatricularAlunoCommandHandler`.

**Tech Stack:** .NET 9, EF Core (Npgsql/Postgres), MediatR, FluentValidation, xUnit, FluentAssertions — all already referenced; no new packages needed except a `ProjectReference` from the test project to `SistemaEscolar.Application` (first Application-layer unit tests in this repo).

**Spec:** `docs/DOMAIN.md` (entity list, already includes `AnoLetivo`/`AnoEscolar`), `docs/ROTEIRO.md` (Etapa 1 — this plan's source requirement), `backend/EXEMPLO-ALUNO.md` (reference pattern), `backend/CLAUDE.md` (backend conventions). Executors should read all four before starting.

## Global Constraints

- Domain project references nothing (not even EF Core or other projects) — see `backend/src/SistemaEscolar.Domain/SistemaEscolar.Domain.csproj`.
- Application depends only on Domain; Infrastructure implements the interfaces defined in Domain.
- One aggregate per file. All business rules live in the aggregate (Domain); Application handlers only orchestrate.
- Repositories expose only business-meaningful methods, never generic `IQueryable`.
- Error messages and method names in Portuguese, matching the `Aluno`/`Turma`/`Matricula` style.
- Each Application Command defines its own local `Result<T>` (record, same namespace as the Command) — never import `SistemaEscolar.Domain.Common` in the same file that uses that Command-local `Result<T>` explicitly (see `CriarTurmaCommand.cs` as reference: handlers never declare the Domain `Result<T>` type explicitly, only use `var` + property access).
- All commands run from the `backend/` directory (`dotnet build`, `dotnet test`).
- No secrets committed.
- **Confirmed design decision (AnoLetivo):** only one `AnoLetivo` may be `Ativo` at a time. Enforced in `AtivarAnoLetivoCommandHandler` via `IAnoLetivoRepository.ExisteOutroAnoLetivoAtivoAsync`, never inside `AnoLetivo.cs` — the aggregate only knows how to manage its own state, not query siblings.
- **Confirmed design decision (DB integrity):** the new SQL migrations add real `FOREIGN KEY` constraints from `turmas`/`matriculas` to `anos_letivos`/`anos_escolares`. This is DB-only integrity — the Domain still references these aggregates only by Guid, never by object reference.
- **Confirmed design decision (Application testing):** this plan introduces the project's first Application-layer unit tests, using hand-written in-memory fake repositories (no mocking framework) under `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/`.
- Cross-context communication is via domain events only, never direct entity references (backend/CLAUDE.md).
- Repositories must work against "pure" PostgreSQL (Supabase today, RDS tomorrow) — no Supabase-specific SQL features.

---

## File Structure

```
docs/ROTEIRO.md                                          (new — synced from main repo root)
CLAUDE.md                                                 (modify — synced from main repo root)

backend/src/SistemaEscolar.Domain/AnosLetivos/
  StatusAnoLetivo.cs      (enum)
  AnoLetivoEvents.cs      (AnoLetivoCriadoEvent, AnoLetivoAtivadoEvent, AnoLetivoEncerradoEvent)
  AnoLetivo.cs            (aggregate root)
  IAnoLetivoRepository.cs

backend/src/SistemaEscolar.Domain/AnosEscolares/
  NivelEnsino.cs          (enum)
  AnoEscolarEvents.cs     (AnoEscolarCriadoEvent)
  AnoEscolar.cs           (aggregate root)
  IAnoEscolarRepository.cs

backend/src/SistemaEscolar.Application/AnosLetivos/
  DTOs/AnoLetivoDto.cs
  Commands/CriarAnoLetivo/{CriarAnoLetivoCommand,CriarAnoLetivoCommandHandler,CriarAnoLetivoCommandValidator}.cs
  Commands/AtivarAnoLetivo/{AtivarAnoLetivoCommand,AtivarAnoLetivoCommandHandler}.cs
  Commands/EncerrarAnoLetivo/{EncerrarAnoLetivoCommand,EncerrarAnoLetivoCommandHandler}.cs
  Queries/ObterAnoLetivoPorId/{ObterAnoLetivoPorIdQuery,ObterAnoLetivoPorIdQueryHandler}.cs

backend/src/SistemaEscolar.Application/AnosEscolares/
  DTOs/AnoEscolarDto.cs
  Commands/CriarAnoEscolar/{CriarAnoEscolarCommand,CriarAnoEscolarCommandHandler,CriarAnoEscolarCommandValidator}.cs
  Queries/ObterAnoEscolarPorId/{ObterAnoEscolarPorIdQuery,ObterAnoEscolarPorIdQueryHandler}.cs

backend/src/SistemaEscolar.Application/Matriculas/Commands/MatricularAluno/
  MatricularAlunoCommandHandler.cs   (modify: enforce AnoLetivo Ativo)

backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/
  AnoLetivoConfiguration.cs
  AnoEscolarConfiguration.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/
  AnoLetivoRepository.cs
  AnoEscolarRepository.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/AppDbContext.cs   (modify: add DbSets)
backend/src/SistemaEscolar.Infrastructure/DependencyInjection.cs        (modify: register repos)

backend/src/SistemaEscolar.Api/Controllers/
  AnosLetivosController.cs
  AnosEscolaresController.cs

backend/tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj   (modify: add Application ProjectReference)
backend/tests/SistemaEscolar.UnitTests/Domain/
  AnoLetivoTests.cs
  AnoEscolarTests.cs
backend/tests/SistemaEscolar.UnitTests/Application/Fakes/
  FakeAlunoRepository.cs
  FakeAnoLetivoRepository.cs
  FakeTurmaRepository.cs
  FakeMatriculaRepository.cs
  FakeUnitOfWork.cs
backend/tests/SistemaEscolar.UnitTests/Application/
  MatricularAlunoCommandHandlerTests.cs

infra/supabase/
  004_create_anos_letivos.sql
  005_create_anos_escolares.sql
  006_add_fk_turmas_matriculas.sql

docs/ROTEIRO.md      (modify: check off Etapa 1 items)
docs/DOMAIN.md        (modify: add new domain events)
docs/DECISIONS.md     (modify: add ADR-004, ADR-005)
```

---

### Task 0: Sync roteiro docs into this worktree

**Files:**
- Create: `docs/ROTEIRO.md`
- Modify: `CLAUDE.md`

**Context:** `docs/ROTEIRO.md` and an updated `CLAUDE.md` (with a new "Manutenção de documentação" section) exist at the main repo root (`C:\AI\sistema-escolar\`, **not** this worktree) but were never committed — another session edited them directly in the main checkout's working tree. This worktree branched before that edit, so it still has the old `CLAUDE.md` and no `ROTEIRO.md` at all. Both files must be brought into this branch before the rest of this plan makes sense (this plan itself is Etapa 1 of that roteiro).

**Interfaces:**
- Consumes: nothing.
- Produces: `docs/ROTEIRO.md` present in this worktree/branch; `CLAUDE.md` in this worktree matches the main repo root's current content (which is a superset of the old one — only additions, nothing removed, verified by diff).

- [ ] **Step 1: Copy `docs/ROTEIRO.md` from the main repo root into this worktree**

Run (from the worktree root, `C:\AI\sistema-escolar\.claude\worktrees\turma-matricula`):

```bash
cp "C:/AI/sistema-escolar/docs/ROTEIRO.md" "docs/ROTEIRO.md"
```

- [ ] **Step 2: Copy the updated `CLAUDE.md` from the main repo root into this worktree**

```bash
cp "C:/AI/sistema-escolar/CLAUDE.md" "CLAUDE.md"
```

- [ ] **Step 3: Verify both files are now present and diff shows only additions**

Run: `git diff --stat CLAUDE.md`
Expected: `CLAUDE.md` shows insertions only (the new "Manutenção de documentação" section), no deletions. `docs/ROTEIRO.md` is untracked (new file).

- [ ] **Step 4: Commit**

```bash
git add docs/ROTEIRO.md CLAUDE.md
git commit -m "docs: sincroniza roteiro de implementacao e CLAUDE.md com o repo principal"
```

---

### Task 1: Domain — agregado `AnoLetivo`

**Files:**
- Create: `backend/src/SistemaEscolar.Domain/AnosLetivos/StatusAnoLetivo.cs`
- Create: `backend/src/SistemaEscolar.Domain/AnosLetivos/AnoLetivoEvents.cs`
- Create: `backend/src/SistemaEscolar.Domain/AnosLetivos/AnoLetivo.cs`
- Create: `backend/src/SistemaEscolar.Domain/AnosLetivos/IAnoLetivoRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Domain/AnoLetivoTests.cs`

**Interfaces:**
- Consumes: `SistemaEscolar.Domain.Common.{AggregateRoot, Result, Result<T>, IDomainEvent}` (already exist).
- Produces (used by later tasks): `StatusAnoLetivo` enum (`Planejado=1, Ativo=2, Encerrado=3`); `AnoLetivo` with public properties `Id, Ano (int), DataInicio (DateOnly), DataFim (DateOnly), Status (StatusAnoLetivo), CriadoEm (DateTime)`; static factory `AnoLetivo.Criar(int ano, DateOnly dataInicio, DateOnly dataFim) -> Result<AnoLetivo>` (always starts `Planejado`); instance methods `Result Ativar()`, `Result Encerrar()`; `IAnoLetivoRepository` with `Task<AnoLetivo?> ObterPorIdAsync(Guid, CancellationToken)`, `Task<bool> ExisteOutroAnoLetivoAtivoAsync(Guid idExcluido, CancellationToken)`, `Task AdicionarAsync(AnoLetivo, CancellationToken)`, `void Atualizar(AnoLetivo)`.

- [ ] **Step 1: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Domain/AnoLetivoTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Domain.AnosLetivos;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class AnoLetivoTests
{
    private static readonly DateOnly DataInicioPadrao = new(2026, 2, 1);
    private static readonly DateOnly DataFimPadrao = new(2026, 12, 15);

    [Fact]
    public void Criar_ComDadosValidos_DeveCriarAnoLetivoPlanejado()
    {
        var resultado = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be(StatusAnoLetivo.Planejado);
        resultado.Valor.Ano.Should().Be(2026);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is AnoLetivoCriadoEvent);
    }

    [Fact]
    public void Criar_ComAnoForaDoIntervalo_DeveFalhar()
    {
        var resultado = AnoLetivo.Criar(1999, DataInicioPadrao, DataFimPadrao);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo deve estar entre 2000 e 2100.");
    }

    [Fact]
    public void Criar_ComDataFimAntesOuIgualDataInicio_DeveFalhar()
    {
        var resultado = AnoLetivo.Criar(2026, DataInicioPadrao, DataInicioPadrao);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Data de fim deve ser posterior à data de início.");
    }

    [Fact]
    public void Ativar_AnoLetivoPlanejado_DeveAtivarEDispararEvento()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;

        var resultado = anoLetivo.Ativar();

        resultado.Sucesso.Should().BeTrue();
        anoLetivo.Status.Should().Be(StatusAnoLetivo.Ativo);
        anoLetivo.DomainEvents.Should().ContainSingle(e => e is AnoLetivoAtivadoEvent);
    }

    [Fact]
    public void Ativar_AnoLetivoJaAtivo_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();

        var resultado = anoLetivo.Ativar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo já está ativo.");
    }

    [Fact]
    public void Ativar_AnoLetivoEncerrado_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();
        anoLetivo.Encerrar();

        var resultado = anoLetivo.Ativar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Não é possível ativar um ano letivo encerrado.");
    }

    [Fact]
    public void Encerrar_AnoLetivoAtivo_DeveEncerrarEDispararEvento()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();

        var resultado = anoLetivo.Encerrar();

        resultado.Sucesso.Should().BeTrue();
        anoLetivo.Status.Should().Be(StatusAnoLetivo.Encerrado);
        anoLetivo.DomainEvents.Should().Contain(e => e is AnoLetivoEncerradoEvent);
    }

    [Fact]
    public void Encerrar_AnoLetivoPlanejado_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;

        var resultado = anoLetivo.Encerrar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Não é possível encerrar um ano letivo que ainda não foi ativado.");
    }

    [Fact]
    public void Encerrar_AnoLetivoJaEncerrado_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();
        anoLetivo.Encerrar();

        var resultado = anoLetivo.Encerrar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo já está encerrado.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `SistemaEscolar.Domain.AnosLetivos` namespace / `AnoLetivo` type does not exist yet.

- [ ] **Step 3: Create `StatusAnoLetivo.cs`**

```csharp
namespace SistemaEscolar.Domain.AnosLetivos;

public enum StatusAnoLetivo
{
    Planejado = 1,
    Ativo = 2,
    Encerrado = 3
}
```

- [ ] **Step 4: Create `AnoLetivoEvents.cs`**

```csharp
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.AnosLetivos;

/// <summary>
/// Disparado quando um ano letivo é criado (sempre como Planejado).
/// </summary>
public sealed record AnoLetivoCriadoEvent(Guid AnoLetivoId, int Ano, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando um ano letivo passa a ser o corrente. Outros contextos
/// (ex: relatórios, painel) podem reagir para saber qual é o ano letivo vigente.
/// </summary>
public sealed record AnoLetivoAtivadoEvent(Guid AnoLetivoId, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando um ano letivo é fechado. Pode ser ouvido pelo contexto
/// Acadêmico para disparar cálculos de situação final (Etapa 5).
/// </summary>
public sealed record AnoLetivoEncerradoEvent(Guid AnoLetivoId, DateTime OcorridoEm) : IDomainEvent;
```

- [ ] **Step 5: Create `AnoLetivo.cs`**

```csharp
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
```

- [ ] **Step 6: Create `IAnoLetivoRepository.cs`**

```csharp
namespace SistemaEscolar.Domain.AnosLetivos;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IAnoLetivoRepository
{
    Task<AnoLetivo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Usado pela Application para reforçar "só um ano letivo Ativo por vez"
    /// antes de ativar um novo — exclui o próprio id da checagem.
    /// </summary>
    Task<bool> ExisteOutroAnoLetivoAtivoAsync(Guid idExcluido, CancellationToken cancellationToken);

    Task AdicionarAsync(AnoLetivo anoLetivo, CancellationToken cancellationToken);
    void Atualizar(AnoLetivo anoLetivo);
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~AnoLetivoTests`
Expected: PASS, 9 tests.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Domain/AnosLetivos backend/tests/SistemaEscolar.UnitTests/Domain/AnoLetivoTests.cs
git commit -m "feat(domain): adiciona agregado AnoLetivo com ciclo Planejado/Ativo/Encerrado"
```

---

### Task 2: Domain — agregado `AnoEscolar`

**Files:**
- Create: `backend/src/SistemaEscolar.Domain/AnosEscolares/NivelEnsino.cs`
- Create: `backend/src/SistemaEscolar.Domain/AnosEscolares/AnoEscolarEvents.cs`
- Create: `backend/src/SistemaEscolar.Domain/AnosEscolares/AnoEscolar.cs`
- Create: `backend/src/SistemaEscolar.Domain/AnosEscolares/IAnoEscolarRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Domain/AnoEscolarTests.cs`

**Interfaces:**
- Consumes: `SistemaEscolar.Domain.Common.{AggregateRoot, Result, Result<T>, IDomainEvent}`.
- Produces (used by later tasks): `NivelEnsino` enum (`EducacaoInfantil=1, FundamentalAnosIniciais=2, FundamentalAnosFinais=3, EnsinoMedio=4`); `AnoEscolar` with public properties `Id, Nome (string), NivelEnsino (NivelEnsino), CriadoEm (DateTime)`; static factory `AnoEscolar.Criar(string nome, NivelEnsino nivelEnsino) -> Result<AnoEscolar>`; `IAnoEscolarRepository` with `Task<AnoEscolar?> ObterPorIdAsync(Guid, CancellationToken)`, `Task AdicionarAsync(AnoEscolar, CancellationToken)`. No `Atualizar` — `AnoEscolar` has no mutable state after creation.

- [ ] **Step 1: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Domain/AnoEscolarTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Domain.AnosEscolares;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class AnoEscolarTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarAnoEscolar()
    {
        var resultado = AnoEscolar.Criar("3º Ano", NivelEnsino.FundamentalAnosIniciais);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Nome.Should().Be("3º Ano");
        resultado.Valor.NivelEnsino.Should().Be(NivelEnsino.FundamentalAnosIniciais);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is AnoEscolarCriadoEvent);
    }

    [Fact]
    public void Criar_SemNome_DeveFalhar()
    {
        var resultado = AnoEscolar.Criar("", NivelEnsino.EnsinoMedio);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome do ano escolar é obrigatório.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `SistemaEscolar.Domain.AnosEscolares` namespace / `AnoEscolar` type does not exist yet.

- [ ] **Step 3: Create `NivelEnsino.cs`**

```csharp
namespace SistemaEscolar.Domain.AnosEscolares;

public enum NivelEnsino
{
    EducacaoInfantil = 1,
    FundamentalAnosIniciais = 2,
    FundamentalAnosFinais = 3,
    EnsinoMedio = 4
}
```

- [ ] **Step 4: Create `AnoEscolarEvents.cs`**

```csharp
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.AnosEscolares;

/// <summary>
/// Disparado quando um ano escolar/série é criado.
/// </summary>
public sealed record AnoEscolarCriadoEvent(Guid AnoEscolarId, string Nome, DateTime OcorridoEm) : IDomainEvent;
```

- [ ] **Step 5: Create `AnoEscolar.cs`**

```csharp
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
```

- [ ] **Step 6: Create `IAnoEscolarRepository.cs`**

```csharp
namespace SistemaEscolar.Domain.AnosEscolares;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IAnoEscolarRepository
{
    Task<AnoEscolar?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task AdicionarAsync(AnoEscolar anoEscolar, CancellationToken cancellationToken);
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~AnoEscolarTests`
Expected: PASS, 2 tests.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Domain/AnosEscolares backend/tests/SistemaEscolar.UnitTests/Domain/AnoEscolarTests.cs
git commit -m "feat(domain): adiciona agregado AnoEscolar"
```

---

### Task 3: Application — casos de uso de `AnoLetivo`

**Files:**
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/DTOs/AnoLetivoDto.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Commands/CriarAnoLetivo/CriarAnoLetivoCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Commands/CriarAnoLetivo/CriarAnoLetivoCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Commands/CriarAnoLetivo/CriarAnoLetivoCommandValidator.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Commands/AtivarAnoLetivo/AtivarAnoLetivoCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Commands/AtivarAnoLetivo/AtivarAnoLetivoCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Commands/EncerrarAnoLetivo/EncerrarAnoLetivoCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Commands/EncerrarAnoLetivo/EncerrarAnoLetivoCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Queries/ObterAnoLetivoPorId/ObterAnoLetivoPorIdQuery.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosLetivos/Queries/ObterAnoLetivoPorId/ObterAnoLetivoPorIdQueryHandler.cs`

**Interfaces:**
- Consumes: `AnoLetivo`, `StatusAnoLetivo`, `IAnoLetivoRepository` (Task 1); `SistemaEscolar.Application.Common.IUnitOfWork` (existing).
- Produces (used by Task 6 - Infrastructure DI, Task 7 - Api): `AnoLetivoDto(Guid Id, int Ano, DateOnly DataInicio, DateOnly DataFim, string Status)`; `CriarAnoLetivoCommand(int Ano, DateOnly DataInicio, DateOnly DataFim) : IRequest<Result<AnoLetivoDto>>`; `AtivarAnoLetivoCommand(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>`; `EncerrarAnoLetivoCommand(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>`; `ObterAnoLetivoPorIdQuery(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>`. `Result<T>` here is the Application-level record defined in `CriarAnoLetivoCommand.cs` — the other three files import it from there (same pattern as `ObterTurmaPorIdQuery.cs` importing from `CriarTurmaCommand.cs`).

- [ ] **Step 1: Create `AnoLetivoDto.cs`**

```csharp
namespace SistemaEscolar.Application.AnosLetivos.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record AnoLetivoDto(
    Guid Id,
    int Ano,
    DateOnly DataInicio,
    DateOnly DataFim,
    string Status
);
```

- [ ] **Step 2: Create `CriarAnoLetivoCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record CriarAnoLetivoCommand(
    int Ano,
    DateOnly DataInicio,
    DateOnly DataFim
) : IRequest<Result<AnoLetivoDto>>;

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

- [ ] **Step 3: Create `CriarAnoLetivoCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de AnoLetivo.cs (Domain).
/// </summary>
public sealed class CriarAnoLetivoCommandHandler
    : IRequestHandler<CriarAnoLetivoCommand, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarAnoLetivoCommandHandler(IAnoLetivoRepository anoLetivoRepository, IUnitOfWork unitOfWork)
    {
        _anoLetivoRepository = anoLetivoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoLetivoDto>> Handle(CriarAnoLetivoCommand request, CancellationToken cancellationToken)
    {
        var anoLetivoResult = AnoLetivo.Criar(request.Ano, request.DataInicio, request.DataFim);

        if (!anoLetivoResult.Sucesso)
            return Result<AnoLetivoDto>.Falha(anoLetivoResult.Erro!);

        var anoLetivo = anoLetivoResult.Valor!;

        await _anoLetivoRepository.AdicionarAsync(anoLetivo, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<AnoLetivoDto>.Ok(new AnoLetivoDto(
            anoLetivo.Id,
            anoLetivo.Ano,
            anoLetivo.DataInicio,
            anoLetivo.DataFim,
            anoLetivo.Status.ToString()));
    }
}
```

- [ ] **Step 4: Create `CriarAnoLetivoCommandValidator.cs`**

```csharp
using FluentValidation;

namespace SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarAnoLetivoCommandValidator : AbstractValidator<CriarAnoLetivoCommand>
{
    public CriarAnoLetivoCommandValidator()
    {
        RuleFor(c => c.Ano)
            .InclusiveBetween(2000, 2100).WithMessage("Ano letivo deve estar entre 2000 e 2100.");

        RuleFor(c => c.DataFim)
            .GreaterThan(c => c.DataInicio).WithMessage("Data de fim deve ser posterior à data de início.");
    }
}
```

- [ ] **Step 5: Create `AtivarAnoLetivoCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Commands.AtivarAnoLetivo;

public sealed record AtivarAnoLetivoCommand(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>;
```

- [ ] **Step 6: Create `AtivarAnoLetivoCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Commands.AtivarAnoLetivo;

/// <summary>
/// Handler = orquestrador. A regra "só um ano letivo Ativo por vez" exige
/// consultar outros agregados AnoLetivo via repositório — por isso vive
/// aqui, não em AnoLetivo.cs (que só sabe cuidar de si mesmo).
/// </summary>
public sealed class AtivarAnoLetivoCommandHandler
    : IRequestHandler<AtivarAnoLetivoCommand, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AtivarAnoLetivoCommandHandler(IAnoLetivoRepository anoLetivoRepository, IUnitOfWork unitOfWork)
    {
        _anoLetivoRepository = anoLetivoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoLetivoDto>> Handle(AtivarAnoLetivoCommand request, CancellationToken cancellationToken)
    {
        var anoLetivo = await _anoLetivoRepository.ObterPorIdAsync(request.AnoLetivoId, cancellationToken);
        if (anoLetivo is null)
            return Result<AnoLetivoDto>.Falha("Ano letivo não encontrado.");

        var existeOutroAtivo = await _anoLetivoRepository.ExisteOutroAnoLetivoAtivoAsync(
            request.AnoLetivoId, cancellationToken);
        if (existeOutroAtivo)
            return Result<AnoLetivoDto>.Falha("Já existe um ano letivo ativo. Encerre-o antes de ativar outro.");

        var ativarResult = anoLetivo.Ativar();
        if (!ativarResult.Sucesso)
            return Result<AnoLetivoDto>.Falha(ativarResult.Erro!);

        _anoLetivoRepository.Atualizar(anoLetivo);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<AnoLetivoDto>.Ok(new AnoLetivoDto(
            anoLetivo.Id,
            anoLetivo.Ano,
            anoLetivo.DataInicio,
            anoLetivo.DataFim,
            anoLetivo.Status.ToString()));
    }
}
```

- [ ] **Step 7: Create `EncerrarAnoLetivoCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Commands.EncerrarAnoLetivo;

public sealed record EncerrarAnoLetivoCommand(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>;
```

- [ ] **Step 8: Create `EncerrarAnoLetivoCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Commands.EncerrarAnoLetivo;

public sealed class EncerrarAnoLetivoCommandHandler
    : IRequestHandler<EncerrarAnoLetivoCommand, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EncerrarAnoLetivoCommandHandler(IAnoLetivoRepository anoLetivoRepository, IUnitOfWork unitOfWork)
    {
        _anoLetivoRepository = anoLetivoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoLetivoDto>> Handle(EncerrarAnoLetivoCommand request, CancellationToken cancellationToken)
    {
        var anoLetivo = await _anoLetivoRepository.ObterPorIdAsync(request.AnoLetivoId, cancellationToken);
        if (anoLetivo is null)
            return Result<AnoLetivoDto>.Falha("Ano letivo não encontrado.");

        var encerrarResult = anoLetivo.Encerrar();
        if (!encerrarResult.Sucesso)
            return Result<AnoLetivoDto>.Falha(encerrarResult.Erro!);

        _anoLetivoRepository.Atualizar(anoLetivo);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<AnoLetivoDto>.Ok(new AnoLetivoDto(
            anoLetivo.Id,
            anoLetivo.Ano,
            anoLetivo.DataInicio,
            anoLetivo.DataFim,
            anoLetivo.Status.ToString()));
    }
}
```

- [ ] **Step 9: Create `ObterAnoLetivoPorIdQuery.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;

namespace SistemaEscolar.Application.AnosLetivos.Queries.ObterAnoLetivoPorId;

public sealed record ObterAnoLetivoPorIdQuery(Guid AnoLetivoId) : IRequest<Result<AnoLetivoDto>>;
```

- [ ] **Step 10: Create `ObterAnoLetivoPorIdQueryHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.DTOs;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Application.AnosLetivos.Queries.ObterAnoLetivoPorId;

public sealed class ObterAnoLetivoPorIdQueryHandler
    : IRequestHandler<ObterAnoLetivoPorIdQuery, Result<AnoLetivoDto>>
{
    private readonly IAnoLetivoRepository _anoLetivoRepository;

    public ObterAnoLetivoPorIdQueryHandler(IAnoLetivoRepository anoLetivoRepository)
    {
        _anoLetivoRepository = anoLetivoRepository;
    }

    public async Task<Result<AnoLetivoDto>> Handle(ObterAnoLetivoPorIdQuery request, CancellationToken cancellationToken)
    {
        var anoLetivo = await _anoLetivoRepository.ObterPorIdAsync(request.AnoLetivoId, cancellationToken);

        if (anoLetivo is null)
            return Result<AnoLetivoDto>.Falha("Ano letivo não encontrado.");

        return Result<AnoLetivoDto>.Ok(new AnoLetivoDto(
            anoLetivo.Id,
            anoLetivo.Ano,
            anoLetivo.DataInicio,
            anoLetivo.DataFim,
            anoLetivo.Status.ToString()));
    }
}
```

- [ ] **Step 11: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 12: Commit**

```bash
git add backend/src/SistemaEscolar.Application/AnosLetivos
git commit -m "feat(application): adiciona casos de uso CriarAnoLetivo, AtivarAnoLetivo, EncerrarAnoLetivo e ObterAnoLetivoPorId"
```

---

### Task 4: Application — casos de uso de `AnoEscolar`

**Files:**
- Create: `backend/src/SistemaEscolar.Application/AnosEscolares/DTOs/AnoEscolarDto.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosEscolares/Commands/CriarAnoEscolar/CriarAnoEscolarCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosEscolares/Commands/CriarAnoEscolar/CriarAnoEscolarCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosEscolares/Commands/CriarAnoEscolar/CriarAnoEscolarCommandValidator.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosEscolares/Queries/ObterAnoEscolarPorId/ObterAnoEscolarPorIdQuery.cs`
- Create: `backend/src/SistemaEscolar.Application/AnosEscolares/Queries/ObterAnoEscolarPorId/ObterAnoEscolarPorIdQueryHandler.cs`

**Interfaces:**
- Consumes: `AnoEscolar`, `NivelEnsino`, `IAnoEscolarRepository` (Task 2); `IUnitOfWork` (existing).
- Produces (used by Task 6 - Infrastructure DI, Task 7 - Api): `AnoEscolarDto(Guid Id, string Nome, string NivelEnsino)`; `CriarAnoEscolarCommand(string Nome, NivelEnsino NivelEnsino) : IRequest<Result<AnoEscolarDto>>`; `ObterAnoEscolarPorIdQuery(Guid AnoEscolarId) : IRequest<Result<AnoEscolarDto>>`. `Result<T>` here is the Application-level record defined in `CriarAnoEscolarCommand.cs`.

- [ ] **Step 1: Create `AnoEscolarDto.cs`**

```csharp
namespace SistemaEscolar.Application.AnosEscolares.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record AnoEscolarDto(
    Guid Id,
    string Nome,
    string NivelEnsino
);
```

- [ ] **Step 2: Create `CriarAnoEscolarCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosEscolares.DTOs;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record CriarAnoEscolarCommand(
    string Nome,
    NivelEnsino NivelEnsino
) : IRequest<Result<AnoEscolarDto>>;

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

- [ ] **Step 3: Create `CriarAnoEscolarCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosEscolares.DTOs;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de AnoEscolar.cs (Domain).
/// </summary>
public sealed class CriarAnoEscolarCommandHandler
    : IRequestHandler<CriarAnoEscolarCommand, Result<AnoEscolarDto>>
{
    private readonly IAnoEscolarRepository _anoEscolarRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarAnoEscolarCommandHandler(IAnoEscolarRepository anoEscolarRepository, IUnitOfWork unitOfWork)
    {
        _anoEscolarRepository = anoEscolarRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnoEscolarDto>> Handle(CriarAnoEscolarCommand request, CancellationToken cancellationToken)
    {
        var anoEscolarResult = AnoEscolar.Criar(request.Nome, request.NivelEnsino);

        if (!anoEscolarResult.Sucesso)
            return Result<AnoEscolarDto>.Falha(anoEscolarResult.Erro!);

        var anoEscolar = anoEscolarResult.Valor!;

        await _anoEscolarRepository.AdicionarAsync(anoEscolar, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<AnoEscolarDto>.Ok(new AnoEscolarDto(
            anoEscolar.Id,
            anoEscolar.Nome,
            anoEscolar.NivelEnsino.ToString()));
    }
}
```

- [ ] **Step 4: Create `CriarAnoEscolarCommandValidator.cs`**

```csharp
using FluentValidation;

namespace SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarAnoEscolarCommandValidator : AbstractValidator<CriarAnoEscolarCommand>
{
    public CriarAnoEscolarCommandValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome do ano escolar é obrigatório.")
            .MaximumLength(100);

        RuleFor(c => c.NivelEnsino)
            .IsInEnum().WithMessage("Nível de ensino inválido.");
    }
}
```

- [ ] **Step 5: Create `ObterAnoEscolarPorIdQuery.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;
using SistemaEscolar.Application.AnosEscolares.DTOs;

namespace SistemaEscolar.Application.AnosEscolares.Queries.ObterAnoEscolarPorId;

public sealed record ObterAnoEscolarPorIdQuery(Guid AnoEscolarId) : IRequest<Result<AnoEscolarDto>>;
```

- [ ] **Step 6: Create `ObterAnoEscolarPorIdQueryHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;
using SistemaEscolar.Application.AnosEscolares.DTOs;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Application.AnosEscolares.Queries.ObterAnoEscolarPorId;

public sealed class ObterAnoEscolarPorIdQueryHandler
    : IRequestHandler<ObterAnoEscolarPorIdQuery, Result<AnoEscolarDto>>
{
    private readonly IAnoEscolarRepository _anoEscolarRepository;

    public ObterAnoEscolarPorIdQueryHandler(IAnoEscolarRepository anoEscolarRepository)
    {
        _anoEscolarRepository = anoEscolarRepository;
    }

    public async Task<Result<AnoEscolarDto>> Handle(ObterAnoEscolarPorIdQuery request, CancellationToken cancellationToken)
    {
        var anoEscolar = await _anoEscolarRepository.ObterPorIdAsync(request.AnoEscolarId, cancellationToken);

        if (anoEscolar is null)
            return Result<AnoEscolarDto>.Falha("Ano escolar não encontrado.");

        return Result<AnoEscolarDto>.Ok(new AnoEscolarDto(
            anoEscolar.Id,
            anoEscolar.Nome,
            anoEscolar.NivelEnsino.ToString()));
    }
}
```

- [ ] **Step 7: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Application/AnosEscolares
git commit -m "feat(application): adiciona casos de uso CriarAnoEscolar e ObterAnoEscolarPorId"
```

---

### Task 5: Application — `MatricularAlunoCommandHandler` passa a exigir `AnoLetivo` Ativo

**Files:**
- Modify: `backend/src/SistemaEscolar.Application/Matriculas/Commands/MatricularAluno/MatricularAlunoCommandHandler.cs`
- Modify: `backend/tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj` (add `ProjectReference` to Application)
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeAlunoRepository.cs`
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeAnoLetivoRepository.cs`
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeTurmaRepository.cs`
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeMatriculaRepository.cs`
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeUnitOfWork.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Application/MatricularAlunoCommandHandlerTests.cs`

**Context:** This is the etapa's explicit rule — "Uma Turma só aceita matrícula se pertencer a um AnoLetivo com status Ativo" — implemented as an early guard in the already-shipped `MatricularAlunoCommandHandler` (commits `ffe0f71`/PR #1, not yet merged, safe to amend in this branch). This is the project's first Application-layer test, so it also introduces the fakes folder and wires the test project to reference `SistemaEscolar.Application`.

**Interfaces:**
- Consumes: `IAnoLetivoRepository`, `StatusAnoLetivo` (Task 1); `IAlunoRepository`, `ITurmaRepository`, `IMatriculaRepository`, `MatricularAlunoCommand`, `MatricularAlunoCommandHandler` (all pre-existing, unchanged signatures except the handler's constructor, which gains one parameter).
- Produces: `MatricularAlunoCommandHandler` constructor now takes `(IAlunoRepository, IAnoLetivoRepository, ITurmaRepository, IMatriculaRepository, IUnitOfWork)` — Task 6's DI registration must supply all five. Fakes under `SistemaEscolar.UnitTests.Application.Fakes` are reusable by any future Application-layer test in this project.

- [ ] **Step 1: Add the Application project reference to the test project**

Edit `backend/tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`, inside the existing `<ItemGroup>` that has the Domain `ProjectReference`, add:

```xml
    <ProjectReference Include="..\..\src\SistemaEscolar.Application\SistemaEscolar.Application.csproj" />
```

So the `ItemGroup` reads:

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\SistemaEscolar.Domain\SistemaEscolar.Domain.csproj" />
    <ProjectReference Include="..\..\src\SistemaEscolar.Application\SistemaEscolar.Application.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Write the fakes**

Create `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeAlunoRepository.cs`:

```csharp
using SistemaEscolar.Domain.Alunos;

namespace SistemaEscolar.UnitTests.Application.Fakes;

/// <summary>
/// Repositório em memória para testar handlers de Application sem banco.
/// Implementa só o suficiente para os cenários testados.
/// </summary>
public sealed class FakeAlunoRepository : IAlunoRepository
{
    private readonly Dictionary<Guid, Aluno> _alunos = new();

    public void Semear(Aluno aluno) => _alunos[aluno.Id] = aluno;

    public Task<Aluno?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_alunos.GetValueOrDefault(id));

    public Task<bool> ExisteComCpfAsync(string cpfNumero, CancellationToken cancellationToken) =>
        Task.FromResult(_alunos.Values.Any(a => a.Cpf?.Numero == cpfNumero));

    public Task AdicionarAsync(Aluno aluno, CancellationToken cancellationToken)
    {
        _alunos[aluno.Id] = aluno;
        return Task.CompletedTask;
    }

    public void Atualizar(Aluno aluno) => _alunos[aluno.Id] = aluno;
}
```

Create `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeAnoLetivoRepository.cs`:

```csharp
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeAnoLetivoRepository : IAnoLetivoRepository
{
    private readonly Dictionary<Guid, AnoLetivo> _anosLetivos = new();

    public void Semear(AnoLetivo anoLetivo) => _anosLetivos[anoLetivo.Id] = anoLetivo;

    public Task<AnoLetivo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_anosLetivos.GetValueOrDefault(id));

    public Task<bool> ExisteOutroAnoLetivoAtivoAsync(Guid idExcluido, CancellationToken cancellationToken) =>
        Task.FromResult(_anosLetivos.Values.Any(a => a.Id != idExcluido && a.Status == StatusAnoLetivo.Ativo));

    public Task AdicionarAsync(AnoLetivo anoLetivo, CancellationToken cancellationToken)
    {
        _anosLetivos[anoLetivo.Id] = anoLetivo;
        return Task.CompletedTask;
    }

    public void Atualizar(AnoLetivo anoLetivo) => _anosLetivos[anoLetivo.Id] = anoLetivo;
}
```

Create `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeTurmaRepository.cs`:

```csharp
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeTurmaRepository : ITurmaRepository
{
    private readonly Dictionary<Guid, Turma> _turmas = new();

    public void Semear(Turma turma) => _turmas[turma.Id] = turma;

    public Task<Turma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_turmas.GetValueOrDefault(id));

    public Task<List<Turma>> ObterTurmasDoGrupoAsync(
        string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken cancellationToken) =>
        Task.FromResult(_turmas.Values
            .Where(t => t.NomeBase == nomeBase
                && t.Turno == turno
                && t.AnoLetivoId == anoLetivoId
                && t.AnoEscolarId == anoEscolarId)
            .OrderBy(t => t.Sufixo)
            .ToList());

    public Task AdicionarAsync(Turma turma, CancellationToken cancellationToken)
    {
        _turmas[turma.Id] = turma;
        return Task.CompletedTask;
    }

    public void Atualizar(Turma turma) => _turmas[turma.Id] = turma;
}
```

Create `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeMatriculaRepository.cs`:

```csharp
using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeMatriculaRepository : IMatriculaRepository
{
    private readonly Dictionary<Guid, Matricula> _matriculas = new();

    public void Semear(Matricula matricula) => _matriculas[matricula.Id] = matricula;

    public Task<Matricula?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_matriculas.GetValueOrDefault(id));

    public Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        Task.FromResult(_matriculas.Values.Any(m =>
            m.AlunoId == alunoId && m.AnoLetivoId == anoLetivoId && m.Status == StatusMatricula.Ativa));

    public Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        _matriculas[matricula.Id] = matricula;
        return Task.CompletedTask;
    }

    public void Atualizar(Matricula matricula) => _matriculas[matricula.Id] = matricula;
}
```

Create `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeUnitOfWork.cs`:

```csharp
using SistemaEscolar.Application.Common;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 3: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Application/MatricularAlunoCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.AnosLetivos;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.UnitTests.Application.Fakes;
using Xunit;

namespace SistemaEscolar.UnitTests.Application;

public sealed class MatricularAlunoCommandHandlerTests
{
    private readonly FakeAlunoRepository _alunoRepository = new();
    private readonly FakeAnoLetivoRepository _anoLetivoRepository = new();
    private readonly FakeTurmaRepository _turmaRepository = new();
    private readonly FakeMatriculaRepository _matriculaRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MatricularAlunoCommandHandler _handler;

    public MatricularAlunoCommandHandlerTests()
    {
        _handler = new MatricularAlunoCommandHandler(
            _alunoRepository, _anoLetivoRepository, _turmaRepository, _matriculaRepository, _unitOfWork);
    }

    private Aluno CriarAlunoSemeado()
    {
        var aluno = Aluno.Cadastrar("Maria Silva", new DateOnly(2015, 3, 10), null, Guid.NewGuid()).Valor!;
        _alunoRepository.Semear(aluno);
        return aluno;
    }

    private AnoLetivo CriarAnoLetivoSemeado(bool ativo)
    {
        var anoLetivo = AnoLetivo.Criar(2026, new DateOnly(2026, 2, 1), new DateOnly(2026, 12, 15)).Valor!;
        if (ativo)
            anoLetivo.Ativar();
        _anoLetivoRepository.Semear(anoLetivo);
        return anoLetivo;
    }

    private Turma CriarTurmaSemeada(Guid anoLetivoId, int vagasMaximas = 30)
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, anoLetivoId, Guid.NewGuid(), vagasMaximas).Valor!;
        _turmaRepository.Semear(turma);
        return turma;
    }

    [Fact]
    public async Task Handle_ComAnoLetivoAtivo_DeveMatricular()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: true);
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be("Ativa");
    }

    [Fact]
    public async Task Handle_ComAnoLetivoPlanejado_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: false);
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível matricular em um ano letivo ativo.");
    }

    [Fact]
    public async Task Handle_ComAnoLetivoEncerrado_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: true);
        anoLetivo.Encerrar();
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível matricular em um ano letivo ativo.");
    }

    [Fact]
    public async Task Handle_ComAnoLetivoInexistente_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var turma = CriarTurmaSemeada(Guid.NewGuid());

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, Guid.NewGuid()), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo não encontrado.");
    }

    [Fact]
    public async Task Handle_ComAlunoJaMatriculadoNoAnoLetivo_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: true);
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        await _handler.Handle(new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);
        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Aluno já possui matrícula ativa neste ano letivo.");
    }
}
```

- [ ] **Step 4: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `MatricularAlunoCommandHandler` has no constructor accepting `IAnoLetivoRepository` yet.

- [ ] **Step 5: Modify `MatricularAlunoCommandHandler.cs`**

Replace the full contents of `backend/src/SistemaEscolar.Application/Matriculas/Commands/MatricularAluno/MatricularAlunoCommandHandler.cs` with:

```csharp
using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Matriculas.DTOs;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.AnosLetivos;
using SistemaEscolar.Domain.Matriculas;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;

/// <summary>
/// Handler = orquestrador entre os agregados Aluno, AnoLetivo, Turma e
/// Matricula. Se a turma informada estiver lotada, procura uma turma-irmã
/// com vaga no mesmo grupo (NomeBase/Turno/AnoLetivo/AnoEscolar) e, se
/// nenhuma tiver vaga, abre uma nova automaticamente (Turma.AbrirTurmaIrma).
/// Toda regra de negócio real está em Turma.cs e Matricula.cs (Domain).
/// </summary>
public sealed class MatricularAlunoCommandHandler
    : IRequestHandler<MatricularAlunoCommand, Result<MatriculaDto>>
{
    private readonly IAlunoRepository _alunoRepository;
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IMatriculaRepository _matriculaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MatricularAlunoCommandHandler(
        IAlunoRepository alunoRepository,
        IAnoLetivoRepository anoLetivoRepository,
        ITurmaRepository turmaRepository,
        IMatriculaRepository matriculaRepository,
        IUnitOfWork unitOfWork)
    {
        _alunoRepository = alunoRepository;
        _anoLetivoRepository = anoLetivoRepository;
        _turmaRepository = turmaRepository;
        _matriculaRepository = matriculaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MatriculaDto>> Handle(MatricularAlunoCommand request, CancellationToken cancellationToken)
    {
        var aluno = await _alunoRepository.ObterPorIdAsync(request.AlunoId, cancellationToken);
        if (aluno is null)
            return Result<MatriculaDto>.Falha("Aluno não encontrado.");

        var anoLetivo = await _anoLetivoRepository.ObterPorIdAsync(request.AnoLetivoId, cancellationToken);
        if (anoLetivo is null)
            return Result<MatriculaDto>.Falha("Ano letivo não encontrado.");

        if (anoLetivo.Status != StatusAnoLetivo.Ativo)
            return Result<MatriculaDto>.Falha("Só é possível matricular em um ano letivo ativo.");

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

- [ ] **Step 6: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~MatricularAlunoCommandHandlerTests`
Expected: PASS, 5 tests. Then run the full suite (`dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`) to confirm nothing else broke — note `SistemaEscolar.Infrastructure` is NOT referenced by the test project, so this step does not (and cannot) verify the DI registration; that's covered by Task 6's build step.

- [ ] **Step 7: Commit**

```bash
git add backend/tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj backend/tests/SistemaEscolar.UnitTests/Application backend/src/SistemaEscolar.Application/Matriculas/Commands/MatricularAluno/MatricularAlunoCommandHandler.cs
git commit -m "feat(application): MatricularAluno passa a exigir AnoLetivo Ativo; adiciona primeiros testes de Application com fakes"
```

---

### Task 6: Infrastructure — persistência (EF Core + SQL) para `AnoLetivo` e `AnoEscolar`

**Files:**
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/AnoLetivoConfiguration.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/AnoEscolarConfiguration.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/AnoLetivoRepository.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/AnoEscolarRepository.cs`
- Modify: `backend/src/SistemaEscolar.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/src/SistemaEscolar.Infrastructure/DependencyInjection.cs`
- Create: `infra/supabase/004_create_anos_letivos.sql`
- Create: `infra/supabase/005_create_anos_escolares.sql`
- Create: `infra/supabase/006_add_fk_turmas_matriculas.sql`

**Interfaces:**
- Consumes: `AnoLetivo`, `StatusAnoLetivo`, `IAnoLetivoRepository` (Task 1); `AnoEscolar`, `NivelEnsino`, `IAnoEscolarRepository` (Task 2); existing `AppDbContext`, `DependencyInjection.AdicionarInfrastructure`; `MatricularAlunoCommandHandler`'s new constructor shape (Task 5) — DI must resolve `IAnoLetivoRepository`.
- Produces (used by Task 7 - Api): `IAnoLetivoRepository` and `IAnoEscolarRepository` resolvable from the DI container; `AppDbContext.AnosLetivos` and `AppDbContext.AnosEscolares` DbSets.

- [ ] **Step 1: Create `AnoLetivoConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class AnoLetivoConfiguration : IEntityTypeConfiguration<AnoLetivo>
{
    public void Configure(EntityTypeBuilder<AnoLetivo> builder)
    {
        builder.ToTable("anos_letivos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Ano)
            .HasColumnName("ano")
            .IsRequired();

        builder.Property(a => a.DataInicio)
            .HasColumnName("data_inicio")
            .IsRequired();

        builder.Property(a => a.DataFim)
            .HasColumnName("data_fim")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();
    }
}
```

- [ ] **Step 2: Create `AnoEscolarConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class AnoEscolarConfiguration : IEntityTypeConfiguration<AnoEscolar>
{
    public void Configure(EntityTypeBuilder<AnoEscolar> builder)
    {
        builder.ToTable("anos_escolares");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.NivelEnsino)
            .HasColumnName("nivel_ensino")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();
    }
}
```

- [ ] **Step 3: Create `AnoLetivoRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IAnoLetivoRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class AnoLetivoRepository : IAnoLetivoRepository
{
    private readonly AppDbContext _context;

    public AnoLetivoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AnoLetivo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.AnosLetivos
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExisteOutroAnoLetivoAtivoAsync(Guid idExcluido, CancellationToken cancellationToken) =>
        _context.AnosLetivos
            .AnyAsync(a => a.Id != idExcluido && a.Status == StatusAnoLetivo.Ativo, cancellationToken);

    public async Task AdicionarAsync(AnoLetivo anoLetivo, CancellationToken cancellationToken) =>
        await _context.AnosLetivos.AddAsync(anoLetivo, cancellationToken);

    public void Atualizar(AnoLetivo anoLetivo) =>
        _context.AnosLetivos.Update(anoLetivo);
}
```

- [ ] **Step 4: Create `AnoEscolarRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IAnoEscolarRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class AnoEscolarRepository : IAnoEscolarRepository
{
    private readonly AppDbContext _context;

    public AnoEscolarRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AnoEscolar?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.AnosEscolares
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AdicionarAsync(AnoEscolar anoEscolar, CancellationToken cancellationToken) =>
        await _context.AnosEscolares.AddAsync(anoEscolar, cancellationToken);
}
```

- [ ] **Step 5: Modify `AppDbContext.cs`**

Add these two usings alongside the existing ones at the top:

```csharp
using SistemaEscolar.Domain.AnosEscolares;
using SistemaEscolar.Domain.AnosLetivos;
```

Add these two DbSet properties alongside the existing `Alunos`/`Turmas`/`Matriculas` ones:

```csharp
    public DbSet<AnoLetivo> AnosLetivos => Set<AnoLetivo>();
    public DbSet<AnoEscolar> AnosEscolares => Set<AnoEscolar>();
```

- [ ] **Step 6: Modify `DependencyInjection.cs`**

Add these two usings alongside the existing `SistemaEscolar.Domain.*` ones:

```csharp
using SistemaEscolar.Domain.AnosEscolares;
using SistemaEscolar.Domain.AnosLetivos;
```

Add these two registrations alongside the existing `AddScoped<I...Repository, ...Repository>()` calls:

```csharp
        services.AddScoped<IAnoLetivoRepository, AnoLetivoRepository>();
        services.AddScoped<IAnoEscolarRepository, AnoEscolarRepository>();
```

- [ ] **Step 7: Create `004_create_anos_letivos.sql`**

```sql
-- Migração: tabela "anos_letivos" (agregado AnoLetivo)
-- Compatível com o mapeamento EF Core em AnoLetivoConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 003_create_matriculas.sql.

create table if not exists anos_letivos (
    id uuid primary key,
    ano integer not null,
    data_inicio date not null,
    data_fim date not null,
    status varchar(20) not null default 'Planejado',
    criado_em timestamp not null default now()
);

comment on table anos_letivos is 'Agregado raiz AnoLetivo (bounded context Acadêmico).';
comment on column anos_letivos.status is
    'Ciclo Planejado -> Ativo -> Encerrado. Regra "só um Ativo por vez" é reforçada na Application, não no banco.';
```

- [ ] **Step 8: Create `005_create_anos_escolares.sql`**

```sql
-- Migração: tabela "anos_escolares" (agregado AnoEscolar / série)
-- Compatível com o mapeamento EF Core em AnoEscolarConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 004_create_anos_letivos.sql.

create table if not exists anos_escolares (
    id uuid primary key,
    nome varchar(100) not null,
    nivel_ensino varchar(30) not null,
    criado_em timestamp not null default now()
);

comment on table anos_escolares is 'Agregado raiz AnoEscolar/Série (bounded context Acadêmico).';
```

- [ ] **Step 9: Create `006_add_fk_turmas_matriculas.sql`**

```sql
-- Migração: adiciona integridade referencial (FK) das tabelas turmas/matriculas
-- para anos_letivos/anos_escolares/alunos/turmas, agora que todas as tabelas
-- referenciadas existem. Rodar após 005_create_anos_escolares.sql.
--
-- Nota: o Domain (Turma, Matricula) continua referenciando Aluno/AnoLetivo/
-- AnoEscolar/Turma só por Guid (nunca por objeto), como manda DDD — esta FK
-- é só integridade de dados no banco, não acopla os agregados no código.

alter table turmas
    add constraint fk_turmas_ano_letivo
    foreign key (ano_letivo_id) references anos_letivos (id);

alter table turmas
    add constraint fk_turmas_ano_escolar
    foreign key (ano_escolar_id) references anos_escolares (id);

alter table matriculas
    add constraint fk_matriculas_ano_letivo
    foreign key (ano_letivo_id) references anos_letivos (id);

alter table matriculas
    add constraint fk_matriculas_turma
    foreign key (turma_id) references turmas (id);

alter table matriculas
    add constraint fk_matriculas_aluno
    foreign key (aluno_id) references alunos (id);
```

- [ ] **Step 10: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded — this is the first build that exercises `MatricularAlunoCommandHandler`'s new constructor against the DI container's registrations end-to-end.

- [ ] **Step 11: Commit**

```bash
git add backend/src/SistemaEscolar.Infrastructure infra/supabase/004_create_anos_letivos.sql infra/supabase/005_create_anos_escolares.sql infra/supabase/006_add_fk_turmas_matriculas.sql
git commit -m "feat(infrastructure): mapeamento EF Core, repositorios, DI e migracoes SQL para AnoLetivo e AnoEscolar, com FK para turmas/matriculas"
```

---

### Task 7: Api — `AnosLetivosController` e `AnosEscolaresController`

**Files:**
- Create: `backend/src/SistemaEscolar.Api/Controllers/AnosLetivosController.cs`
- Create: `backend/src/SistemaEscolar.Api/Controllers/AnosEscolaresController.cs`

**Interfaces:**
- Consumes: `CriarAnoLetivoCommand`, `AtivarAnoLetivoCommand`, `EncerrarAnoLetivoCommand`, `ObterAnoLetivoPorIdQuery` (Task 3); `CriarAnoEscolarCommand`, `ObterAnoEscolarPorIdQuery`, `NivelEnsino` (Task 2, Task 4).
- Produces: `POST /api/anos-letivos`, `GET /api/anos-letivos/{id}`, `POST /api/anos-letivos/{id}/ativar`, `POST /api/anos-letivos/{id}/encerrar`, `POST /api/anos-escolares`, `GET /api/anos-escolares/{id}` — used by the Swagger validation checklist below.

- [ ] **Step 1: Create `AnosLetivosController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.AnosLetivos.Commands.AtivarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.Commands.CriarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.Commands.EncerrarAnoLetivo;
using SistemaEscolar.Application.AnosLetivos.Queries.ObterAnoLetivoPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/anos-letivos")]
public sealed class AnosLetivosController : ControllerBase
{
    private readonly ISender _sender;

    public AnosLetivosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAnoLetivoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarAnoLetivoCommand(request.Ano, request.DataInicio, request.DataFim);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterAnoLetivoPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }

    [HttpPost("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new AtivarAnoLetivoCommand(id), cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }

    [HttpPost("{id:guid}/encerrar")]
    public async Task<IActionResult> Encerrar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new EncerrarAnoLetivoCommand(id), cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarAnoLetivoRequest(
    int Ano,
    DateOnly DataInicio,
    DateOnly DataFim);
```

- [ ] **Step 2: Create `AnosEscolaresController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.AnosEscolares.Commands.CriarAnoEscolar;
using SistemaEscolar.Application.AnosEscolares.Queries.ObterAnoEscolarPorId;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/anos-escolares")]
public sealed class AnosEscolaresController : ControllerBase
{
    private readonly ISender _sender;

    public AnosEscolaresController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAnoEscolarRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarAnoEscolarCommand(request.Nome, request.NivelEnsino);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterAnoEscolarPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarAnoEscolarRequest(
    string Nome,
    NivelEnsino NivelEnsino);
```

- [ ] **Step 3: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Run the full test suite**

Run (from `backend/`): `dotnet test`
Expected: PASS — all Domain tests (including the pre-existing `AlunoTests`, `TurmaTests`, `MatriculaTests`) plus the new `AnoLetivoTests` (9), `AnoEscolarTests` (2), and `MatricularAlunoCommandHandlerTests` (5).

- [ ] **Step 5: Manual Swagger check**

Run (from `backend/`): `dotnet run --project src/SistemaEscolar.Api`, open `/swagger`, and walk the etapa's validation flow: `POST /api/anos-letivos` → `POST /api/anos-letivos/{id}/ativar` → `POST /api/anos-escolares` → `POST /api/turmas` (existing) → `POST /api/matriculas` (existing, now requires the AnoLetivo from step 1 to be Ativo). Confirm a `POST /api/matriculas` against a non-activated AnoLetivo returns 400 with `"Só é possível matricular em um ano letivo ativo."`.

- [ ] **Step 6: Commit**

```bash
git add backend/src/SistemaEscolar.Api/Controllers/AnosLetivosController.cs backend/src/SistemaEscolar.Api/Controllers/AnosEscolaresController.cs
git commit -m "feat(api): adiciona AnosLetivosController e AnosEscolaresController"
```

---

### Task 8: Manutenção de documentação (CLAUDE.md — obrigatório ao final de cada etapa)

**Files:**
- Modify: `docs/ROTEIRO.md`
- Modify: `docs/DOMAIN.md`
- Modify: `docs/DECISIONS.md`

**Context:** Per the root `CLAUDE.md`'s "Manutenção de documentação" section (synced in Task 0), every completed implementation must update the roteiro checklist, the domain model doc, and register an ADR for any non-trivial architectural decision — done automatically, without asking. This plan made two such decisions (single-active-AnoLetivo rule; real FK constraints in SQL), both already flagged in Global Constraints above.

- [ ] **Step 1: Check off Etapa 1's validation checklist in `docs/ROTEIRO.md`**

In the "Etapa 1" section, change:

```markdown
**Checklist de validação:**
- [ ] `dotnet build` e `dotnet test` passam
- [ ] Migração SQL criada em `infra/supabase/`
- [ ] Endpoint testado no Swagger: criar ano letivo → criar turma → matricular aluno
- [ ] Regra "matrícula duplicada no mesmo ano letivo" tem teste unitário
```

to:

```markdown
**Checklist de validação:**
- [x] `dotnet build` e `dotnet test` passam
- [x] Migração SQL criada em `infra/supabase/`
- [x] Endpoint testado no Swagger: criar ano letivo → criar turma → matricular aluno
- [x] Regra "matrícula duplicada no mesmo ano letivo" tem teste unitário
```

- [ ] **Step 2: Add the new domain events to `docs/DOMAIN.md`**

In the "Eventos de dominio (exemplos)" section, change:

```markdown
## Eventos de dominio (exemplos)
- AlunoMatriculadoEvent
- PresencaRegistradaEvent
- CobrancaVencidaEvent
- PagamentoConfirmadoEvent
```

to:

```markdown
## Eventos de dominio (exemplos)
- AlunoMatriculadoEvent
- PresencaRegistradaEvent
- CobrancaVencidaEvent
- PagamentoConfirmadoEvent
- AnoLetivoCriadoEvent / AnoLetivoAtivadoEvent / AnoLetivoEncerradoEvent
- AnoEscolarCriadoEvent
```

- [ ] **Step 3: Register ADR-004 and ADR-005 in `docs/DECISIONS.md`**

Append to the end of the file:

```markdown

## ADR-004: AnoLetivo — apenas um Ativo por vez
Contexto: múltiplos AnoLetivo podem existir (histórico de anos anteriores), mas o
sistema precisa saber qual é o corrente para validar matrícula, calendário letivo etc.
Decisao: a regra "só um AnoLetivo Ativo por vez" é aplicada na Application
(AtivarAnoLetivoCommandHandler, via IAnoLetivoRepository.ExisteOutroAnoLetivoAtivoAsync),
nunca dentro do agregado AnoLetivo — que só sabe cuidar do próprio estado.
Consequencia: trocar o ano corrente exige encerrar o atual antes de ativar o próximo
(transição explícita em duas chamadas, nunca automática).

## ADR-005: FK real no banco entre turmas/matriculas e anos_letivos/anos_escolares
Contexto: Turma e Matricula referenciam AnoLetivo/AnoEscolar só por Guid no Domain
(DDD — nunca por referência de objeto), mas isso deixava as tabelas do Postgres sem
integridade referencial real.
Decisao: adicionar FOREIGN KEY nas colunas ano_letivo_id/ano_escolar_id de turmas e
matriculas (migração infra/supabase/006_add_fk_turmas_matriculas.sql), aplicada só
no banco — o Domain continua desacoplado, sem referência de objeto entre agregados.
Consequencia: a migração 006 precisa rodar depois de 004 e 005 (ordem importa);
qualquer inserção futura com ano_letivo_id/ano_escolar_id inexistente falha no banco
como defesa em profundidade, além da validação de Application.
```

- [ ] **Step 4: Commit**

```bash
git add docs/ROTEIRO.md docs/DOMAIN.md docs/DECISIONS.md
git commit -m "docs: atualiza roteiro, modelo de dominio e ADRs para AnoLetivo/AnoEscolar"
```

---

## Após todas as tasks

Resuma para o usuário (2-3 linhas) o que foi feito nesta sessão e pergunte se as
atualizações de documentação (Task 0 e Task 8) devem ser commitadas junto com o
código (já estão, neste plano) ou se ele prefere reorganizar os commits — conforme
o item 5 da seção "Manutenção de documentação" do `CLAUDE.md`.
