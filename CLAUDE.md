# Sistema Escolar - Contexto do Projeto

## Visao Geral
Sistema de gestao escolar (web + mobile) com controle de alunos, professores,
disciplinas, turmas, calendario letivo, notas e frequencia via **reconhecimento
facial**. Inclui modulo financeiro com **Stripe** e acompanhamento evolutivo do
aluno (individual, por turma e por escola).

## Stack
- Backend: **C# / .NET** - Clean Architecture + DDD
- Frontend Web: **React**
- Mobile: **React Native**
- Banco (fase 1): **Supabase (PostgreSQL)** - migracao planejada para **AWS RDS**
- Pagamentos: **Stripe** (Subscriptions + Webhooks)
- Reconhecimento facial: servico desacoplado via interface de dominio
- Infra final: **AWS** (ECS/Fargate, S3, RDS, CloudFront)

## Estrutura do Monorepo
- `backend/` - API .NET (Domain, Application, Infrastructure, Api) - ver `backend/CLAUDE.md`
- `frontend-web/` - Painel administrativo/professor em React - ver `frontend-web/CLAUDE.md`
- `mobile/` - App professor (chamada facial) e responsavel (acompanhamento/pagamento) - ver `mobile/CLAUDE.md`
- `docs/DOMAIN.md` - Modelo de dominio, bounded contexts, agregados, eventos
- `docs/DECISIONS.md` - Decisoes arquiteturais (ADRs) - leia antes de propor mudanca de arquitetura
- `infra/` - Scripts/config de infraestrutura (Supabase, AWS)

## Regras de Arquitetura (nao negociaveis)
- Seguir **Clean Architecture**: Domain nao depende de nada; Application depende so de Domain;
  Infrastructure implementa interfaces definidas no Domain.
- Aplicar **SOLID** em toda integracao externa (Stripe, reconhecimento facial, banco de dados)
  via inversao de dependencia - isso e o que permite trocar Supabase por outro banco depois.
- Seguir **DDD**: bounded contexts = Academico, Frequencia/Biometria, Financeiro, Identidade.
  Comunicacao entre contextos via eventos de dominio, nao referencia direta de entidades.
- Presenca de aluno e um evento **derivado** do reconhecimento facial (RegistroReconhecimento),
  nao um lancamento manual - chamada manual e fallback/excecao, nao o fluxo padrao.
- Nunca commitar segredos (chaves Stripe, connection string, credenciais AWS). Usar `.env` (git-ignorado).
- Dado biometrico e dado sensivel (LGPD): nunca logar embeddings/fotos em texto claro,
  nunca remover checagem de consentimento do fluxo de enrolamento facial.

## Comandos
- Backend: `dotnet build` / `dotnet test` / `dotnet run --project backend/src/SistemaEscolar.Api`
- Frontend: `npm run dev` (dentro de `frontend-web/`)
- Mobile: `npm start` (dentro de `mobile/`, Expo/React Native CLI)

## Fluxo de trabalho esperado
1. Antes de implementar regra de negocio nova, consultar `docs/DOMAIN.md`.
2. Antes de mudar algo estrutural, consultar `docs/DECISIONS.md` e, se necessario, registrar novo ADR.
3. Preferir Explorar -> Planejar -> Implementar -> Testar, nessa ordem, para mudancas nao triviais.

## Manutenção de documentação (fazer isso sempre, sem perguntar)

Sempre que você concluir uma implementação (agregado, endpoint, regra de negócio):

1. **Atualize `docs/ROTEIRO.md`:**
   - Marque os itens da checklist da etapa concluída trocando `[ ]` por `[x]`.
   - Se a etapa foi só parcialmente concluída, adicione uma nota curta abaixo da
     checklist explicando o que falta (ex: "Falta endpoint de cancelamento de matrícula").

2. **Atualize `docs/DOMAIN.md`:**
   - Se você criou uma entidade/agregado/enum que não estava documentado lá,
     adicione na lista de "Entidades e Agregados principais".
   - Se você descobriu, durante a implementação, uma regra de negócio que não
     estava prevista no documento original, registre-a na seção correspondente.

3. **Registre um novo ADR em `docs/DECISIONS.md`** sempre que você tomar uma
   decisão arquitetural não trivial durante a implementação (ex: escolher entre
   duas formas de modelar um relacionamento, decidir usar um pacote novo,
   mudar uma estratégia de cálculo). Formato: `## ADR-00X: Título / Contexto /
   Decisão / Consequência`. Nunca decida algo estrutural silenciosamente sem
   registrar — se não tiver certeza se é "não trivial", registre mesmo assim.

4. **Atualize este `CLAUDE.md`** apenas se você perceber um padrão novo de
   convenção que deveria valer para todo o projeto daqui pra frente (ex: um
   jeito específico de tratar um tipo de erro). Não adicione detalhes de uma
   implementação específica aqui — isso vai em `docs/DOMAIN.md`.

5. **Ao final de cada sessão**, resuma em 2-3 linhas o que foi feito e me
   pergunte se devo commitar essas atualizações de documentação junto com o
   código, ou separadamente.

Nunca me pergunte se deve fazer os itens 1-3 acima — faça sempre, automaticamente,
como parte de concluir qualquer etapa. Só me avise no final o que foi atualizado.