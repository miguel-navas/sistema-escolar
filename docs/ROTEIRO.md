# Roteiro de Implementação — Backend Sistema Escolar

> Guia de sessões com o Claude Code. Siga a ordem — cada etapa depende da anterior.
> Sempre peça para ele ler o `CLAUDE.md`, `docs/DOMAIN.md` e `backend/EXEMPLO-ALUNO.md`
> antes de codificar, e usar exatamente o mesmo padrão do agregado `Aluno`.

---

## Etapa 0 — já feita ✅
- Estrutura do projeto (CLAUDE.md, docs, monorepo)
- Agregado `Aluno` completo (Domain, Application, Infrastructure, Api, testes)

---

## Etapa 1 — AnoLetivo, AnoEscolar/Série, Turma, Matrícula
**Por quê primeiro:** tudo no sistema depende de "em qual ano letivo e turma" um aluno está. Sem isso, não dá pra ligar Aula, Nota, Presença ou Cobrança a nada.

**Agregados/entidades:**
- `AnoLetivo` (ano, data_inicio, data_fim, status)
- `AnoEscolar` (nome, nivel_ensino)
- `Turma` (ano_letivo_id, ano_escolar_id, nome, turno) — aggregate root
- `Matricula` (aluno_id, turma_id, ano_letivo_id, status) — pode ser entidade filha de Turma ou agregado próprio (ver nota abaixo)

**Regras de negócio a encapsular:**
- Uma Turma só aceita matrícula se pertencer a um AnoLetivo com status "Ativo"
- Aluno não pode ter 2 matrículas ativas no mesmo AnoLetivo
- Cancelar/trancar matrícula é uma transição de estado (não delete)

**Nota de design:** decida com o Claude Code se `Matricula` é agregado próprio (referenciando `Aluno.Id` e `Turma.Id` só por Guid, nunca por referência de objeto) ou entidade interna de `Turma`. Recomendado: agregado próprio — matrícula muda de estado independente do ciclo de vida da turma.

**Prompt sugerido:**
```
Leia CLAUDE.md, docs/DOMAIN.md e backend/EXEMPLO-ALUNO.md.
Implemente os agregados AnoLetivo, AnoEscolar e Turma, e o agregado Matricula
(referenciando Aluno e Turma só por Id), seguindo exatamente o padrão do
agregado Aluno: Domain (entidade + regras + repositório + eventos),
Application (commands/queries via MediatR), Infrastructure (EF Core config
+ repositório), Api (controller), e testes unitários das regras de negócio.
Não deixe eu sem revisar: pare e me mostre o plano antes de implementar.
```

**Checklist de validação:**
- [x] `dotnet build` e `dotnet test` passam
- [x] Migração SQL criada em `infra/supabase/`
- [ ] Endpoint testado no Swagger: criar ano letivo → criar turma → matricular aluno (código pronto e testado via build/testes automatizados; fluxo manual ainda não exercitado neste ambiente por falta de conexão real com Supabase/Postgres)
- [x] Regra "matrícula duplicada no mesmo ano letivo" tem teste unitário

---

## Etapa 2 — Professor, Disciplina, ProfessorDisciplinaTurma
**Por quê agora:** Aula (próxima etapa) precisa de Professor + Disciplina + Turma já existindo.

**Agregados/entidades:**
- `Professor` (dados pessoais, formação)
- `Disciplina` (nome, carga_horaria, ano_escolar_id)
- `ProfessorDisciplinaTurma` (vínculo — pode ser entidade simples, sem muita regra de negócio)

**Regras de negócio a encapsular:**
- Disciplina só pode ser vinculada a Turma cujo AnoEscolar bate com o AnoEscolar da Disciplina
- Um Professor não pode ter dois vínculos idênticos (mesma disciplina + turma + ano letivo)

**Prompt sugerido:**
```
Leia CLAUDE.md e docs/DOMAIN.md. Implemente os agregados Professor, Disciplina
e o vínculo ProfessorDisciplinaTurma, seguindo o padrão do agregado Aluno.
Adicione a regra de negócio de que uma Disciplina só pode ser vinculada a uma
Turma do mesmo AnoEscolar. Pare e me mostre o plano antes de implementar.
```

