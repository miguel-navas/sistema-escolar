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
