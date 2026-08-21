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
