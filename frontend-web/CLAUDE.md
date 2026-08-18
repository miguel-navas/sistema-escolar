# Frontend Web - Sistema Escolar (React)

## Sobre
Painel administrativo, painel do professor e painel financeiro do sistema escolar.

## Convencoes
- Componentes funcionais + hooks; sem componentes de classe.
- Chamadas de API centralizadas em `src/api/` (um cliente por bounded context).
- Estado de servidor via React Query - nao duplicar estado de API em Redux/Context.
- Formularios de cadastro devem validar no client E confiar na validacao do backend.

## Estrutura sugerida
- `src/pages/` - telas por modulo (academico/, financeiro/, frequencia/)
- `src/components/` - componentes reutilizaveis
- `src/api/` - clientes HTTP
- `src/hooks/` - hooks compartilhados

## Comandos
- `npm run dev`
- `npm run build`
- `npm run lint`
