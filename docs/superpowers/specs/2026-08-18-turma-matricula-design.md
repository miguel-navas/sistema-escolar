# Design: Agregados `Turma` e `Matricula`

Data: 2026-08-18
Contexto: Academico (ver `backend/CLAUDE.md`)
Referencia de padrao: `backend/EXEMPLO-ALUNO.md` (agregado `Aluno`)

## Objetivo

Implementar os agregados `Turma` e `Matricula`, seguindo exatamente a mesma
estrutura de camadas (Domain, Application, Infrastructure, Api, testes)
usada no agregado `Aluno`, incluindo a regra de negocio de controle de vagas
com abertura automatica de turma quando a turma atual lota.

## Decisoes tomadas

| Decisao | Escolha | Motivo |
|---|---|---|
| Controle de vagas em Turma | Sim, campo `VagasMaximas` + contador `VagasOcupadas` | Regra de negocio real, pedida explicitamente pelo usuario |
| Turma lotada + nova matricula | Abre turma irma automaticamente dentro do proprio caso de uso de matricula | Pedido explicito do usuario: fluxo transparente, sem passo manual |
| Identificacao da turma irma | Sufixo incremental (`NomeBase` + `Sufixo` char: "3o Ano A" -> "3o Ano B") | Convencao escolar comum; permite localizar o grupo de turmas por `NomeBase + Turno + AnoLetivoId + AnoEscolarId` |
| Status de Matricula | `Ativa, Trancada, Cancelada, Concluida` | Espelha `StatusAluno` (ja tem `Trancado`); cobre licenca temporaria, desistencia e fim de ano letivo |
| Matricula unica por ano letivo | Validada no caso de uso (`ExisteMatriculaAtivaAsync`) | Mesmo padrao do CPF unico em `CriarAlunoCommandHandler` |

## Agregado `Turma`

`Domain/Turmas/Turma.cs`

Campos:
- `AnoLetivoId` (Guid)
- `AnoEscolarId` (Guid)
- `NomeBase` (string) — ex: "3o Ano"
- `Sufixo` (char) — ex: 'A', 'B'...
- `Turno` (enum `TurnoTurma`: `Manha=1, Tarde=2, Noite=3, Integral=4`)
- `VagasMaximas` (int)
- `VagasOcupadas` (int)
- `CriadoEm` (DateTime)
- `Nome` — propriedade computada, nao persistida: `$"{NomeBase} {Sufixo}"`

Fabrica e comportamento:
- `Turma.Criar(nomeBase, turno, anoLetivoId, anoEscolarId, vagasMaximas)` -> `Result<Turma>`
  - Valida: `nomeBase` obrigatorio, `anoLetivoId`/`anoEscolarId` != `Guid.Empty`, `vagasMaximas > 0`.
  - Sempre cria com `Sufixo = 'A'`, `VagasOcupadas = 0`.
  - Dispara `TurmaCriadaEvent`.
- `AbrirTurmaIrma()` -> `Result<Turma>`
  - Cria nova `Turma` com mesmo `NomeBase`/`Turno`/`AnoLetivoId`/`AnoEscolarId`/`VagasMaximas`, `Sufixo` incrementado (`(char)(Sufixo + 1)`).
  - Falha se `Sufixo` atual já for `'Z'` ("Limite de turmas irmas atingido para este grupo.").
  - Dispara `TurmaCriadaEvent` (mesma semantica de "turma criada", independente do gatilho).
- `OcuparVaga()` -> `Result`
  - Falha se `VagasOcupadas >= VagasMaximas` ("Turma esta lotada.").
  - Incrementa `VagasOcupadas`; dispara `TurmaLotadaEvent` se, apos incrementar, `VagasOcupadas == VagasMaximas`.
- `LiberarVaga()` -> `Result`
  - Decrementa `VagasOcupadas` (nunca abaixo de 0). Chamado quando uma `Matricula` e cancelada/trancada.
- `PodeReceberNovaMatricula()` -> `bool` => `VagasOcupadas < VagasMaximas`.

