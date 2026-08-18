# Decisoes Arquiteturais (ADRs)

## ADR-001: Supabase na fase inicial, migracao futura para AWS RDS
Contexto: precisamos comecar rapido, mas o destino final de infraestrutura e AWS.
Decisao: usar Supabase apenas como Postgres gerenciado. Auth propria (nao Supabase Auth)
para reduzir acoplamento. Repositorios desacoplados via interface (DDD) na camada Domain.
Consequencia: migracao futura = dump/restore Postgres + reapontar connection string.

## ADR-002: Stripe para pagamentos
Contexto: cobranca de mensalidades/planos dos responsaveis.
Decisao: usar Stripe Subscriptions + Webhooks. Processamento de webhook deve ser idempotente.

## ADR-003: Reconhecimento facial como fonte primaria de presenca
Contexto: controle de presenca deve ser automatico via camera em sala/app do professor.
Decisao: presenca e evento derivado do reconhecimento facial; fallback manual sempre disponivel.
Consequencia: exige politica de consentimento (LGPD) antes do enrolamento biometrico do aluno.
