# Backend - Sistema Escolar (.NET)

## Camadas (Clean Architecture)
- `SistemaEscolar.Domain` - Entidades, Agregados, Value Objects, interfaces de
  repositorio e de servicos externos (IFacialRecognitionService, IPaymentGateway).
  Nao pode referenciar nenhuma outra camada.
- `SistemaEscolar.Application` - Casos de uso (ex: RegistrarChamadaPorReconhecimentoFacial,
  GerarCobrancaMensalidade, CalcularIndicadorEvolutivoAluno), DTOs, validacoes.
- `SistemaEscolar.Infrastructure` - Implementacoes concretas: EF Core / Supabase,
  integracao Stripe, integracao servico de reconhecimento facial, storage S3.
- `SistemaEscolar.Api` - Controllers, autenticacao/autorizacao, mapeamento DTO.

## Bounded Contexts (DDD)
- **Academico**: AnoLetivo, AnoEscolar, Turma, Aluno, Matricula, Disciplina, Aula, Nota
- **Frequencia/Biometria**: PerfilBiometrico, RegistroReconhecimento, Presenca
- **Financeiro**: Plano, Cobranca, Pagamento
- **Identidade**: Usuario, Perfil, permissoes

Comunicacao entre contextos via eventos de dominio (ex: `AlunoMatriculadoEvent`,
`CobrancaVencidaEvent`), nunca por referencia direta de entidade de outro contexto.

## Convencoes de codigo
- Um agregado por arquivo; Value Objects imutaveis (ex: `Cpf`, `PeriodoLetivo`, `ScoreConfianca`).
- Repositorios expoem so metodos que fazem sentido de dominio (nao `IQueryable` generico vazando para fora).
- Casos de uso da Application nao devem conter SQL nem chamadas HTTP diretas - sempre via interface.
- Testes: um projeto de unit tests para Domain/Application, um de integracao para Infrastructure/Api.

## Gotchas do projeto
- `IFacialRecognitionService` deve ser mockavel - nao amarrar Application a um provedor especifico.
- Repositorios devem funcionar contra PostgreSQL "puro" (Supabase hoje, RDS amanha).
- Webhooks do Stripe precisam ser idempotentes.

## Comandos
- `dotnet build`
- `dotnet test`
- `dotnet run --project src/SistemaEscolar.Api`
