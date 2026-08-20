-- Migração: tabela "matriculas" (agregado Matricula)
-- Compatível com o mapeamento EF Core em MatriculaConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 002_create_turmas.sql.

create table if not exists matriculas (
    id uuid primary key,
    aluno_id uuid not null,
    turma_id uuid not null,
    ano_letivo_id uuid not null,
    status varchar(20) not null default 'Ativa',
    matriculado_em timestamp not null default now()
);

create unique index if not exists ux_matriculas_aluno_ativa
    on matriculas (aluno_id, ano_letivo_id)
    where status = 'Ativa';

comment on table matriculas is 'Agregado raiz Matricula (bounded context Acadêmico).';
comment on column matriculas.status is
    'Índice único parcial garante no máximo uma matrícula Ativa por aluno/ano letivo.';