**Checklist de validação:**
- [ ] Testes cobrindo a regra de compatibilidade AnoEscolar
- [ ] Swagger: criar professor → criar disciplina → vincular a uma turma da Etapa 1

---

## Etapa 3 — CalendarioLetivo e Aula (conteúdo aplicado no dia)
**Por quê agora:** depende de Turma + Disciplina + Professor já existirem (Etapas 1 e 2). É pré-requisito da Etapa 4 (presença), já que presença sempre se vincula a uma Aula específica.

**Agregados/entidades:**
- `CalendarioLetivo` (ano_letivo_id, data, tipo_evento — feriado, recesso, evento)
- `Aula` (turma_id, disciplina_id, professor_id, data, conteudo_aplicado) — aggregate root

**Regras de negócio a encapsular:**
- Não é possível registrar Aula em data marcada como feriado/recesso no CalendarioLetivo
- Aula só pode ser criada se existir vínculo ativo ProfessorDisciplinaTurma para aquela combinação

**Prompt sugerido:**
```
Leia CLAUDE.md e docs/DOMAIN.md. Implemente CalendarioLetivo e o agregado Aula,
validando que a Aula não pode ser criada em data de feriado/recesso e que o
professor/disciplina/turma têm vínculo ativo (reaproveite o repositório de
ProfessorDisciplinaTurma da Etapa 2). Siga o padrão do agregado Aluno.
Pare e me mostre o plano antes de implementar.
```

**Checklist de validação:**
- [ ] Teste: criar aula em dia de feriado deve falhar
- [ ] Teste: criar aula sem vínculo professor-disciplina-turma deve falhar
- [ ] Swagger: fluxo completo de registrar aula do dia com conteúdo aplicado

---

## Etapa 4 — PerfilBiometrico e RegistroReconhecimento (Frequência/Biometria)
**Por quê agora:** é o módulo mais sensível (LGPD) e mais arriscado tecnicamente — só faz sentido depois que Aluno e Aula já existem e estão testados.

**Agregados/entidades:**
- `PerfilBiometrico` (aluno_id, embedding_facial, foto_referencia, status_consentimento) — aggregate root, criado só depois de `Aluno.ConsentimentoBiometricoRegistrado == true`
- `RegistroReconhecimento` (aula_id, aluno_id, score_confianca, timestamp, origem_dispositivo, status)
- `Presenca` (aula_id, aluno_id, status, origem: facial|manual, justificativa) — evento derivado de RegistroReconhecimento

**Interface a definir (Domain), implementar depois (Infrastructure):**
```csharp
public interface IFacialRecognitionService
{
    Task<ResultadoReconhecimento> IdentificarAsync(byte[] fotoCapturada, Guid turmaId, CancellationToken ct);
}
```
Isso mantém o provedor (AWS Rekognition ou outro) desacoplado — trocar depois não deve afetar Domain/Application.

**Regras de negócio a encapsular:**
- `PerfilBiometrico` só pode ser criado se `Aluno.PodeTerPresencaRegistradaPorReconhecimentoFacial()` permitir
- `RegistroReconhecimento` com score abaixo de um limiar configurável gera status "PendenteRevisao", não presença automática
- `Presenca` é sempre gerada a partir de um `RegistroReconhecimento` confirmado, OU lançada manualmente pelo professor como fallback (registrar `origem: manual`)

**Prompt sugerido (fazer em 2 sessões separadas — é módulo maior):**

*Sessão 4a — Domain e Application (sem integração real ainda):*
```
Leia CLAUDE.md, docs/DOMAIN.md e docs/DECISIONS.md (ADR-003).
Implemente o Domain e Application dos agregados PerfilBiometrico,
RegistroReconhecimento e Presenca. Defina a interface IFacialRecognitionService
no Domain (sem implementação ainda). Regra central: PerfilBiometrico só pode
ser criado se Aluno.PodeTerPresencaRegistradaPorReconhecimentoFacial() for
verdadeiro. Presenca é sempre derivada de um RegistroReconhecimento confirmado
ou lançada manualmente como fallback. Pare e me mostre o plano antes de implementar.
```

