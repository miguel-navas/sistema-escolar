# Exemplo de referência: agregado `Aluno`

Este é o primeiro agregado implementado, servindo de **modelo** para os próximos
(Turma, Matricula, Aula, PerfilBiometrico, RegistroReconhecimento, Cobranca...).
Ao pedir ao Claude Code para implementar um novo agregado, referencie este exemplo.

## Onde está cada coisa

| Camada | Arquivo | Responsabilidade |
|---|---|---|
| Domain | `Domain/Common/Entity.cs`, `AggregateRoot.cs`, `ValueObject.cs`, `Result.cs` | Building blocks de DDD reutilizados por todo agregado |
| Domain | `Domain/SharedKernel/ValueObjects/Cpf.cs`, `Email.cs` | Value Objects compartilhados, com validação embutida |
| Domain | `Domain/Alunos/Aluno.cs` | **Regra de negócio real** — único lugar que pode alterar o estado do aluno |
| Domain | `Domain/Alunos/IAlunoRepository.cs` | Contrato do repositório (implementado em Infrastructure) |
| Domain | `Domain/Alunos/AlunoEvents.cs` | Eventos de domínio disparados pelo agregado |
| Application | `Application/Alunos/Commands/CriarAluno/*` | Caso de uso "criar aluno" (orquestração, sem regra de negócio) |
| Application | `Application/Alunos/Queries/ObterAlunoPorId/*` | Caso de uso de leitura |
| Infrastructure | `Infrastructure/Persistence/AppDbContext.cs` | DbContext + publicação de eventos de domínio pós-commit |
| Infrastructure | `Infrastructure/Persistence/Configurations/AlunoConfiguration.cs` | Mapeamento EF Core (Fluent API) |
| Infrastructure | `Infrastructure/Persistence/Repositories/AlunoRepository.cs` | Implementação concreta do repositório |
| Infrastructure | `Infrastructure/DependencyInjection.cs` | Registro de DI (DbContext, repositórios) |
| Api | `Api/Controllers/AlunosController.cs` | Endpoints HTTP — só traduz request/response, sem lógica |
| Tests | `tests/SistemaEscolar.UnitTests/Domain/AlunoTests.cs` | Testes das regras de negócio do agregado, sem banco/HTTP |

## Regras de negócio implementadas neste exemplo
- Aluno começa como `PreCadastrado`, precisa ser `Ativado` explicitamente.
- CPF é opcional, mas se informado precisa ser matematicamente válido e único.
- **Consentimento biométrico é obrigatório antes de qualquer presença por
  reconhecimento facial** — ver `PodeTerPresencaRegistradaPorReconhecimentoFacial()`.
  Este método será chamado pelo bounded context de Frequência/Biometria quando
  esse módulo for implementado.
- `AlunoCadastradoEvent` e `ConsentimentoBiometricoRegistradoEvent` ficam
  disponíveis para outros contextos reagirem (ex: Financeiro criar cobrança
  de matrícula ao ouvir `AlunoCadastradoEvent`).

## Como rodar localmente
1. Configure `ConnectionStrings:Postgres` em `src/SistemaEscolar.Api/appsettings.json`
   (ou via variável de ambiente / `dotnet user-secrets`) com a connection string do Supabase.
2. Rode a migração SQL: `infra/supabase/001_create_alunos.sql` no SQL editor do Supabase.
3. `dotnet restore`
4. `dotnet build`
5. `dotnet test` — deve rodar os testes de `AlunoTests.cs`
6. `dotnet run --project src/SistemaEscolar.Api` — sobe a API com Swagger em `/swagger`

## Próximo passo sugerido
Implementar o agregado `Turma` (e `Matricula`) seguindo exatamente esta mesma
estrutura de pastas e camadas. Depois, `PerfilBiometrico`/`RegistroReconhecimento`
no bounded context de Frequência, consumindo `PodeTerPresencaRegistradaPorReconhecimentoFacial()`.
