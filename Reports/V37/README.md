# MiniFazendinha V3.7 — evidências

Implementação e validação pelo Unity Editor 6000.6.7f1, cena FarmMain.

## Resultados

- `production.txt`: limites determinísticos de todas as espécies; bem-estar altera quantidade, não intervalo.
- `production-real-time.txt`: 185 segundos de Updates reais, sem acelerar o jogo e sem invocar produção manualmente.
- `product-collection.txt`: coleta individual e pelo curral nas cinco espécies, saldo exato e repetição sem duplicação.
- `tree-placement.txt`: prévia e clique real de posicionamento das três árvores; bloqueio em estrutura/curral e entre árvores.
- `tree-harvest.txt`: clique real na copa e cesta; três frutas, árvore mantida, repetição recusada e nova frutificação. Crescimento e frutificação avançados deterministicamente pelo próprio componente.
- `gates.txt`: renderizadores e colliders da folha móvel acompanham o estado nos cinco currais.
- `ui.txt`: dimensões iguais ao armazém, um único botão de porteira e moradores vinculados corretamente.
- `responsiveness.txt`: limites e conteúdo dos painéis em 1920×1080, 1600×900 e 1366×768; capturas reais em Screenshots/ContextUX/V37-*.
- `vendor-check.txt`: nenhum arquivo original dos fornecedores alterado, verificado por SHA-256.

Os arquivos de diagnóstico de posicionamento e retorno registram a investigação, não são testes de aceitação. A regressão de navegação identificou disputa pela entrada e motivou a escolha de um ponto livre com prioridade de espera. Os resultados finais estão em `navigation.txt`.

## Configuração

Galinha 60 s; pato 90 s; vaca 120 s; porco 150 s; ovelha 180 s. Valores editáveis nos dados por espécie e limite mínimo de 60 s. Ovo de pato e trufa são itens próprios no inventário comum. Árvores: maturação 60 s, frutificação 90 s, três frutas por coleta. Sem custo novo de muda: preservada a regra existente de posicionamento.

## Limites e revisão manual

Não foi criado salvamento de partida nem animação específica de escavação para trufas. Não se afirma cobertura de todas as combinações possíveis de animais ou de todos os clips. A revisão humana de hover, conforto visual, digitação e movimentação prolongada continua recomendada. Instruções completas em Assets/MiniFazendinha/Settings/V37/README.md. A lista de alterações está em changed-files.txt.

Checkpoint anterior: .minifazendinha-checkpoints/before-v37-20261022.zip. O pacote contém MiniFazendinha para restauração dentro de Assets, além de Packages e ProjectSettings. Pare o Editor antes de restaurar e preserve uma cópia da versão atual.

## Regressão final

`scroll.txt`: roda sobre a UI altera o Orthographic Size em exatamente zero; fora da UI o zoom continua. `pointer-planting.txt`: clique real no prédio, poço e canteiro, coleta de água, paginação, plantio por clique/arraste e colheita com foice.

`navigation.txt`: duas vacas saíram e retornaram pela passagem real; distância mínima 1,517 m para raios somados de 1,50 m, sem teleporte. `four-animals.txt`: quatro animais por 30,03 s com distância mínima 0,833 m e consumo conforme configuração. O primeiro ensaio de quatro animais usava coordenada fixa para inserir o quarto numa sessão em movimento; falhou e está preservado em four-animals-initial-fixture-failure.txt. O teste foi corrigido para validar uma posição livre antes da observação; não foram reduzidos os raios ou tolerâncias para fazê-lo passar.

Capturas foram inspecionadas visualmente para o painel Full HD, painel 1366×768, menu de plantio e poço. Todas são renderizações do jogo, não imagens de referência sobrepostas.

## Entrada e estado final do Editor

A rotina normal e a recarga de scripts passaram pelas asserções do Input System (`input-cleanup-tests.txt`). Após interromper Play Mode, o estado real foi consultado separadamente (`input-after-stop.txt`): mouse e teclado físicos habilitados, mouse físico atual e nenhum dispositivo virtual restante. `input-after-reload.txt` registra o mesmo estado após recarga. O callback assíncrono da interrupção não anexou uma conclusão ao log do teste; a comprovação dessa etapa é a consulta posterior dos dispositivos.

Editor deixado fora de Play Mode; Run In Background restaurado ao valor do projeto. Cena salva pelo comando do Editor. Compilação e Console finais em final-compilation.json e final-console.json.
