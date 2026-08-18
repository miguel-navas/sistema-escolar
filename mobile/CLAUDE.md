# Mobile - Sistema Escolar (React Native)

## Apps neste projeto
- **App Professor**: chamada por reconhecimento facial, aulas do dia, lancamento de notas.
- **App Responsavel**: acompanhamento do aluno, pagamentos (Stripe).

## Convencoes
- Navegacao via React Navigation.
- Captura de camera para reconhecimento facial isolada em `src/features/reconhecimento-facial/`,
  com interface clara de entrada/saida (foto -> resultado do matching).
- Nunca persistir foto/embedding facial em armazenamento local sem criptografia.
- Fluxo de pagamento sempre via Stripe SDK oficial - nunca capturar dado de cartao manualmente.

## Estrutura sugerida
- `src/features/` - um modulo por funcionalidade
- `src/navigation/`
- `src/api/`

## Comandos
- `npm start`
- `npm run android` / `npm run ios`
