# Professor, Disciplina & Vínculo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `Professor`, `Disciplina`, and `ProfessorDisciplinaTurma` aggregates (Domain, Application, Infrastructure, Api, unit tests) to complete Etapa 2 of `docs/ROTEIRO.md`, enforcing the etapa's two business rules: a `Disciplina` can only be linked to a `Turma` from the same `AnoEscolar`, and a `Professor` cannot hold two identical active links (same disciplina + turma + ano letivo).

**Architecture:** Clean Architecture / DDD, mirroring the existing `Aluno`/`Turma`/`Matricula`/`AnoLetivo`/`AnoEscolar` aggregates exactly. `Professor` and `Disciplina` are simple aggregate roots with no lifecycle beyond creation (mirroring `AnoEscolar`'s shape — factory only, no state-transition methods, no `Atualizar` on their repositories). `ProfessorDisciplinaTurma` ("Vínculo") is a third aggregate root, referencing `Professor`/`Disciplina`/`Turma`/`AnoLetivo` only by Guid (never by object reference), with a minimal `StatusVinculo` (`Ativo`/`Encerrado`) — this is a deliberate forward-compat addition beyond Etapa 2's literal checklist: `docs/ROTEIRO.md`'s own Etapa 3 text says Aula creation must check for a "vínculo ativo" and explicitly says to "reaproveite o repositório de ProfessorDisciplinaTurma da Etapa 2," so the repository must expose an "ativo" concept now. Both cross-aggregate business rules (AnoEscolar match; duplicate active vínculo) live in the vínculo's Application handler, never inside the aggregates themselves — same pattern as `AtivarAnoLetivoCommandHandler` and `MatricularAlunoCommandHandler`.

**Tech Stack:** .NET 9, EF Core (Npgsql/Postgres), MediatR, FluentValidation, xUnit, FluentAssertions — all already referenced; no new packages needed.

**Spec:** `docs/DOMAIN.md` (entity list, already includes `Professor`/`Disciplina`/`ProfessorDisciplinaTurma`), `docs/ROTEIRO.md` (Etapa 2 — this plan's source requirement, and Etapa 3's forward reference to "vínculo ativo"), `backend/EXEMPLO-ALUNO.md` (reference pattern), `backend/CLAUDE.md` (backend conventions). Executors should read all four before starting.

## Global Constraints

- Domain project references nothing (not even EF Core or other projects) — see `backend/src/SistemaEscolar.Domain/SistemaEscolar.Domain.csproj`.
- Application depends only on Domain; Infrastructure implements the interfaces defined in Domain.
- One aggregate per file. All business rules live in the aggregate (Domain); Application handlers only orchestrate.
- Repositories expose only business-meaningful methods, never generic `IQueryable`.
- Error messages and method names in Portuguese, matching the `Aluno`/`Turma`/`Matricula`/`AnoLetivo`/`AnoEscolar` style.
- Each Application Command defines its own local `Result<T>` (record, same namespace as the Command) — never import `SistemaEscolar.Domain.Common` in the same file that uses that Command-local `Result<T>` explicitly (see `CriarTurmaCommand.cs` as reference: handlers never declare the Domain `Result<T>` type explicitly, only use `var` + property access).
- All commands run from the `backend/` directory (`dotnet build`, `dotnet test`).
- No secrets committed.
- **Confirmed design decision (namespace):** the `ProfessorDisciplinaTurma` aggregate lives under the `SistemaEscolar.Domain.Vinculos` namespace (folder `Vinculos/`), not a namespace matching its own class name — `SistemaEscolar.Domain.ProfessorDisciplinaTurma.ProfessorDisciplinaTurma` would force awkward full-qualification everywhere it's referenced. The class name itself stays `ProfessorDisciplinaTurma`, matching `docs/DOMAIN.md` exactly.
- **Confirmed design decision (StatusVinculo forward-compat):** `ProfessorDisciplinaTurma` carries a `StatusVinculo` (`Ativo=1, Encerrado=2`) and an `Encerrar()` method, plus `IProfessorDisciplinaTurmaRepository.ExisteVinculoAtivoAsync(...)`. This is scoped minimally: one enum, one transition method, one repository query method, one Application command (`EncerrarVinculoCommand`), one thin Api endpoint — the smallest complete, testable, reachable unit. Justification: `docs/ROTEIRO.md`'s Etapa 3 explicitly plans to reuse this repository for an "ativo" check; introducing `Ativo`/`Encerrado` without a way to reach `Encerrado` would be dead code.
- **Confirmed design decision (AnoLetivoId on Vínculo):** `VincularProfessorDisciplinaTurmaCommand` takes `AnoLetivoId` as an explicit, independent input (not derived from the fetched `Turma`), with no cross-check against `Turma.AnoLetivoId`. This exactly mirrors the existing precedent in `MatricularAlunoCommand`/`MatricularAlunoCommandHandler`, which also takes `AnoLetivoId` explicitly without validating it against the target `Turma`'s own `AnoLetivoId`. Kept consistent rather than inventing a new, stricter rule unilaterally.
- **Confirmed design decision (no Professor/Disciplina lifecycle):** neither `Professor` nor `Disciplina` gets a mutable-state method or an `Atualizar` repository method in this plan — `docs/ROTEIRO.md`'s Etapa 2 describes no such rule, and both mirror `AnoEscolar`'s "factory only" shape exactly.
- Cross-context communication is via domain events only, never direct entity references (backend/CLAUDE.md).
- Repositories must work against "pure" PostgreSQL (Supabase today, RDS tomorrow) — no Supabase-specific SQL features.

---

## File Structure

```
backend/src/SistemaEscolar.Domain/Professores/
  Professor.cs            (aggregate root)
  ProfessorEvents.cs       (ProfessorCadastradoEvent)
  IProfessorRepository.cs

backend/src/SistemaEscolar.Domain/Disciplinas/
  Disciplina.cs            (aggregate root)
  DisciplinaEvents.cs      (DisciplinaCriadaEvent)
  IDisciplinaRepository.cs

backend/src/SistemaEscolar.Domain/Vinculos/
  StatusVinculo.cs         (enum)
  VinculoEvents.cs         (VinculoProfessorDisciplinaTurmaCriadoEvent, VinculoProfessorDisciplinaTurmaEncerradoEvent)
  ProfessorDisciplinaTurma.cs   (aggregate root)
  IProfessorDisciplinaTurmaRepository.cs

backend/src/SistemaEscolar.Application/Professores/
  DTOs/ProfessorDto.cs
  Commands/CriarProfessor/{CriarProfessorCommand,CriarProfessorCommandHandler,CriarProfessorCommandValidator}.cs
  Queries/ObterProfessorPorId/{ObterProfessorPorIdQuery,ObterProfessorPorIdQueryHandler}.cs

backend/src/SistemaEscolar.Application/Disciplinas/
  DTOs/DisciplinaDto.cs
  Commands/CriarDisciplina/{CriarDisciplinaCommand,CriarDisciplinaCommandHandler,CriarDisciplinaCommandValidator}.cs
  Queries/ObterDisciplinaPorId/{ObterDisciplinaPorIdQuery,ObterDisciplinaPorIdQueryHandler}.cs

backend/src/SistemaEscolar.Application/Vinculos/
  DTOs/VinculoProfessorDisciplinaTurmaDto.cs
  Commands/VincularProfessorDisciplinaTurma/{VincularProfessorDisciplinaTurmaCommand,VincularProfessorDisciplinaTurmaCommandHandler,VincularProfessorDisciplinaTurmaCommandValidator}.cs
  Commands/EncerrarVinculo/{EncerrarVinculoCommand,EncerrarVinculoCommandHandler}.cs
  Queries/ObterVinculoPorId/{ObterVinculoPorIdQuery,ObterVinculoPorIdQueryHandler}.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/
  ProfessorConfiguration.cs
  DisciplinaConfiguration.cs
  ProfessorDisciplinaTurmaConfiguration.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/
  ProfessorRepository.cs
  DisciplinaRepository.cs
  ProfessorDisciplinaTurmaRepository.cs

backend/src/SistemaEscolar.Infrastructure/Persistence/AppDbContext.cs   (modify: add DbSets)
backend/src/SistemaEscolar.Infrastructure/DependencyInjection.cs        (modify: register repos)

backend/src/SistemaEscolar.Api/Controllers/
  ProfessoresController.cs
  DisciplinasController.cs
  VinculosProfessorDisciplinaTurmaController.cs

backend/tests/SistemaEscolar.UnitTests/Domain/
  ProfessorTests.cs
  DisciplinaTests.cs
  ProfessorDisciplinaTurmaTests.cs

backend/tests/SistemaEscolar.UnitTests/Application/Fakes/
  FakeProfessorRepository.cs
  FakeDisciplinaRepository.cs
  FakeProfessorDisciplinaTurmaRepository.cs
  (FakeTurmaRepository already exists — reused, not recreated)

backend/tests/SistemaEscolar.UnitTests/Application/
  VincularProfessorDisciplinaTurmaCommandHandlerTests.cs

infra/supabase/
  007_create_professores.sql
  008_create_disciplinas.sql
  009_create_vinculos_professor_disciplina_turma.sql

docs/ROTEIRO.md      (modify: check off Etapa 2 items)
docs/DOMAIN.md        (modify: add new domain events)
docs/DECISIONS.md     (modify: add ADR-006, ADR-007)
```

---

### Task 1: Domain — agregado `Professor`

**Files:**
- Create: `backend/src/SistemaEscolar.Domain/Professores/Professor.cs`
- Create: `backend/src/SistemaEscolar.Domain/Professores/ProfessorEvents.cs`
- Create: `backend/src/SistemaEscolar.Domain/Professores/IProfessorRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Domain/ProfessorTests.cs`

**Interfaces:**
- Consumes: `SistemaEscolar.Domain.Common.{AggregateRoot, Result, Result<T>, IDomainEvent}` (already exist); `SistemaEscolar.Domain.SharedKernel.ValueObjects.Email` (already exists — `Email.Criar(string) -> Result<Email>`, `Email.Endereco` property).
- Produces (used by later tasks): `Professor` with public properties `Id, NomeCompleto (string), Email (Email), Formacao (string), CriadoEm (DateTime)`; static factory `Professor.Cadastrar(string nomeCompleto, string emailInformado, string formacao) -> Result<Professor>`; `IProfessorRepository` with `Task<Professor?> ObterPorIdAsync(Guid, CancellationToken)`, `Task AdicionarAsync(Professor, CancellationToken)`. No `Atualizar` — `Professor` has no mutable state after creation in this plan.

- [ ] **Step 1: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Domain/ProfessorTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Domain.Professores;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class ProfessorTests
{
    [Fact]
    public void Cadastrar_ComDadosValidos_DeveCriarProfessor()
    {
        var resultado = Professor.Cadastrar("Carla Mendes", "carla.mendes@escola.com", "Licenciatura em Matemática");

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.NomeCompleto.Should().Be("Carla Mendes");
        resultado.Valor.Email.Endereco.Should().Be("carla.mendes@escola.com");
        resultado.Valor.Formacao.Should().Be("Licenciatura em Matemática");
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is ProfessorCadastradoEvent);
    }

    [Fact]
    public void Cadastrar_SemNome_DeveFalhar()
    {
        var resultado = Professor.Cadastrar("", "carla.mendes@escola.com", "Licenciatura em Matemática");

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome completo é obrigatório.");
    }

    [Fact]
    public void Cadastrar_ComEmailInvalido_DeveFalhar()
    {
        var resultado = Professor.Cadastrar("Carla Mendes", "nao-e-um-email", "Licenciatura em Matemática");

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("E-mail inválido.");
    }

    [Fact]
    public void Cadastrar_SemFormacao_DeveFalhar()
    {
        var resultado = Professor.Cadastrar("Carla Mendes", "carla.mendes@escola.com", "");

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Formação é obrigatória.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `SistemaEscolar.Domain.Professores` namespace / `Professor` type does not exist yet.

- [ ] **Step 3: Create `ProfessorEvents.cs`**

```csharp
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Professores;

/// <summary>
/// Disparado quando um professor é cadastrado.
/// </summary>
public sealed record ProfessorCadastradoEvent(Guid ProfessorId, string NomeCompleto, DateTime OcorridoEm) : IDomainEvent;
```

- [ ] **Step 4: Create `Professor.cs`**

```csharp
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
```

- [ ] **Step 5: Create `IProfessorRepository.cs`**

```csharp
namespace SistemaEscolar.Domain.Professores;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IProfessorRepository
{
    Task<Professor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task AdicionarAsync(Professor professor, CancellationToken cancellationToken);
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~ProfessorTests`
Expected: PASS, 4 tests.

- [ ] **Step 7: Commit**

```bash
git add backend/src/SistemaEscolar.Domain/Professores backend/tests/SistemaEscolar.UnitTests/Domain/ProfessorTests.cs
git commit -m "feat(domain): adiciona agregado Professor"
```

---

### Task 2: Domain — agregado `Disciplina`

**Files:**
- Create: `backend/src/SistemaEscolar.Domain/Disciplinas/Disciplina.cs`
- Create: `backend/src/SistemaEscolar.Domain/Disciplinas/DisciplinaEvents.cs`
- Create: `backend/src/SistemaEscolar.Domain/Disciplinas/IDisciplinaRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Domain/DisciplinaTests.cs`

**Interfaces:**
- Consumes: `SistemaEscolar.Domain.Common.{AggregateRoot, Result, Result<T>, IDomainEvent}`.
- Produces (used by later tasks): `Disciplina` with public properties `Id, Nome (string), CargaHoraria (int), AnoEscolarId (Guid), CriadoEm (DateTime)`; static factory `Disciplina.Criar(string nome, int cargaHoraria, Guid anoEscolarId) -> Result<Disciplina>`; `IDisciplinaRepository` with `Task<Disciplina?> ObterPorIdAsync(Guid, CancellationToken)`, `Task AdicionarAsync(Disciplina, CancellationToken)`. No `Atualizar`.

- [ ] **Step 1: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Domain/DisciplinaTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Domain.Disciplinas;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class DisciplinaTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarDisciplina()
    {
        var anoEscolarId = Guid.NewGuid();

        var resultado = Disciplina.Criar("Matemática", 80, anoEscolarId);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Nome.Should().Be("Matemática");
        resultado.Valor.CargaHoraria.Should().Be(80);
        resultado.Valor.AnoEscolarId.Should().Be(anoEscolarId);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is DisciplinaCriadaEvent);
    }

    [Fact]
    public void Criar_SemNome_DeveFalhar()
    {
        var resultado = Disciplina.Criar("", 80, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome da disciplina é obrigatório.");
    }

    [Fact]
    public void Criar_ComCargaHorariaZeroOuNegativa_DeveFalhar()
    {
        var resultado = Disciplina.Criar("Matemática", 0, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Carga horária deve ser maior que zero.");
    }

    [Fact]
    public void Criar_SemAnoEscolar_DeveFalhar()
    {
        var resultado = Disciplina.Criar("Matemática", 80, Guid.Empty);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Disciplina precisa estar vinculada a um ano escolar.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `SistemaEscolar.Domain.Disciplinas` namespace / `Disciplina` type does not exist yet.

- [ ] **Step 3: Create `DisciplinaEvents.cs`**

```csharp
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Disciplinas;

/// <summary>
/// Disparado quando uma disciplina é criada.
/// </summary>
public sealed record DisciplinaCriadaEvent(Guid DisciplinaId, string Nome, DateTime OcorridoEm) : IDomainEvent;
```

- [ ] **Step 4: Create `Disciplina.cs`**

```csharp
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
```

- [ ] **Step 5: Create `IDisciplinaRepository.cs`**

```csharp
namespace SistemaEscolar.Domain.Disciplinas;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IDisciplinaRepository
{
    Task<Disciplina?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task AdicionarAsync(Disciplina disciplina, CancellationToken cancellationToken);
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~DisciplinaTests`
Expected: PASS, 4 tests.

- [ ] **Step 7: Commit**

```bash
git add backend/src/SistemaEscolar.Domain/Disciplinas backend/tests/SistemaEscolar.UnitTests/Domain/DisciplinaTests.cs
git commit -m "feat(domain): adiciona agregado Disciplina"
```

---

### Task 3: Domain — agregado `ProfessorDisciplinaTurma` (Vínculo)

**Files:**
- Create: `backend/src/SistemaEscolar.Domain/Vinculos/StatusVinculo.cs`
- Create: `backend/src/SistemaEscolar.Domain/Vinculos/VinculoEvents.cs`
- Create: `backend/src/SistemaEscolar.Domain/Vinculos/ProfessorDisciplinaTurma.cs`
- Create: `backend/src/SistemaEscolar.Domain/Vinculos/IProfessorDisciplinaTurmaRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Domain/ProfessorDisciplinaTurmaTests.cs`

**Interfaces:**
- Consumes: `SistemaEscolar.Domain.Common.{AggregateRoot, Result, Result<T>, IDomainEvent}`.
- Produces (used by later tasks): `StatusVinculo` enum (`Ativo=1, Encerrado=2`); `ProfessorDisciplinaTurma` with public properties `Id, ProfessorId (Guid), DisciplinaId (Guid), TurmaId (Guid), AnoLetivoId (Guid), Status (StatusVinculo), CriadoEm (DateTime)`; static factory `ProfessorDisciplinaTurma.Vincular(Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId) -> Result<ProfessorDisciplinaTurma>` (always starts `Ativo`); instance method `Result Encerrar()`; `IProfessorDisciplinaTurmaRepository` with `Task<ProfessorDisciplinaTurma?> ObterPorIdAsync(Guid, CancellationToken)`, `Task<bool> ExisteVinculoAtivoAsync(Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId, CancellationToken)`, `Task AdicionarAsync(ProfessorDisciplinaTurma, CancellationToken)`, `void Atualizar(ProfessorDisciplinaTurma)`.

- [ ] **Step 1: Write the failing test file**

Create `backend/tests/SistemaEscolar.UnitTests/Domain/ProfessorDisciplinaTurmaTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Domain.Vinculos;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class ProfessorDisciplinaTurmaTests
{
    [Fact]
    public void Vincular_ComDadosValidos_DeveCriarVinculoAtivo()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be(StatusVinculo.Ativo);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is VinculoProfessorDisciplinaTurmaCriadoEvent);
    }

    [Fact]
    public void Vincular_SemProfessor_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a um professor.");
    }

    [Fact]
    public void Vincular_SemDisciplina_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a uma disciplina.");
    }

    [Fact]
    public void Vincular_SemTurma_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a uma turma.");
    }

    [Fact]
    public void Vincular_SemAnoLetivo_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a um ano letivo.");
    }

    [Fact]
    public void Encerrar_VinculoAtivo_DeveEncerrarEDispararEvento()
    {
        var vinculo = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = vinculo.Encerrar();

        resultado.Sucesso.Should().BeTrue();
        vinculo.Status.Should().Be(StatusVinculo.Encerrado);
        vinculo.DomainEvents.Should().Contain(e => e is VinculoProfessorDisciplinaTurmaEncerradoEvent);
    }

    [Fact]
    public void Encerrar_VinculoJaEncerrado_DeveFalhar()
    {
        var vinculo = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        vinculo.Encerrar();

        var resultado = vinculo.Encerrar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo já está encerrado.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`
Expected: build error — `SistemaEscolar.Domain.Vinculos` namespace / `ProfessorDisciplinaTurma` type does not exist yet.

- [ ] **Step 3: Create `StatusVinculo.cs`**

```csharp
namespace SistemaEscolar.Domain.Vinculos;

public enum StatusVinculo
{
    Ativo = 1,
    Encerrado = 2
}
```

- [ ] **Step 4: Create `VinculoEvents.cs`**

```csharp
using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Vinculos;

/// <summary>
/// Disparado quando um professor é vinculado a uma disciplina/turma/ano
/// letivo.
/// </summary>
public sealed record VinculoProfessorDisciplinaTurmaCriadoEvent(
    Guid VinculoId, Guid ProfessorId, Guid DisciplinaId, Guid TurmaId, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando um vínculo é encerrado. A Etapa 3 (Aula) consulta o
/// repositório por vínculos ativos antes de permitir o registro de aula.
/// </summary>
public sealed record VinculoProfessorDisciplinaTurmaEncerradoEvent(Guid VinculoId, DateTime OcorridoEm) : IDomainEvent;
```

- [ ] **Step 5: Create `ProfessorDisciplinaTurma.cs`**

```csharp
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
```

- [ ] **Step 6: Create `IProfessorDisciplinaTurmaRepository.cs`**

```csharp
namespace SistemaEscolar.Domain.Vinculos;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IProfessorDisciplinaTurmaRepository
{
    Task<ProfessorDisciplinaTurma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Usado pela Application para reforçar "sem vínculo duplicado" ao
    /// vincular, e reaproveitado pela Etapa 3 para checar se existe vínculo
    /// ativo antes de permitir o registro de uma Aula.
    /// </summary>
    Task<bool> ExisteVinculoAtivoAsync(
        Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId, CancellationToken cancellationToken);

    Task AdicionarAsync(ProfessorDisciplinaTurma vinculo, CancellationToken cancellationToken);
    void Atualizar(ProfessorDisciplinaTurma vinculo);
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~ProfessorDisciplinaTurmaTests`
Expected: PASS, 7 tests.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Domain/Vinculos backend/tests/SistemaEscolar.UnitTests/Domain/ProfessorDisciplinaTurmaTests.cs
git commit -m "feat(domain): adiciona agregado ProfessorDisciplinaTurma com vinculo Ativo/Encerrado"
```

---

### Task 4: Application — casos de uso de `Professor`

**Files:**
- Create: `backend/src/SistemaEscolar.Application/Professores/DTOs/ProfessorDto.cs`
- Create: `backend/src/SistemaEscolar.Application/Professores/Commands/CriarProfessor/CriarProfessorCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/Professores/Commands/CriarProfessor/CriarProfessorCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/Professores/Commands/CriarProfessor/CriarProfessorCommandValidator.cs`
- Create: `backend/src/SistemaEscolar.Application/Professores/Queries/ObterProfessorPorId/ObterProfessorPorIdQuery.cs`
- Create: `backend/src/SistemaEscolar.Application/Professores/Queries/ObterProfessorPorId/ObterProfessorPorIdQueryHandler.cs`

**Interfaces:**
- Consumes: `Professor`, `IProfessorRepository` (Task 1); `SistemaEscolar.Application.Common.IUnitOfWork` (existing).
- Produces (used by Task 7 - Infrastructure DI, Task 8 - Api): `ProfessorDto(Guid Id, string NomeCompleto, string Email, string Formacao)`; `CriarProfessorCommand(string NomeCompleto, string Email, string Formacao) : IRequest<Result<ProfessorDto>>`; `ObterProfessorPorIdQuery(Guid ProfessorId) : IRequest<Result<ProfessorDto>>`. `Result<T>` here is the Application-level record defined in `CriarProfessorCommand.cs` — the query file imports it from there (same pattern as `ObterTurmaPorIdQuery.cs` importing from `CriarTurmaCommand.cs`).

- [ ] **Step 1: Create `ProfessorDto.cs`**

```csharp
namespace SistemaEscolar.Application.Professores.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record ProfessorDto(
    Guid Id,
    string NomeCompleto,
    string Email,
    string Formacao
);
```

- [ ] **Step 2: Create `CriarProfessorCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Professores.DTOs;

namespace SistemaEscolar.Application.Professores.Commands.CriarProfessor;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record CriarProfessorCommand(
    string NomeCompleto,
    string Email,
    string Formacao
) : IRequest<Result<ProfessorDto>>;

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

- [ ] **Step 3: Create `CriarProfessorCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Professores.DTOs;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Application.Professores.Commands.CriarProfessor;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de Professor.cs (Domain).
/// </summary>
public sealed class CriarProfessorCommandHandler
    : IRequestHandler<CriarProfessorCommand, Result<ProfessorDto>>
{
    private readonly IProfessorRepository _professorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarProfessorCommandHandler(IProfessorRepository professorRepository, IUnitOfWork unitOfWork)
    {
        _professorRepository = professorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfessorDto>> Handle(CriarProfessorCommand request, CancellationToken cancellationToken)
    {
        var professorResult = Professor.Cadastrar(request.NomeCompleto, request.Email, request.Formacao);

        if (!professorResult.Sucesso)
            return Result<ProfessorDto>.Falha(professorResult.Erro!);

        var professor = professorResult.Valor!;

        await _professorRepository.AdicionarAsync(professor, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<ProfessorDto>.Ok(new ProfessorDto(
            professor.Id,
            professor.NomeCompleto,
            professor.Email.Endereco,
            professor.Formacao));
    }
}
```

- [ ] **Step 4: Create `CriarProfessorCommandValidator.cs`**

```csharp
using FluentValidation;

namespace SistemaEscolar.Application.Professores.Commands.CriarProfessor;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarProfessorCommandValidator : AbstractValidator<CriarProfessorCommand>
{
    public CriarProfessorCommandValidator()
    {
        RuleFor(c => c.NomeCompleto)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(200);

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.");

        RuleFor(c => c.Formacao)
            .NotEmpty().WithMessage("Formação é obrigatória.")
            .MaximumLength(200);
    }
}
```

- [ ] **Step 5: Create `ObterProfessorPorIdQuery.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Professores.Commands.CriarProfessor;
using SistemaEscolar.Application.Professores.DTOs;

namespace SistemaEscolar.Application.Professores.Queries.ObterProfessorPorId;

public sealed record ObterProfessorPorIdQuery(Guid ProfessorId) : IRequest<Result<ProfessorDto>>;
```

- [ ] **Step 6: Create `ObterProfessorPorIdQueryHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Professores.Commands.CriarProfessor;
using SistemaEscolar.Application.Professores.DTOs;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Application.Professores.Queries.ObterProfessorPorId;

public sealed class ObterProfessorPorIdQueryHandler
    : IRequestHandler<ObterProfessorPorIdQuery, Result<ProfessorDto>>
{
    private readonly IProfessorRepository _professorRepository;

    public ObterProfessorPorIdQueryHandler(IProfessorRepository professorRepository)
    {
        _professorRepository = professorRepository;
    }

    public async Task<Result<ProfessorDto>> Handle(ObterProfessorPorIdQuery request, CancellationToken cancellationToken)
    {
        var professor = await _professorRepository.ObterPorIdAsync(request.ProfessorId, cancellationToken);

        if (professor is null)
            return Result<ProfessorDto>.Falha("Professor não encontrado.");

        return Result<ProfessorDto>.Ok(new ProfessorDto(
            professor.Id,
            professor.NomeCompleto,
            professor.Email.Endereco,
            professor.Formacao));
    }
}
```

- [ ] **Step 7: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Application/Professores
git commit -m "feat(application): adiciona casos de uso CriarProfessor e ObterProfessorPorId"
```

---

### Task 5: Application — casos de uso de `Disciplina`

**Files:**
- Create: `backend/src/SistemaEscolar.Application/Disciplinas/DTOs/DisciplinaDto.cs`
- Create: `backend/src/SistemaEscolar.Application/Disciplinas/Commands/CriarDisciplina/CriarDisciplinaCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/Disciplinas/Commands/CriarDisciplina/CriarDisciplinaCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/Disciplinas/Commands/CriarDisciplina/CriarDisciplinaCommandValidator.cs`
- Create: `backend/src/SistemaEscolar.Application/Disciplinas/Queries/ObterDisciplinaPorId/ObterDisciplinaPorIdQuery.cs`
- Create: `backend/src/SistemaEscolar.Application/Disciplinas/Queries/ObterDisciplinaPorId/ObterDisciplinaPorIdQueryHandler.cs`

**Interfaces:**
- Consumes: `Disciplina`, `IDisciplinaRepository` (Task 2); `IUnitOfWork` (existing).
- Produces (used by Task 6 - Vinculo handler, Task 7 - Infrastructure DI, Task 8 - Api): `DisciplinaDto(Guid Id, string Nome, int CargaHoraria, Guid AnoEscolarId)`; `CriarDisciplinaCommand(string Nome, int CargaHoraria, Guid AnoEscolarId) : IRequest<Result<DisciplinaDto>>`; `ObterDisciplinaPorIdQuery(Guid DisciplinaId) : IRequest<Result<DisciplinaDto>>`. `Result<T>` here is the Application-level record defined in `CriarDisciplinaCommand.cs`.

- [ ] **Step 1: Create `DisciplinaDto.cs`**

```csharp
namespace SistemaEscolar.Application.Disciplinas.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record DisciplinaDto(
    Guid Id,
    string Nome,
    int CargaHoraria,
    Guid AnoEscolarId
);
```

- [ ] **Step 2: Create `CriarDisciplinaCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Disciplinas.DTOs;

namespace SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record CriarDisciplinaCommand(
    string Nome,
    int CargaHoraria,
    Guid AnoEscolarId
) : IRequest<Result<DisciplinaDto>>;

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

- [ ] **Step 3: Create `CriarDisciplinaCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Disciplinas.DTOs;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;

/// <summary>
/// Handler = orquestrador. NÃO contém regra de negócio — apenas: 1) chama a
/// fábrica do agregado, 2) persiste, 3) mapeia para DTO. Toda regra de
/// negócio real está dentro de Disciplina.cs (Domain).
/// </summary>
public sealed class CriarDisciplinaCommandHandler
    : IRequestHandler<CriarDisciplinaCommand, Result<DisciplinaDto>>
{
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarDisciplinaCommandHandler(IDisciplinaRepository disciplinaRepository, IUnitOfWork unitOfWork)
    {
        _disciplinaRepository = disciplinaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DisciplinaDto>> Handle(CriarDisciplinaCommand request, CancellationToken cancellationToken)
    {
        var disciplinaResult = Disciplina.Criar(request.Nome, request.CargaHoraria, request.AnoEscolarId);

        if (!disciplinaResult.Sucesso)
            return Result<DisciplinaDto>.Falha(disciplinaResult.Erro!);

        var disciplina = disciplinaResult.Valor!;

        await _disciplinaRepository.AdicionarAsync(disciplina, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<DisciplinaDto>.Ok(new DisciplinaDto(
            disciplina.Id,
            disciplina.Nome,
            disciplina.CargaHoraria,
            disciplina.AnoEscolarId));
    }
}
```

- [ ] **Step 4: Create `CriarDisciplinaCommandValidator.cs`**

```csharp
using FluentValidation;

namespace SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain, não aqui.
/// </summary>
public sealed class CriarDisciplinaCommandValidator : AbstractValidator<CriarDisciplinaCommand>
{
    public CriarDisciplinaCommandValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome da disciplina é obrigatório.")
            .MaximumLength(100);

        RuleFor(c => c.CargaHoraria)
            .GreaterThan(0).WithMessage("Carga horária deve ser maior que zero.");

        RuleFor(c => c.AnoEscolarId)
            .NotEmpty().WithMessage("Ano escolar é obrigatório.");
    }
}
```

- [ ] **Step 5: Create `ObterDisciplinaPorIdQuery.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;
using SistemaEscolar.Application.Disciplinas.DTOs;

namespace SistemaEscolar.Application.Disciplinas.Queries.ObterDisciplinaPorId;

public sealed record ObterDisciplinaPorIdQuery(Guid DisciplinaId) : IRequest<Result<DisciplinaDto>>;
```

- [ ] **Step 6: Create `ObterDisciplinaPorIdQueryHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;
using SistemaEscolar.Application.Disciplinas.DTOs;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Application.Disciplinas.Queries.ObterDisciplinaPorId;

public sealed class ObterDisciplinaPorIdQueryHandler
    : IRequestHandler<ObterDisciplinaPorIdQuery, Result<DisciplinaDto>>
{
    private readonly IDisciplinaRepository _disciplinaRepository;

    public ObterDisciplinaPorIdQueryHandler(IDisciplinaRepository disciplinaRepository)
    {
        _disciplinaRepository = disciplinaRepository;
    }

    public async Task<Result<DisciplinaDto>> Handle(ObterDisciplinaPorIdQuery request, CancellationToken cancellationToken)
    {
        var disciplina = await _disciplinaRepository.ObterPorIdAsync(request.DisciplinaId, cancellationToken);

        if (disciplina is null)
            return Result<DisciplinaDto>.Falha("Disciplina não encontrada.");

        return Result<DisciplinaDto>.Ok(new DisciplinaDto(
            disciplina.Id,
            disciplina.Nome,
            disciplina.CargaHoraria,
            disciplina.AnoEscolarId));
    }
}
```

- [ ] **Step 7: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add backend/src/SistemaEscolar.Application/Disciplinas
git commit -m "feat(application): adiciona casos de uso CriarDisciplina e ObterDisciplinaPorId"
```

---

### Task 6: Application — casos de uso de `ProfessorDisciplinaTurma` (regras de negócio da etapa) + testes

**Files:**
- Create: `backend/src/SistemaEscolar.Application/Vinculos/DTOs/VinculoProfessorDisciplinaTurmaDto.cs`
- Create: `backend/src/SistemaEscolar.Application/Vinculos/Commands/VincularProfessorDisciplinaTurma/VincularProfessorDisciplinaTurmaCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/Vinculos/Commands/VincularProfessorDisciplinaTurma/VincularProfessorDisciplinaTurmaCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/Vinculos/Commands/VincularProfessorDisciplinaTurma/VincularProfessorDisciplinaTurmaCommandValidator.cs`
- Create: `backend/src/SistemaEscolar.Application/Vinculos/Commands/EncerrarVinculo/EncerrarVinculoCommand.cs`
- Create: `backend/src/SistemaEscolar.Application/Vinculos/Commands/EncerrarVinculo/EncerrarVinculoCommandHandler.cs`
- Create: `backend/src/SistemaEscolar.Application/Vinculos/Queries/ObterVinculoPorId/ObterVinculoPorIdQuery.cs`
- Create: `backend/src/SistemaEscolar.Application/Vinculos/Queries/ObterVinculoPorId/ObterVinculoPorIdQueryHandler.cs`
- Modify: `backend/tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj` — **no change needed**, the `SistemaEscolar.Application` `ProjectReference` was already added in the prior plan's Task 5.
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeProfessorRepository.cs`
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeDisciplinaRepository.cs`
- Create: `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeProfessorDisciplinaTurmaRepository.cs`
- Test: `backend/tests/SistemaEscolar.UnitTests/Application/VincularProfessorDisciplinaTurmaCommandHandlerTests.cs`

**Context:** This is the etapa's two explicit rules — "Disciplina só pode ser vinculada a Turma cujo AnoEscolar bate com o AnoEscolar da Disciplina" and "Um Professor não pode ter dois vínculos idênticos (mesma disciplina + turma + ano letivo)" — both implemented in `VincularProfessorDisciplinaTurmaCommandHandler`. `FakeTurmaRepository` already exists (from the prior plan's Task 5) and is reused here unchanged, since `ITurmaRepository`'s shape is unaffected by this plan.

**Interfaces:**
- Consumes: `ProfessorDisciplinaTurma`, `IProfessorDisciplinaTurmaRepository` (Task 3); `IProfessorRepository` (Task 1); `IDisciplinaRepository` (Task 2); `ITurmaRepository` (existing, unchanged); `IUnitOfWork` (existing). Reuses `FakeTurmaRepository` from `backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeTurmaRepository.cs` (already exists).
- Produces (used by Task 7 - Infrastructure DI, Task 8 - Api): `VinculoProfessorDisciplinaTurmaDto(Guid Id, Guid ProfessorId, Guid DisciplinaId, Guid TurmaId, Guid AnoLetivoId, string Status, DateTime CriadoEm)`; `VincularProfessorDisciplinaTurmaCommand(Guid ProfessorId, Guid DisciplinaId, Guid TurmaId, Guid AnoLetivoId) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>`; `EncerrarVinculoCommand(Guid VinculoId) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>`; `ObterVinculoPorIdQuery(Guid VinculoId) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>`. `Result<T>` here is the Application-level record defined in `VincularProfessorDisciplinaTurmaCommand.cs`.

- [ ] **Step 1: Create `VinculoProfessorDisciplinaTurmaDto.cs`**

```csharp
namespace SistemaEscolar.Application.Vinculos.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record VinculoProfessorDisciplinaTurmaDto(
    Guid Id,
    Guid ProfessorId,
    Guid DisciplinaId,
    Guid TurmaId,
    Guid AnoLetivoId,
    string Status,
    DateTime CriadoEm
);
```

- [ ] **Step 2: Create `VincularProfessorDisciplinaTurmaCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Vinculos.DTOs;

namespace SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record VincularProfessorDisciplinaTurmaCommand(
    Guid ProfessorId,
    Guid DisciplinaId,
    Guid TurmaId,
    Guid AnoLetivoId
) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>;

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

- [ ] **Step 3: Create `VincularProfessorDisciplinaTurmaCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Vinculos.DTOs;
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;

/// <summary>
/// Handler = orquestrador entre os agregados Professor, Disciplina, Turma e
/// ProfessorDisciplinaTurma. As duas regras da etapa exigem consultar mais
/// de um agregado, por isso vivem aqui, não em ProfessorDisciplinaTurma.cs:
/// 1) a Disciplina só pode ser vinculada a uma Turma do mesmo AnoEscolar;
/// 2) o Professor não pode ter dois vínculos idênticos (mesma disciplina,
///    turma e ano letivo) ativos ao mesmo tempo.
/// </summary>
public sealed class VincularProfessorDisciplinaTurmaCommandHandler
    : IRequestHandler<VincularProfessorDisciplinaTurmaCommand, Result<VinculoProfessorDisciplinaTurmaDto>>
{
    private readonly IProfessorRepository _professorRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IProfessorDisciplinaTurmaRepository _vinculoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VincularProfessorDisciplinaTurmaCommandHandler(
        IProfessorRepository professorRepository,
        IDisciplinaRepository disciplinaRepository,
        ITurmaRepository turmaRepository,
        IProfessorDisciplinaTurmaRepository vinculoRepository,
        IUnitOfWork unitOfWork)
    {
        _professorRepository = professorRepository;
        _disciplinaRepository = disciplinaRepository;
        _turmaRepository = turmaRepository;
        _vinculoRepository = vinculoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VinculoProfessorDisciplinaTurmaDto>> Handle(
        VincularProfessorDisciplinaTurmaCommand request, CancellationToken cancellationToken)
    {
        var professor = await _professorRepository.ObterPorIdAsync(request.ProfessorId, cancellationToken);
        if (professor is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Professor não encontrado.");

        var disciplina = await _disciplinaRepository.ObterPorIdAsync(request.DisciplinaId, cancellationToken);
        if (disciplina is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Disciplina não encontrada.");

        var turma = await _turmaRepository.ObterPorIdAsync(request.TurmaId, cancellationToken);
        if (turma is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Turma não encontrada.");

        if (disciplina.AnoEscolarId != turma.AnoEscolarId)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(
                "Disciplina só pode ser vinculada a uma turma do mesmo ano escolar.");

        var existeVinculo = await _vinculoRepository.ExisteVinculoAtivoAsync(
            request.ProfessorId, request.DisciplinaId, request.TurmaId, request.AnoLetivoId, cancellationToken);
        if (existeVinculo)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(
                "Professor já possui um vínculo ativo idêntico (mesma disciplina, turma e ano letivo).");

        var vinculoResult = ProfessorDisciplinaTurma.Vincular(
            request.ProfessorId, request.DisciplinaId, request.TurmaId, request.AnoLetivoId);
        if (!vinculoResult.Sucesso)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(vinculoResult.Erro!);

        var vinculo = vinculoResult.Valor!;

        await _vinculoRepository.AdicionarAsync(vinculo, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<VinculoProfessorDisciplinaTurmaDto>.Ok(new VinculoProfessorDisciplinaTurmaDto(
            vinculo.Id,
            vinculo.ProfessorId,
            vinculo.DisciplinaId,
            vinculo.TurmaId,
            vinculo.AnoLetivoId,
            vinculo.Status.ToString(),
            vinculo.CriadoEm));
    }
}
```

- [ ] **Step 4: Create `VincularProfessorDisciplinaTurmaCommandValidator.cs`**

```csharp
using FluentValidation;

namespace SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;

/// <summary>
/// Validação de FORMATO/entrada. Regra de NEGÓCIO fica no Domain e no
/// handler (orquestração entre agregados).
/// </summary>
public sealed class VincularProfessorDisciplinaTurmaCommandValidator : AbstractValidator<VincularProfessorDisciplinaTurmaCommand>
{
    public VincularProfessorDisciplinaTurmaCommandValidator()
    {
        RuleFor(c => c.ProfessorId)
            .NotEmpty().WithMessage("Professor é obrigatório.");

        RuleFor(c => c.DisciplinaId)
            .NotEmpty().WithMessage("Disciplina é obrigatória.");

        RuleFor(c => c.TurmaId)
            .NotEmpty().WithMessage("Turma é obrigatória.");

        RuleFor(c => c.AnoLetivoId)
            .NotEmpty().WithMessage("Ano letivo é obrigatório.");
    }
}
```

- [ ] **Step 5: Create `EncerrarVinculoCommand.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;

namespace SistemaEscolar.Application.Vinculos.Commands.EncerrarVinculo;

public sealed record EncerrarVinculoCommand(Guid VinculoId) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>;
```

- [ ] **Step 6: Create `EncerrarVinculoCommandHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Common;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Application.Vinculos.Commands.EncerrarVinculo;

public sealed class EncerrarVinculoCommandHandler
    : IRequestHandler<EncerrarVinculoCommand, Result<VinculoProfessorDisciplinaTurmaDto>>
{
    private readonly IProfessorDisciplinaTurmaRepository _vinculoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EncerrarVinculoCommandHandler(IProfessorDisciplinaTurmaRepository vinculoRepository, IUnitOfWork unitOfWork)
    {
        _vinculoRepository = vinculoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VinculoProfessorDisciplinaTurmaDto>> Handle(
        EncerrarVinculoCommand request, CancellationToken cancellationToken)
    {
        var vinculo = await _vinculoRepository.ObterPorIdAsync(request.VinculoId, cancellationToken);
        if (vinculo is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Vínculo não encontrado.");

        var encerrarResult = vinculo.Encerrar();
        if (!encerrarResult.Sucesso)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha(encerrarResult.Erro!);

        _vinculoRepository.Atualizar(vinculo);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Result<VinculoProfessorDisciplinaTurmaDto>.Ok(new VinculoProfessorDisciplinaTurmaDto(
            vinculo.Id,
            vinculo.ProfessorId,
            vinculo.DisciplinaId,
            vinculo.TurmaId,
            vinculo.AnoLetivoId,
            vinculo.Status.ToString(),
            vinculo.CriadoEm));
    }
}
```

- [ ] **Step 7: Create `ObterVinculoPorIdQuery.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;

namespace SistemaEscolar.Application.Vinculos.Queries.ObterVinculoPorId;

public sealed record ObterVinculoPorIdQuery(Guid VinculoId) : IRequest<Result<VinculoProfessorDisciplinaTurmaDto>>;
```

- [ ] **Step 8: Create `ObterVinculoPorIdQueryHandler.cs`**

```csharp
using MediatR;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.DTOs;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Application.Vinculos.Queries.ObterVinculoPorId;

public sealed class ObterVinculoPorIdQueryHandler
    : IRequestHandler<ObterVinculoPorIdQuery, Result<VinculoProfessorDisciplinaTurmaDto>>
{
    private readonly IProfessorDisciplinaTurmaRepository _vinculoRepository;

    public ObterVinculoPorIdQueryHandler(IProfessorDisciplinaTurmaRepository vinculoRepository)
    {
        _vinculoRepository = vinculoRepository;
    }

    public async Task<Result<VinculoProfessorDisciplinaTurmaDto>> Handle(
        ObterVinculoPorIdQuery request, CancellationToken cancellationToken)
    {
        var vinculo = await _vinculoRepository.ObterPorIdAsync(request.VinculoId, cancellationToken);

        if (vinculo is null)
            return Result<VinculoProfessorDisciplinaTurmaDto>.Falha("Vínculo não encontrado.");

        return Result<VinculoProfessorDisciplinaTurmaDto>.Ok(new VinculoProfessorDisciplinaTurmaDto(
            vinculo.Id,
            vinculo.ProfessorId,
            vinculo.DisciplinaId,
            vinculo.TurmaId,
            vinculo.AnoLetivoId,
            vinculo.Status.ToString(),
            vinculo.CriadoEm));
    }
}
```

- [ ] **Step 9: Create `FakeProfessorRepository.cs`**

```csharp
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.UnitTests.Application.Fakes;

/// <summary>
/// Repositório em memória para testar handlers de Application sem banco.
/// Implementa só o suficiente para os cenários testados.
/// </summary>
public sealed class FakeProfessorRepository : IProfessorRepository
{
    private readonly Dictionary<Guid, Professor> _professores = new();

    public void Semear(Professor professor) => _professores[professor.Id] = professor;

    public Task<Professor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_professores.GetValueOrDefault(id));

    public Task AdicionarAsync(Professor professor, CancellationToken cancellationToken)
    {
        _professores[professor.Id] = professor;
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 10: Create `FakeDisciplinaRepository.cs`**

```csharp
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeDisciplinaRepository : IDisciplinaRepository
{
    private readonly Dictionary<Guid, Disciplina> _disciplinas = new();

    public void Semear(Disciplina disciplina) => _disciplinas[disciplina.Id] = disciplina;

    public Task<Disciplina?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_disciplinas.GetValueOrDefault(id));

    public Task AdicionarAsync(Disciplina disciplina, CancellationToken cancellationToken)
    {
        _disciplinas[disciplina.Id] = disciplina;
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 11: Create `FakeProfessorDisciplinaTurmaRepository.cs`**

```csharp
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeProfessorDisciplinaTurmaRepository : IProfessorDisciplinaTurmaRepository
{
    private readonly Dictionary<Guid, ProfessorDisciplinaTurma> _vinculos = new();

    public void Semear(ProfessorDisciplinaTurma vinculo) => _vinculos[vinculo.Id] = vinculo;

    public Task<ProfessorDisciplinaTurma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_vinculos.GetValueOrDefault(id));

    public Task<bool> ExisteVinculoAtivoAsync(
        Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        Task.FromResult(_vinculos.Values.Any(v =>
            v.ProfessorId == professorId
            && v.DisciplinaId == disciplinaId
            && v.TurmaId == turmaId
            && v.AnoLetivoId == anoLetivoId
            && v.Status == StatusVinculo.Ativo));

    public Task AdicionarAsync(ProfessorDisciplinaTurma vinculo, CancellationToken cancellationToken)
    {
        _vinculos[vinculo.Id] = vinculo;
        return Task.CompletedTask;
    }

    public void Atualizar(ProfessorDisciplinaTurma vinculo) => _vinculos[vinculo.Id] = vinculo;
}
```

- [ ] **Step 12: Write the test file**

Create `backend/tests/SistemaEscolar.UnitTests/Application/VincularProfessorDisciplinaTurmaCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.UnitTests.Application.Fakes;
using Xunit;

namespace SistemaEscolar.UnitTests.Application;

public sealed class VincularProfessorDisciplinaTurmaCommandHandlerTests
{
    private readonly FakeProfessorRepository _professorRepository = new();
    private readonly FakeDisciplinaRepository _disciplinaRepository = new();
    private readonly FakeTurmaRepository _turmaRepository = new();
    private readonly FakeProfessorDisciplinaTurmaRepository _vinculoRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly VincularProfessorDisciplinaTurmaCommandHandler _handler;

    public VincularProfessorDisciplinaTurmaCommandHandlerTests()
    {
        _handler = new VincularProfessorDisciplinaTurmaCommandHandler(
            _professorRepository, _disciplinaRepository, _turmaRepository, _vinculoRepository, _unitOfWork);
    }

    private Professor CriarProfessorSemeado()
    {
        var professor = Professor.Cadastrar("Carla Mendes", "carla.mendes@escola.com", "Licenciatura em Matemática").Valor!;
        _professorRepository.Semear(professor);
        return professor;
    }

    private Disciplina CriarDisciplinaSemeada(Guid anoEscolarId)
    {
        var disciplina = Disciplina.Criar("Matemática", 80, anoEscolarId).Valor!;
        _disciplinaRepository.Semear(disciplina);
        return disciplina;
    }

    private Turma CriarTurmaSemeada(Guid anoLetivoId, Guid anoEscolarId)
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, anoLetivoId, anoEscolarId, 30).Valor!;
        _turmaRepository.Semear(turma);
        return turma;
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveVincular()
    {
        var anoEscolarId = Guid.NewGuid();
        var anoLetivoId = Guid.NewGuid();
        var professor = CriarProfessorSemeado();
        var disciplina = CriarDisciplinaSemeada(anoEscolarId);
        var turma = CriarTurmaSemeada(anoLetivoId, anoEscolarId);

        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be("Ativo");
    }

    [Fact]
    public async Task Handle_ComAnoEscolarDivergenteEntreDisciplinaETurma_DeveFalhar()
    {
        var anoLetivoId = Guid.NewGuid();
        var professor = CriarProfessorSemeado();
        var disciplina = CriarDisciplinaSemeada(Guid.NewGuid());
        var turma = CriarTurmaSemeada(anoLetivoId, Guid.NewGuid());

        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Disciplina só pode ser vinculada a uma turma do mesmo ano escolar.");
    }

    [Fact]
    public async Task Handle_ComVinculoIdenticoJaAtivo_DeveFalhar()
    {
        var anoEscolarId = Guid.NewGuid();
        var anoLetivoId = Guid.NewGuid();
        var professor = CriarProfessorSemeado();
        var disciplina = CriarDisciplinaSemeada(anoEscolarId);
        var turma = CriarTurmaSemeada(anoLetivoId, anoEscolarId);

        await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);
        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Professor já possui um vínculo ativo idêntico (mesma disciplina, turma e ano letivo).");
    }

    [Fact]
    public async Task Handle_ComProfessorInexistente_DeveFalhar()
    {
        var anoEscolarId = Guid.NewGuid();
        var anoLetivoId = Guid.NewGuid();
        var disciplina = CriarDisciplinaSemeada(anoEscolarId);
        var turma = CriarTurmaSemeada(anoLetivoId, anoEscolarId);

        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(Guid.NewGuid(), disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Professor não encontrado.");
    }
}
```

- [ ] **Step 13: Run tests to verify they pass**

Run (from `backend/`): `dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj --filter FullyQualifiedName~VincularProfessorDisciplinaTurmaCommandHandlerTests`
Expected: PASS, 4 tests. Then run the full suite (`dotnet test tests/SistemaEscolar.UnitTests/SistemaEscolar.UnitTests.csproj`) to confirm nothing else broke.

- [ ] **Step 14: Commit**

```bash
git add backend/src/SistemaEscolar.Application/Vinculos backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeProfessorRepository.cs backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeDisciplinaRepository.cs backend/tests/SistemaEscolar.UnitTests/Application/Fakes/FakeProfessorDisciplinaTurmaRepository.cs backend/tests/SistemaEscolar.UnitTests/Application/VincularProfessorDisciplinaTurmaCommandHandlerTests.cs
git commit -m "feat(application): adiciona VincularProfessorDisciplinaTurma, EncerrarVinculo e ObterVinculoPorId com regras de negocio da etapa"
```

---

### Task 7: Infrastructure — persistência (EF Core + SQL) para `Professor`, `Disciplina` e `ProfessorDisciplinaTurma`

**Files:**
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/ProfessorConfiguration.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/DisciplinaConfiguration.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Configurations/ProfessorDisciplinaTurmaConfiguration.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/ProfessorRepository.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/DisciplinaRepository.cs`
- Create: `backend/src/SistemaEscolar.Infrastructure/Persistence/Repositories/ProfessorDisciplinaTurmaRepository.cs`
- Modify: `backend/src/SistemaEscolar.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/src/SistemaEscolar.Infrastructure/DependencyInjection.cs`
- Create: `infra/supabase/007_create_professores.sql`
- Create: `infra/supabase/008_create_disciplinas.sql`
- Create: `infra/supabase/009_create_vinculos_professor_disciplina_turma.sql`

**Interfaces:**
- Consumes: `Professor`, `IProfessorRepository` (Task 1); `Disciplina`, `IDisciplinaRepository` (Task 2); `ProfessorDisciplinaTurma`, `StatusVinculo`, `IProfessorDisciplinaTurmaRepository` (Task 3); existing `AppDbContext`, `DependencyInjection.AdicionarInfrastructure`.
- Produces (used by Task 8 - Api): `IProfessorRepository`, `IDisciplinaRepository`, `IProfessorDisciplinaTurmaRepository` resolvable from the DI container; `AppDbContext.Professores`, `AppDbContext.Disciplinas`, `AppDbContext.VinculosProfessorDisciplinaTurma` DbSets.

- [ ] **Step 1: Create `ProfessorConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class ProfessorConfiguration : IEntityTypeConfiguration<Professor>
{
    public void Configure(EntityTypeBuilder<Professor> builder)
    {
        builder.ToTable("professores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.NomeCompleto)
            .HasColumnName("nome_completo")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Formacao)
            .HasColumnName("formacao")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        // Email é Value Object obrigatório -> mapeado como owned type em coluna própria.
        builder.OwnsOne(p => p.Email, emailBuilder =>
        {
            emailBuilder.Property(e => e.Endereco)
                .HasColumnName("email")
                .HasMaxLength(200)
                .IsRequired();
        });
    }
}
```

- [ ] **Step 2: Create `DisciplinaConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class DisciplinaConfiguration : IEntityTypeConfiguration<Disciplina>
{
    public void Configure(EntityTypeBuilder<Disciplina> builder)
    {
        builder.ToTable("disciplinas");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.CargaHoraria)
            .HasColumnName("carga_horaria")
            .IsRequired();

        builder.Property(d => d.AnoEscolarId)
            .HasColumnName("ano_escolar_id")
            .IsRequired();

        builder.Property(d => d.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasIndex(d => d.AnoEscolarId);
    }
}
```

- [ ] **Step 3: Create `ProfessorDisciplinaTurmaConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class ProfessorDisciplinaTurmaConfiguration : IEntityTypeConfiguration<ProfessorDisciplinaTurma>
{
    public void Configure(EntityTypeBuilder<ProfessorDisciplinaTurma> builder)
    {
        builder.ToTable("professores_disciplinas_turmas");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProfessorId)
            .HasColumnName("professor_id")
            .IsRequired();

        builder.Property(v => v.DisciplinaId)
            .HasColumnName("disciplina_id")
            .IsRequired();

        builder.Property(v => v.TurmaId)
            .HasColumnName("turma_id")
            .IsRequired();

        builder.Property(v => v.AnoLetivoId)
            .HasColumnName("ano_letivo_id")
            .IsRequired();

        builder.Property(v => v.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        // Defesa em profundidade: mesma checagem que ExisteVinculoAtivoAsync
        // usa para impedir vínculo duplicado. Não é UNIQUE puro porque um
        // vínculo Encerrado permite recriar o mesmo vínculo depois — por
        // isso o índice não é aplicado no banco (ficaria inconsistente com
        // a regra de negócio "duplicado" = mesma combinação ainda Ativa);
        // um índice normal (não único) basta para acelerar a consulta.
        builder.HasIndex(v => new { v.ProfessorId, v.DisciplinaId, v.TurmaId, v.AnoLetivoId });
    }
}
```

- [ ] **Step 4: Create `ProfessorRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IProfessorRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class ProfessorRepository : IProfessorRepository
{
    private readonly AppDbContext _context;

    public ProfessorRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Professor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Professores
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AdicionarAsync(Professor professor, CancellationToken cancellationToken) =>
        await _context.Professores.AddAsync(professor, cancellationToken);
}
```

- [ ] **Step 5: Create `DisciplinaRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IDisciplinaRepository (definida no Domain).
/// Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class DisciplinaRepository : IDisciplinaRepository
{
    private readonly AppDbContext _context;

    public DisciplinaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Disciplina?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Disciplinas
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AdicionarAsync(Disciplina disciplina, CancellationToken cancellationToken) =>
        await _context.Disciplinas.AddAsync(disciplina, cancellationToken);
}
```

- [ ] **Step 6: Create `ProfessorDisciplinaTurmaRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de IProfessorDisciplinaTurmaRepository (definida
/// no Domain). Só esta camada conhece EF Core / detalhes do Postgres.
/// </summary>
public sealed class ProfessorDisciplinaTurmaRepository : IProfessorDisciplinaTurmaRepository
{
    private readonly AppDbContext _context;

    public ProfessorDisciplinaTurmaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ProfessorDisciplinaTurma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.VinculosProfessorDisciplinaTurma
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<bool> ExisteVinculoAtivoAsync(
        Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        _context.VinculosProfessorDisciplinaTurma
            .AnyAsync(v =>
                v.ProfessorId == professorId
                && v.DisciplinaId == disciplinaId
                && v.TurmaId == turmaId
                && v.AnoLetivoId == anoLetivoId
                && v.Status == StatusVinculo.Ativo,
                cancellationToken);

    public async Task AdicionarAsync(ProfessorDisciplinaTurma vinculo, CancellationToken cancellationToken) =>
        await _context.VinculosProfessorDisciplinaTurma.AddAsync(vinculo, cancellationToken);

    public void Atualizar(ProfessorDisciplinaTurma vinculo) =>
        _context.VinculosProfessorDisciplinaTurma.Update(vinculo);
}
```

- [ ] **Step 7: Modify `AppDbContext.cs`**

Add these three usings alongside the existing ones at the top:

```csharp
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Vinculos;
```

Add these three DbSet properties alongside the existing `Alunos`/`Turmas`/`Matriculas`/`AnosLetivos`/`AnosEscolares` ones:

```csharp
    public DbSet<Professor> Professores => Set<Professor>();
    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<ProfessorDisciplinaTurma> VinculosProfessorDisciplinaTurma => Set<ProfessorDisciplinaTurma>();
```

- [ ] **Step 8: Modify `DependencyInjection.cs`**

Add these three usings alongside the existing `SistemaEscolar.Domain.*` ones:

```csharp
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Vinculos;
```

Add these three registrations alongside the existing `AddScoped<I...Repository, ...Repository>()` calls:

```csharp
        services.AddScoped<IProfessorRepository, ProfessorRepository>();
        services.AddScoped<IDisciplinaRepository, DisciplinaRepository>();
        services.AddScoped<IProfessorDisciplinaTurmaRepository, ProfessorDisciplinaTurmaRepository>();
```

- [ ] **Step 9: Create `007_create_professores.sql`**

```sql
-- Migração: tabela "professores" (agregado Professor)
-- Compatível com o mapeamento EF Core em ProfessorConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 006_add_fk_turmas_matriculas.sql.

create table if not exists professores (
    id uuid primary key,
    nome_completo varchar(200) not null,
    email varchar(200) not null,
    formacao varchar(200) not null,
    criado_em timestamp not null default now()
);

comment on table professores is 'Agregado raiz Professor (bounded context Acadêmico).';
```

- [ ] **Step 10: Create `008_create_disciplinas.sql`**

```sql
-- Migração: tabela "disciplinas" (agregado Disciplina)
-- Compatível com o mapeamento EF Core em DisciplinaConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 007_create_professores.sql.

create table if not exists disciplinas (
    id uuid primary key,
    nome varchar(100) not null,
    carga_horaria integer not null,
    ano_escolar_id uuid not null,
    criado_em timestamp not null default now(),
    constraint fk_disciplinas_ano_escolar foreign key (ano_escolar_id) references anos_escolares (id)
);

create index if not exists ix_disciplinas_ano_escolar_id on disciplinas (ano_escolar_id);

comment on table disciplinas is 'Agregado raiz Disciplina (bounded context Acadêmico).';
```

- [ ] **Step 11: Create `009_create_vinculos_professor_disciplina_turma.sql`**

```sql
-- Migração: tabela "professores_disciplinas_turmas" (agregado ProfessorDisciplinaTurma)
-- Compatível com o mapeamento EF Core em ProfessorDisciplinaTurmaConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 008_create_disciplinas.sql.

create table if not exists professores_disciplinas_turmas (
    id uuid primary key,
    professor_id uuid not null,
    disciplina_id uuid not null,
    turma_id uuid not null,
    ano_letivo_id uuid not null,
    status varchar(20) not null default 'Ativo',
    criado_em timestamp not null default now(),
    constraint fk_pdt_professor foreign key (professor_id) references professores (id),
    constraint fk_pdt_disciplina foreign key (disciplina_id) references disciplinas (id),
    constraint fk_pdt_turma foreign key (turma_id) references turmas (id),
    constraint fk_pdt_ano_letivo foreign key (ano_letivo_id) references anos_letivos (id)
);

create index if not exists ix_pdt_professor_disciplina_turma_ano
    on professores_disciplinas_turmas (professor_id, disciplina_id, turma_id, ano_letivo_id);

comment on table professores_disciplinas_turmas is 'Agregado raiz ProfessorDisciplinaTurma / vínculo (bounded context Acadêmico).';
comment on column professores_disciplinas_turmas.status is
    'Ciclo Ativo -> Encerrado. Regra "sem vínculo ativo duplicado" é reforçada na Application, não no banco (índice acima não é UNIQUE por isso).';
```

- [ ] **Step 12: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 13: Commit**

```bash
git add backend/src/SistemaEscolar.Infrastructure infra/supabase/007_create_professores.sql infra/supabase/008_create_disciplinas.sql infra/supabase/009_create_vinculos_professor_disciplina_turma.sql
git commit -m "feat(infrastructure): mapeamento EF Core, repositorios, DI e migracoes SQL para Professor, Disciplina e ProfessorDisciplinaTurma"
```

---

### Task 8: Api — `ProfessoresController`, `DisciplinasController`, `VinculosProfessorDisciplinaTurmaController`

**Files:**
- Create: `backend/src/SistemaEscolar.Api/Controllers/ProfessoresController.cs`
- Create: `backend/src/SistemaEscolar.Api/Controllers/DisciplinasController.cs`
- Create: `backend/src/SistemaEscolar.Api/Controllers/VinculosProfessorDisciplinaTurmaController.cs`

**Interfaces:**
- Consumes: `CriarProfessorCommand`, `ObterProfessorPorIdQuery` (Task 4); `CriarDisciplinaCommand`, `ObterDisciplinaPorIdQuery` (Task 5); `VincularProfessorDisciplinaTurmaCommand`, `EncerrarVinculoCommand`, `ObterVinculoPorIdQuery` (Task 6).
- Produces: `POST /api/professores`, `GET /api/professores/{id}`, `POST /api/disciplinas`, `GET /api/disciplinas/{id}`, `POST /api/professores-disciplinas-turmas`, `GET /api/professores-disciplinas-turmas/{id}`, `POST /api/professores-disciplinas-turmas/{id}/encerrar` — used by the Swagger validation checklist below.

- [ ] **Step 1: Create `ProfessoresController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Professores.Commands.CriarProfessor;
using SistemaEscolar.Application.Professores.Queries.ObterProfessorPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/professores")]
public sealed class ProfessoresController : ControllerBase
{
    private readonly ISender _sender;

    public ProfessoresController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarProfessorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarProfessorCommand(request.NomeCompleto, request.Email, request.Formacao);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterProfessorPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarProfessorRequest(
    string NomeCompleto,
    string Email,
    string Formacao);
```

- [ ] **Step 2: Create `DisciplinasController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;
using SistemaEscolar.Application.Disciplinas.Queries.ObterDisciplinaPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/disciplinas")]
public sealed class DisciplinasController : ControllerBase
{
    private readonly ISender _sender;

    public DisciplinasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarDisciplinaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CriarDisciplinaCommand(request.Nome, request.CargaHoraria, request.AnoEscolarId);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterDisciplinaPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record CriarDisciplinaRequest(
    string Nome,
    int CargaHoraria,
    Guid AnoEscolarId);
```

- [ ] **Step 3: Create `VinculosProfessorDisciplinaTurmaController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolar.Application.Vinculos.Commands.EncerrarVinculo;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Application.Vinculos.Queries.ObterVinculoPorId;

namespace SistemaEscolar.Api.Controllers;

/// <summary>
/// Controller é uma camada "burra": só traduz HTTP <-> comando/query do
/// MediatR. Nenhuma regra de negócio ou acesso a dados aqui.
/// </summary>
[ApiController]
[Route("api/professores-disciplinas-turmas")]
public sealed class VinculosProfessorDisciplinaTurmaController : ControllerBase
{
    private readonly ISender _sender;

    public VinculosProfessorDisciplinaTurmaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Vincular(
        [FromBody] VincularProfessorDisciplinaTurmaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new VincularProfessorDisciplinaTurmaCommand(
            request.ProfessorId, request.DisciplinaId, request.TurmaId, request.AnoLetivoId);

        var resultado = await _sender.Send(command, cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new ObterVinculoPorIdQuery(id), cancellationToken);

        if (!resultado.Sucesso)
            return NotFound(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }

    [HttpPost("{id:guid}/encerrar")]
    public async Task<IActionResult> Encerrar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new EncerrarVinculoCommand(id), cancellationToken);

        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        return Ok(resultado.Valor);
    }
}

public sealed record VincularProfessorDisciplinaTurmaRequest(
    Guid ProfessorId,
    Guid DisciplinaId,
    Guid TurmaId,
    Guid AnoLetivoId);
```

- [ ] **Step 4: Build to verify it compiles**

Run (from `backend/`): `dotnet build`
Expected: Build succeeded.

- [ ] **Step 5: Run the full test suite**

Run (from `backend/`): `dotnet test`
Expected: PASS — all pre-existing tests plus the new `ProfessorTests` (4), `DisciplinaTests` (4), `ProfessorDisciplinaTurmaTests` (7), and `VincularProfessorDisciplinaTurmaCommandHandlerTests` (4).

- [ ] **Step 6: Manual Swagger check (or HTTP flow, if no browser available)**

Run (from `backend/`): `dotnet run --project src/SistemaEscolar.Api`, open `/swagger`, and walk the etapa's validation flow: `POST /api/professores` → `POST /api/disciplinas` (using an `AnoEscolarId` created via `POST /api/anos-escolares`) → `POST /api/professores-disciplinas-turmas` against a `Turma` from Etapa 1 with the same `AnoEscolarId` → confirm success. Then confirm a `POST /api/professores-disciplinas-turmas` against a `Turma` with a *different* `AnoEscolarId` than the `Disciplina` returns 400 with `"Disciplina só pode ser vinculada a uma turma do mesmo ano escolar."`. If no live Supabase/Postgres connection is available in this environment, note that as a known limitation (mirroring how Etapa 1's Task 7 handled the same gap) rather than blocking the task.

- [ ] **Step 7: Commit**

```bash
git add backend/src/SistemaEscolar.Api/Controllers/ProfessoresController.cs backend/src/SistemaEscolar.Api/Controllers/DisciplinasController.cs backend/src/SistemaEscolar.Api/Controllers/VinculosProfessorDisciplinaTurmaController.cs
git commit -m "feat(api): adiciona ProfessoresController, DisciplinasController e VinculosProfessorDisciplinaTurmaController"
```

---

### Task 9: Manutenção de documentação (CLAUDE.md — obrigatório ao final de cada etapa)

**Files:**
- Modify: `docs/ROTEIRO.md`
- Modify: `docs/DOMAIN.md`
- Modify: `docs/DECISIONS.md`

**Context:** Per the root `CLAUDE.md`'s "Manutenção de documentação" section, every completed implementation must update the roteiro checklist, the domain model doc, and register an ADR for any non-trivial architectural decision — done automatically, without asking. This plan made three such decisions, all already flagged in Global Constraints above: the `Vinculos` namespace choice, the `StatusVinculo` forward-compat addition, and reusing the `MatricularAlunoCommand` precedent for `AnoLetivoId`.

- [ ] **Step 1: Check off Etapa 2's validation checklist in `docs/ROTEIRO.md`**

In the "Etapa 2" section, change:

```markdown
**Checklist de validação:**
- [ ] Testes cobrindo a regra de compatibilidade AnoEscolar
- [ ] Swagger: criar professor → criar disciplina → vincular a uma turma da Etapa 1
```

to (adjust the second line based on whether Task 8's Step 6 actually reached a live database in this environment — if it did not, leave that item unchecked with a short note, exactly as Etapa 1's Task 8 handled its own deferred Swagger check; if it did, check it as `[x]`):

```markdown
**Checklist de validação:**
- [x] Testes cobrindo a regra de compatibilidade AnoEscolar
- [ ] Swagger: criar professor → criar disciplina → vincular a uma turma da Etapa 1 (código pronto e testado via build/testes automatizados; fluxo manual ainda não exercitado neste ambiente por falta de conexão real com Supabase/Postgres)
```

- [ ] **Step 2: Add the new domain events to `docs/DOMAIN.md`**

In the "Eventos de dominio (exemplos)" section, append:

```markdown
- ProfessorCadastradoEvent
- DisciplinaCriadaEvent
- VinculoProfessorDisciplinaTurmaCriadoEvent / VinculoProfessorDisciplinaTurmaEncerradoEvent
```

- [ ] **Step 3: Register ADR-006 and ADR-007 in `docs/DECISIONS.md`**

Append to the end of the file:

```markdown

## ADR-006: ProfessorDisciplinaTurma ganha StatusVinculo (Ativo/Encerrado) já na Etapa 2
Contexto: a Etapa 2 original só pedia o vínculo em si, sem menção a estado.
Mas a Etapa 3 do roteiro (Aula) já presume "vínculo ativo" e planeja reaproveitar
o repositório de ProfessorDisciplinaTurma desta etapa.
Decisao: adicionar StatusVinculo (Ativo/Encerrado), o método Encerrar() e
IProfessorDisciplinaTurmaRepository.ExisteVinculoAtivoAsync já nesta etapa, em vez
de forçar a Etapa 3 a alterar retroativamente este agregado.
Consequencia: um vínculo Encerrado pode ser recriado (nova linha, novo Id) para a
mesma combinação professor/disciplina/turma/ano-letivo — por isso o índice no banco
não é UNIQUE, só a checagem em Application impede duplicidade entre vínculos Ativos.

## ADR-007: AnoLetivoId do vínculo não é validado contra o AnoLetivoId da Turma
Contexto: VincularProfessorDisciplinaTurmaCommand recebe AnoLetivoId como campo
próprio, assim como Turma também tem seu próprio AnoLetivoId.
Decisao: não cruzar os dois valores no handler — mesma decisão já tomada
(sem comentário até agora) em MatricularAlunoCommandHandler, que também recebe
AnoLetivoId sem validá-lo contra o AnoLetivoId da Turma. Mantido consistente em
vez de introduzir uma regra mais rígida unilateralmente nesta etapa.
Consequencia: um cliente da API pode, em tese, enviar um AnoLetivoId que não bate
com o da Turma informada; isso já era possível em MatricularAluno e não foi
sinalizado como bug até agora — registrado aqui para que uma etapa futura decida
se vale a pena adicionar a checagem nos dois handlers de uma vez.
```

- [ ] **Step 4: Commit**

```bash
git add docs/ROTEIRO.md docs/DOMAIN.md docs/DECISIONS.md
git commit -m "docs: atualiza roteiro, modelo de dominio e ADRs para Professor/Disciplina/Vinculo"
```

---

## Após todas as tasks

Resuma para o usuário (2-3 linhas) o que foi feito nesta sessão e pergunte se as
atualizações de documentação (Task 9) devem ser commitadas junto com o código
(já estão, neste plano) ou se ele prefere reorganizar os commits — conforme o
item 5 da seção "Manutenção de documentação" do `CLAUDE.md`.