*Sessão 4b — Infrastructure (integração real):*
```
Implemente FacialRecognitionService (Infrastructure) usando [AWS Rekognition /
outro provedor que você escolher], implementando IFacialRecognitionService do
Domain. Registre no DependencyInjection.cs. Não altere nada em Domain ou
Application nesta sessão.
```

**Checklist de validação (atenção redobrada aqui — LGPD):**
- [ ] Teste: criar PerfilBiometrico sem consentimento deve falhar
- [ ] Teste: score abaixo do limiar não gera presença automática
- [ ] Confirmar que nenhuma foto/embedding é logada em texto claro (revisar logs manualmente)
- [ ] Fluxo de fallback manual testado no Swagger

---

## Etapa 5 — Nota e IndicadorEvolutivo
# Adendo — Situação Acadêmica do Aluno (Aprovado / Recuperação / Reprovado)

> Aplique este conteúdo em `docs/DOMAIN.md` (nova seção) e substitua a Etapa 5
> do `roteiro-implementacao-backend.md` pela versão revisada no final deste arquivo.

## Por que isso precisa ser modelado explicitamente
No roteiro original, "situação (aprovado/recuperação/reprovado)" apareceu só
como comentário dentro de `IndicadorEvolutivo`. Isso é errado: `IndicadorEvolutivo`
é uma foto estatística (médias, ranking) — a **situação acadêmica é uma decisão
de negócio com regra própria, histórico e consequência** (aluno reprovado não
avança de série; aluno em recuperação tem uma atividade extra pendente). Merece
entidade e Domain Service próprios.

## Novo conceito: `StatusAcademico` (enum)
```csharp
public enum StatusAcademico
{
    EmCursamento = 1,      // período letivo ainda não fechado
    Aprovado = 2,
    EmRecuperacao = 3,     // média ficou na faixa de recuperação, aguardando nota extra
    AprovadoAposRecuperacao = 4,
    Reprovado = 5,
    ReprovadoPorFalta = 6  // frequência abaixo do mínimo, independente da média
}
```

## Nova entidade: `SituacaoDisciplina`
Situação do aluno em **uma disciplina**, dentro de **um ano letivo** — é o nível
onde a regra realmente se aplica (aprovação é sempre por disciplina antes de
virar situação final da matrícula).

```
SituacaoDisciplina
- id
- aluno_id
- disciplina_id
- ano_letivo_id
- media_final          (calculada a partir das Notas por periodo_avaliativo)
- frequencia_percentual (vem do módulo de Presença — Etapa 4)
- status: StatusAcademico
- nota_recuperacao      (nullable — só preenchida se status = EmRecuperacao)
- atualizado_em
```

## Nova entidade agregada: `SituacaoFinalMatricula`
Situação **consolidada do aluno no ano letivo**, calculada a partir de todas as
`SituacaoDisciplina` daquele aluno naquele `ano_letivo_id`. É o que decide se o
aluno avança para o próximo ano/série.

```
SituacaoFinalMatricula
- id
- matricula_id
- ano_letivo_id
- status: StatusAcademico   (Aprovado / Reprovado / EmRecuperacao — nunca "ReprovadoPorFalta" aqui, isso é por disciplina)
- disciplinas_pendentes: List<Guid>  (disciplinas ainda em recuperação/reprovadas)
- calculado_em
```

## Regra de negócio (Domain Service, não Application)
Criar um `CalculadoraSituacaoAcademicaService` no Domain (é lógica pura, sem I/O,
mas envolve múltiplas entidades — por isso Domain Service, não método de uma
única entidade):

