# Modelo de Dominio - Sistema Escolar

> Consultar este arquivo antes de implementar/alterar regra de negocio.

## Bounded Contexts
1. Academico
2. Frequencia/Biometria
3. Financeiro
4. Identidade

## Entidades e Agregados principais
- AnoLetivo (id, ano, data_inicio, data_fim, status)
- AnoEscolar/Serie (id, nome, nivel_ensino)
- Turma (id, ano_letivo_id, ano_escolar_id, nome, turno)
- Aluno (id, dados pessoais, responsavel_id)
- Matricula (id, aluno_id, turma_id, ano_letivo_id, status)
- Professor (id, dados pessoais, formacao)
- Disciplina (id, nome, carga_horaria, ano_escolar_id)
- ProfessorDisciplinaTurma (id, professor_id, disciplina_id, turma_id, ano_letivo_id)
- CalendarioLetivo (id, ano_letivo_id, data, tipo_evento)
- Aula (id, turma_id, disciplina_id, professor_id, data, conteudo_aplicado)
- PerfilBiometrico (aluno_id, embedding_facial, foto_referencia, status_consentimento)
- RegistroReconhecimento (aula_id, aluno_id, score_confianca, timestamp, origem_dispositivo, status)
- Presenca (aula_id, aluno_id, status, origem: facial|manual, justificativa)
- Nota (aluno_id, disciplina_id, periodo_avaliativo, valor)
- IndicadorEvolutivo (aluno_id, periodo, media_geral, frequencia_percentual, posicao_turma, posicao_escola)
- Plano/Cobranca (responsavel_id, tipo, valor, recorrencia, stripe_subscription_id)
- Pagamento (cobranca_id, status, data_pagamento, stripe_payment_intent_id)

## Regras de negocio importantes
- Presenca e um evento derivado de RegistroReconhecimento; chamada manual e excecao.
- IndicadorEvolutivo e sempre calculado, nunca editado diretamente pelo usuario.
- Comparativo "turma x escola" so faz sentido quando ha mais de uma turma no mesmo ano/serie
  dentro do mesmo ano letivo.

## Eventos de dominio (exemplos)
- AlunoMatriculadoEvent
- PresencaRegistradaEvent
- CobrancaVencidaEvent
- PagamentoConfirmadoEvent
- AnoLetivoCriadoEvent / AnoLetivoAtivadoEvent / AnoLetivoEncerradoEvent
- AnoEscolarCriadoEvent
- ProfessorCadastradoEvent
- DisciplinaCriadaEvent
- VinculoProfessorDisciplinaTurmaCriadoEvent / VinculoProfessorDisciplinaTurmaEncerradoEvent
