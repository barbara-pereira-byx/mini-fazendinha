# Resultado da validação V3.6

Relatórios produzidos em Play Mode real no Unity 6000.6.7f1:

- `01-scroll.txt`: eventos reais do Input System; zoom invariável sobre a lista, zoom fora, topo/fim e resposta de rolagem.
- `02-tree-materials.txt`: os três prefabs próprios apontam para materiais URP com suas texturas originais.
- `03-acceptance.txt`: inventário/capacidade/ícones, necessidades, transferências exatas, cancelamento, reentrada, último cultivo, nomes/IDs, quatro níveis de produção, porteira e retorno da ficha para o curral. Testa callbacks reais dos botões e APIs dos modelos.
- `04-pointer-planting.txt`: cliques reais no prédio, poço, coleta, canteiro, seta e cartão; arraste de dois canteiros e colheita com foice. O teste de colheita antecipa somente o relógio de crescimento para não esperar o período completo.
- `05-navigation.txt`: duas vacas saem fisicamente pela porteira, retornam ao sino e permanecem confinadas; distância mínima de 1,506 m.
- `06-four-animals.txt`: quatro galinhas caminham/descansam por 30 segundos; distância mínima de 0,800 m; queda observada de alimentação 0,945615 → 0,928938 em 30,02 s; hidratação confere com a taxa configurada; reserva exclusiva e liberação do recipiente.
- `07-responsive-and-orchard.txt`: renderização e asserções dos limites dos dois gerenciadores, ficha animal e popup em 1920×1080, 1600×900 e 1366×768. Seleção de árvore preservada e preview com textura.
- `08-collection-transactions.txt`: produção e poço creditam exatamente uma vez; chamadas repetidas e callbacks reentrantes são recusados.
- `09-pagination-catalogs.txt`: catálogos de 1, 2, 3, 4, 9 e 10 cultivos, limites inferior/superior e número de cartões da última página; todos passaram.
- `vendor-differences.txt`: comparação SHA-256 contra o checkpoint anterior.
- `changed-files.txt`: arquivos efetivamente novos/modificados, comparados ao ZIP inicial, incluindo metadados.

## O que os testes não garantem

As necessidades em 30 s, 1 min, 5 min e 30 min foram simuladas no relógio dos getters; somente a observação indicada de 30 segundos foi acompanhada em tempo real. A fila de recipientes foi verificada pela reserva, não por um teste longo de todos os animais bebendo e comendo simultaneamente.

Não foram executadas todas as combinações de espécie, rotação e lotação, nem validação manual completa de digitação, hover, Esc, toque ou todas as trocas de resolução durante uso contínuo. Esses passos estão no README em `Assets/MiniFazendinha/Settings/V36`. As capturas não substituem uma avaliação humana de conforto.

A navegação usa aproximações por raio; não é uma simulação de colisão das malhas completas. Foram preservados os Animator/loops anteriores; não se afirma uma auditoria completa de todos os clips.

Não há sistema de salvamento de partida existente. Nomes, recursos e porteiras são estado da sessão. Reinicie Play Mode depois de recompilar scripts: a suíte valida a sessão iniciada a partir da cena salva, não hot reload de toda a interface dinâmica.

## Iterações de diagnóstico

Os testes encontraram bloqueio da primeira saída e do retorno pelo corredor. A correção incluiu ponto para liberar a saída, aproximação alinhada, precisão da grade e reserva de passagem. As rodadas finais estão nos relatórios numerados.

O teste de ponteiro também precisou esperar o Canvas retornar à resolução da Game View após uma captura em RenderTexture; clicar antes disso usava coordenadas de captura antigas. A regressão final passou após estabilizar essas coordenadas.

As tentativas de avaliação dinâmica são utilitários de diagnóstico fora de Assets. Uma avaliação de código falhou por formato de `using`, e outra pela conversão de strings do avaliador Roslyn; esses erros não eram falhas de compilação do projeto. O teste de paginação foi movido para um menu compilado no Editor.

## Recuperação

Checkpoint inicial: `.minifazendinha-checkpoints/before-v36-20261022.zip`.
Documentação de arquitetura, configuração, uso e validação: `Assets/MiniFazendinha/Settings/V36/README.md`.
Capturas finais: `Screenshots/ContextUX/V36-*-1920x1080.png`, equivalentes em 1600×900 e 1366×768, `V36-cultivos.png`, `V36-planting-clean.png` e `V36-textured-trees.png`.

## Estado final

Compilação concluída sem falhas; console final com zero erros e zero avisos (arquivos final-compilation.json e final-console.json). Nenhum arquivo dos fornecedores mudou. O inventário e os nomes usados como fixtures foram descartados ao sair do Play Mode; a cena salva conserva as configurações iniciais.
