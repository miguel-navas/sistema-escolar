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