```csharp
public interface ICalculadoraSituacaoAcademica
{
    StatusAcademico CalcularSituacaoDisciplina(
        IReadOnlyList<Nota> notasDoPeriodo,
        decimal frequenciaPercentual,
        CriteriosAprovacao criterios);

    StatusAcademico CalcularSituacaoFinalMatricula(
        IReadOnlyList<SituacaoDisciplina> situacoesPorDisciplina);
}
```

`CriteriosAprovacao` é um **Value Object configurável por escola** (não hardcode):
```csharp
public sealed record CriteriosAprovacao(
    decimal MediaMinimaAprovacao,     // ex: 7.0
    decimal MediaMinimaRecuperacao,   // ex: 5.0 (abaixo disso já é reprovado direto, sem recuperação)
    decimal FrequenciaMinimaPercentual // ex: 75%
);
```

**Fluxo da regra:**
1. Se `frequenciaPercentual < FrequenciaMinimaPercentual` → `ReprovadoPorFalta` (frequência sempre é verificada primeiro; nota não salva o aluno de falta).
2. Senão, se `media >= MediaMinimaAprovacao` → `Aprovado`.
3. Senão, se `media >= MediaMinimaRecuperacao` → `EmRecuperacao` (aguarda `nota_recuperacao`).
4. Senão → `Reprovado`.
5. Quando `nota_recuperacao` for lançada: se a média entre nota original e nota de recuperação (regra específica da escola) atingir o mínimo → `AprovadoAposRecuperacao`; senão → `Reprovado`.

> ⚠️ Os valores de `CriteriosAprovacao` e a fórmula exata de recuperação (média
> simples, substituição da menor nota, etc.) **variam entre escolas** — isso é
> risco já sinalizado no plano original. Validar com a escola antes de fixar
> os números; manter `CriteriosAprovacao` como configuração, nunca como
> constante no código.

## Eventos de domínio novos
- `SituacaoDisciplinaCalculadaEvent` (aluno_id, disciplina_id, status)
- `AlunoReprovadoEvent` (aluno_id, ano_letivo_id) — pode ser escutado pelo
  contexto Acadêmico para impedir matrícula automática na série seguinte
- `AlunoEntrouEmRecuperacaoEvent` (aluno_id, disciplina_id) — pode disparar
  notificação ao responsável (app mobile)

## Onde isso aparece no boletim / painel
- Boletim do aluno mostra `SituacaoDisciplina` por disciplina (não só a nota)
- Painel do professor mostra lista de alunos em recuperação na sua disciplina
- `IndicadorEvolutivo` (Etapa 5 original) continua existindo, mas agora é
  **estatística** (médias, ranking) — a **decisão** de aprovado/reprovado vive
  em `SituacaoDisciplina`/`SituacaoFinalMatricula`, não nele.

---

## Etapa 5 revisada — Nota, Situação Acadêmica e IndicadorEvolutivo

**Agregados/entidades (substituindo a Etapa 5 original):**
- `Nota` (aluno_id, disciplina_id, periodo_avaliativo, valor) — já estava no roteiro
- `SituacaoDisciplina` (novo — ver acima)
- `SituacaoFinalMatricula` (novo — ver acima)
- `CriteriosAprovacao` — Value Object, carregado de configuração (por escola/ano letivo)
- `IndicadorEvolutivo` (media_geral, frequencia_percentual, posicao_turma, posicao_escola) — só estatística, sem decisão de aprovação

**Prompt sugerido (substitui o prompt original da Etapa 5):**
```
Leia CLAUDE.md, docs/DOMAIN.md e docs/DOMAIN-status-aluno.md (adendo sobre
situação acadêmica). Implemente:
1. O agregado Nota.
2. O Value Object CriteriosAprovacao.
3. O Domain Service CalculadoraSituacaoAcademica (interface ICalculadoraSituacaoAcademica
   + implementação), com a regra: frequência mínima é verificada antes da média;
   faixas de Aprovado / EmRecuperacao / Reprovado / ReprovadoPorFalta conforme
   o adendo.
4. As entidades SituacaoDisciplina e SituacaoFinalMatricula, incluindo o fluxo
   de lançamento de nota de recuperação e recálculo do status.
5. O serviço de cálculo de IndicadorEvolutivo (media_geral, frequencia_percentual,
   posicao_turma, posicao_escola), separado da decisão de aprovação.
6. Eventos de domínio: SituacaoDisciplinaCalculadaEvent, AlunoReprovadoEvent,
   AlunoEntrouEmRecuperacaoEvent.
Siga o padrão do agregado Aluno em todas as camadas (Domain, Application,
Infrastructure, Api, testes). Pare e me mostre o plano antes de implementar.
```