`Domain/Turmas/TurmaEvents.cs`:
- `TurmaCriadaEvent(TurmaId, Nome, OcorridoEm)`
- `TurmaLotadaEvent(TurmaId, OcorridoEm)`

`Domain/Turmas/ITurmaRepository.cs`:
- `Task<Turma?> ObterPorIdAsync(Guid id, CancellationToken)`
- `Task<List<Turma>> ObterTurmasDoGrupoAsync(string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken)` — ordenado por `Sufixo` ascendente
- `Task AdicionarAsync(Turma turma, CancellationToken)`
- `void Atualizar(Turma turma)`

## Agregado `Matricula`

`Domain/Matriculas/Matricula.cs`

Campos:
- `AlunoId` (Guid)
- `TurmaId` (Guid)
- `AnoLetivoId` (Guid)
- `Status` (enum `StatusMatricula`: `Ativa=1, Trancada=2, Cancelada=3, Concluida=4`)
- `MatriculadoEm` (DateTime)

Fabrica e comportamento:
- `Matricula.Matricular(alunoId, turmaId, anoLetivoId)` -> `Result<Matricula>`
  - Valida: `alunoId`/`turmaId`/`anoLetivoId` != `Guid.Empty`.
  - `Status = Ativa`. Dispara `AlunoMatriculadoEvent(MatriculaId, AlunoId, TurmaId, OcorridoEm)` (evento ja citado em `docs/DOMAIN.md`; sera consumido futuramente pelo Financeiro para gerar cobranca de matricula).
- `Trancar()` -> `Result` — permitido apenas a partir de `Ativa`.
- `Cancelar()` -> `Result` — permitido a partir de `Ativa` ou `Trancada`; falha se `Concluida`.
- `Concluir()` -> `Result` — permitido apenas a partir de `Ativa`.

`Domain/Matriculas/MatriculaEvents.cs`:
- `AlunoMatriculadoEvent(MatriculaId, AlunoId, TurmaId, OcorridoEm)`

`Domain/Matriculas/IMatriculaRepository.cs`:
- `Task<Matricula?> ObterPorIdAsync(Guid id, CancellationToken)`
- `Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken)`
- `Task AdicionarAsync(Matricula matricula, CancellationToken)`
- `void Atualizar(Matricula matricula)`

## Caso de uso `MatricularAluno` (orquestracao entre agregados)

`Application/Matriculas/Commands/MatricularAluno/MatricularAlunoCommandHandler.cs`

Entrada: `MatricularAlunoCommand(AlunoId, TurmaId, AnoLetivoId)`.

Fluxo:
1. Verifica que o aluno existe (`IAlunoRepository.ObterPorIdAsync`) — falha se nao encontrado.
2. Verifica matricula unica: `IMatriculaRepository.ExisteMatriculaAtivaAsync(alunoId, anoLetivoId)` — falha se ja existir.
3. Carrega a `Turma` pelo `TurmaId` informado — falha se nao encontrada.
4. Se `turma.PodeReceberNovaMatricula()`, usa essa turma como alvo.
5. Senao, busca `ITurmaRepository.ObterTurmasDoGrupoAsync(turma.NomeBase, turma.Turno, turma.AnoLetivoId, turma.AnoEscolarId)`:
   - Percorre em ordem de `Sufixo` procurando a primeira com `PodeReceberNovaMatricula()`.
   - Se nenhuma tiver vaga, pega a de maior `Sufixo` e chama `AbrirTurmaIrma()`; persiste a nova turma via `AdicionarAsync`.
6. Chama `OcuparVaga()` na turma alvo; `Atualizar(turma)` se ja existente.
7. Cria a `Matricula` via `Matricula.Matricular(...)`; `AdicionarAsync`.
8. Um unico `IUnitOfWork.SalvarAlteracoesAsync` persiste turma(s) + matricula atomicamente; eventos de dominio publicados pelo `AppDbContext` apos o commit (mesmo mecanismo ja existente).
9. Retorna `MatriculaDto`.

## Estrutura de arquivos (espelha `Aluno`)

