-- Migração: tabela "turmas" (agregado Turma)
-- Compatível com o mapeamento EF Core em TurmaConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 001_create_alunos.sql.

create table if not exists turmas (
    id uuid primary key,
    ano_letivo_id uuid not null,
    ano_escolar_id uuid not null,
    nome_base varchar(100) not null,
    sufixo char(1) not null,
    turno varchar(20) not null,
    vagas_maximas integer not null,
    vagas_ocupadas integer not null default 0,
    criado_em timestamp not null default now()
);

create unique index if not exists ux_turmas_grupo_sufixo
    on turmas (nome_base, turno, ano_letivo_id, ano_escolar_id, sufixo);

comment on table turmas is 'Agregado raiz Turma (bounded context Acadêmico).';
comment on column turmas.sufixo is
    'Diferencia turmas-irmãs do mesmo grupo (mesmo nome_base/turno/ano), aberta automaticamente quando a anterior lota.';