**Checklist de validação (substitui a checklist original da Etapa 5):**
- [ ] Teste: frequência abaixo do mínimo reprova mesmo com média alta (ReprovadoPorFalta)
- [ ] Teste: média entre os dois limiares gera EmRecuperacao, não Reprovado direto
- [ ] Teste: lançar nota de recuperação recalcula status corretamente
- [ ] Teste: SituacaoFinalMatricula reflete corretamente quando há disciplina reprovada
- [ ] Teste: posicao_escola continua null/N-A quando só há 1 turma no ano/série
- [ ] Boletim (endpoint) mostra status por disciplina, não só a nota
- [ ] `CriteriosAprovacao` vem de configuração, não de valor fixo no código


## Etapa 6 — Plano, Cobranca, Pagamento (Stripe)
**Por quê por último no backend:** é um bounded context isolado (Financeiro) — pode, tecnicamente, ser feito em paralelo a partir da Etapa 2, mas colocamos depois para não competir por atenção com o núcleo acadêmico/biometria primeiro.

**Agregados/entidades:**
- `Plano` (responsavel_id, tipo, valor, recorrencia)
- `Cobranca` (plano_id, valor, vencimento, status, stripe_subscription_id)
- `Pagamento` (cobranca_id, status, data_pagamento, stripe_payment_intent_id)

**Regras de negócio a encapsular:**
- Cobrança vencida sem pagamento dispara `CobrancaVencidaEvent`
- Webhook do Stripe deve ser **idempotente**: checar `stripe_payment_intent_id` já processado antes de aplicar

**Prompt sugerido (2 sessões):**

*Sessão 6a — Domain e Application:*
```
Leia CLAUDE.md e docs/DECISIONS.md (ADR-002). Implemente os agregados Plano,
Cobranca e Pagamento (Domain + Application), com a interface IPaymentGateway
no Domain (sem implementação ainda). Pare e me mostre o plano antes de implementar.
```

*Sessão 6b — Infrastructure (Stripe real):*
```
Implemente StripePaymentGateway (Infrastructure) usando Stripe.net, incluindo
endpoint de webhook idempotente (checar stripe_payment_intent_id antes de
aplicar). Registre no DependencyInjection.cs.
```

**Checklist de validação:**
- [ ] Teste: webhook duplicado não gera pagamento duplicado
- [ ] Teste: cobrança vencida dispara evento corretamente
- [ ] Fluxo de teste real no Stripe (modo sandbox/test mode)

---

## Depois do backend: Frontend e Mobile
Só depois que os módulos acima estiverem com API funcional, partir para:
1. **Frontend Web** — telas administrativas consumindo os endpoints já testados
2. **Mobile (React Native)** — app do professor (chamada facial) e do responsável (acompanhamento/pagamento)

Cada tela deve ser pedida ao Claude Code apontando para o endpoint específico já existente — nunca pedir para "criar a tela X" sem o backend correspondente pronto.

---

## Regra geral de ouro para todas as sessões
1. Sempre mande ele ler `CLAUDE.md` + `docs/DOMAIN.md` (+ ADR relevante em `docs/DECISIONS.md`) antes de codificar.
2. Sempre peça o plano antes da implementação (`Shift+Tab` / plan mode, ou peça explicitamente no prompt).
3. Depois de cada etapa: `dotnet build`, `dotnet test`, e um teste manual no Swagger antes de avançar para a próxima.
4. Se ele desviar do padrão (regra de negócio vazando pro Controller, Application acessando banco direto, etc.), corrija na hora — não deixe passar, vira precedente.