```
Domain/Turmas/{Turma,TurmaEvents,TurnoTurma,ITurmaRepository}.cs
Domain/Matriculas/{Matricula,MatriculaEvents,StatusMatricula,IMatriculaRepository}.cs

Application/Turmas/DTOs/TurmaDto.cs
Application/Turmas/Commands/CriarTurma/{CriarTurmaCommand,CriarTurmaCommandHandler,CriarTurmaCommandValidator}.cs
Application/Turmas/Queries/ObterTurmaPorId/{ObterTurmaPorIdQuery,ObterTurmaPorIdQueryHandler}.cs

Application/Matriculas/DTOs/MatriculaDto.cs
Application/Matriculas/Commands/MatricularAluno/{MatricularAlunoCommand,MatricularAlunoCommandHandler,MatricularAlunoCommandValidator}.cs
Application/Matriculas/Queries/ObterMatriculaPorId/{ObterMatriculaPorIdQuery,ObterMatriculaPorIdQueryHandler}.cs

Infrastructure/Persistence/Configurations/{TurmaConfiguration,MatriculaConfiguration}.cs
Infrastructure/Persistence/Repositories/{TurmaRepository,MatriculaRepository}.cs
Infrastructure/Persistence/AppDbContext.cs — adiciona DbSet<Turma>, DbSet<Matricula>
Infrastructure/DependencyInjection.cs — registra ITurmaRepository/IMatriculaRepository

Api/Controllers/{TurmasController,MatriculasController}.cs

tests/SistemaEscolar.UnitTests/Domain/{TurmaTests,MatriculaTests}.cs

infra/supabase/002_create_turmas.sql
infra/supabase/003_create_matriculas.sql
```

## Mapeamento EF Core (pontos de atencao)

- `Turma.Nome` (computada) — `builder.Ignore(t => t.Nome)`, nao mapear coluna.
- `Turma.Sufixo` (`char`) — mapear com `.HasConversion<string>()` para evitar peculiaridades de provider com tipo `char` no Npgsql.
- Indice composto em `turmas` para `(nome_base, turno, ano_letivo_id, ano_escolar_id)` — usado por `ObterTurmasDoGrupoAsync`.
- Indice em `matriculas` para `(aluno_id, ano_letivo_id)` — usado por `ExisteMatriculaAtivaAsync`; considerar indice parcial/filtrado por `status = 'Ativa'` no Postgres para reforcar a regra de matricula unica no banco tambem (defesa em profundidade, alem da checagem na Application).

## Testes unitarios planejados

`TurmaTests.cs`:
- Criar com dados validos → `Sufixo = 'A'`, `VagasOcupadas = 0`, dispara `TurmaCriadaEvent`.
- Criar sem `nomeBase` / com `vagasMaximas <= 0` → falha.
- `OcuparVaga()` ate atingir o maximo → dispara `TurmaLotadaEvent` na vaga que lota.
- `OcuparVaga()` quando ja lotada → falha.
- `AbrirTurmaIrma()` incrementa o sufixo corretamente (`'A' -> 'B'`).
- `AbrirTurmaIrma()` a partir de `Sufixo = 'Z'` → falha.

`MatriculaTests.cs`:
- `Matricular` com dados validos → `Status = Ativa`, dispara `AlunoMatriculadoEvent`.
- `Matricular` com `alunoId`/`turmaId`/`anoLetivoId` vazio → falha.
- `Trancar()` a partir de `Ativa` → sucesso; a partir de `Cancelada` → falha.
- `Cancelar()` a partir de `Ativa` e de `Trancada` → sucesso; a partir de `Concluida` → falha.
- `Concluir()` a partir de `Ativa` → sucesso; a partir de `Cancelada` → falha.

## Fora de escopo (YAGNI, proxima iteracao)

- Validar `Status` do `Aluno` (ex: exigir `Ativo`) antes de matricular.
- Reativar uma `Matricula` trancada/cancelada.
- Indicador de vagas/lotacao consultavel via query dedicada (`ListarTurmasComVagaDisponivel`).
- Integracao com Financeiro (geracao de cobranca de matricula ao ouvir `AlunoMatriculadoEvent`).
