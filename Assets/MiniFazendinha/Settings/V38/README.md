# MiniFazendinha — ajustes de coleta e retorno (V3.8)

Solicitação: caixas menores e consistentes para poço/árvores, coleta animal textual, bacon em vez de trufa e correção do sino.

## Alterações

- Poço e árvores reutilizam CollectionPanel, 300×206 unidades do Canvas. Botão verde Coletar de 116×38, centralizado horizontalmente. Informações reais de disponibilidade/crescimento/recarga preservadas.
- Ficha individual e painel de curral usam o texto Coleta. As transferências continuam nos componentes existentes.
- Truffle.asset foi renomeado pelo AssetDatabase para Bacon.asset, mantendo GUID e referências da cena/inventário. Identificador bacon, nome Bacon e ícone próprio; porcos continuam com ciclo de 150 s e rendimento-base 1, ajustado pelo bem-estar. Não há abate nem perda do animal.

## Causas do retorno

A sessão do usuário mostrava porcos fora do curral marcados como recolhidos, com mistura de rota de saída e espera por retorno. O comando anterior considerava somente o centro do corpo dentro da cerca e podia ativar confinamento prematuramente. Ao aguardar outro animal, mantinha partes da rota antiga. Além disso, usava um ponto intermediário na mesma coordenada Z do animal, que podia cair sobre uma cerca/curral vizinho.

O retorno agora cancela a rota anterior ao entrar em espera, só conclui imediatamente se o corpo inteiro estiver dentro, libera a navegação externa durante a aproximação e usa busca de caminho até os pontos reais de acesso.

Currais opostos compartilham o mesmo corredor. As reservas de passagem agora coordenam esses acessos próximos. Moradores que bloqueiam a rota caminham até um destino livre e aguardam a travessia; isso também considera o ponto final de saída. Nenhum animal é teleportado e as cercas continuam sendo obstáculos.

## Validação

Evidências em Reports/V38. O teste return.txt utiliza eventos de mouse na interface real e movimentação em tempo ativo: chama durante saída e após passeio. As tentativas anteriores que identificaram bloqueios foram preservadas como diagnóstico, sem serem apresentadas como aprovação.

Checkpoint anterior: .minifazendinha-checkpoints/before-v38-20261022.zip.

## Resultado final e limites

O teste de mouse do sino passou para as cinco espécies: interrupção da saída, retorno após passeio/espera no acesso e permanência dentro do curral. O teste registra explicitamente quantos animais estavam totalmente fora; não afirma que todos conseguiram sair simultaneamente. No cenário de liberar todos os currais juntos, ainda pode haver espera prolongada no corredor compartilhado. O comando de recolher foi validado também nessa situação.

Os popups passaram por verificação de tamanho, centralização, rótulos, transferência exata e recusa de coleta repetida. Foram capturados em 1920×1080, 1600×900 e 1366×768 e inspecionados visualmente. A coleta individual e pelo curral passou nas cinco espécies, incluindo bacon. Regredidos cliques no armazém/poço, scroll sem zoom, três cultivos por página, plantio por clique/arraste e colheita.

A navegação mantém busca por grade e distâncias de corpo existentes. O sino com a porteira fechada continua orientando a abri-la; não há travessia de cerca nem teleporte. Não foi criado salvamento de partida, abate ou nova economia.

Validação manual: reinicie Play Mode após recompilar; abra/feche a porteira, libere alguns moradores e toque o sino com a porteira aberta. Observe a passagem real e o estado recolhido. Clique no poço e em uma árvore pronta para conferir as caixas menores. Abra o curral dos porcos e a ficha de um animal: a produção é Bacon e a ação textual é Coleta.

Compilação final sem erros; consulta final do Console sem erros ou avisos atuais. Mouse e teclado físicos habilitados, mouse nativo atual e nenhum dispositivo virtual restante. Editor deixado fora de Play Mode, cena salva e execução em segundo plano restaurada ao valor original. Nenhum arquivo de fornecedor modificado por SHA-256.

Capturas: Screenshots/ContextUX/V38-well-*.png, V38-tree-*.png e V38-pigs.png. Lista de arquivos em Reports/V38/changed-files.txt. Checkpoint posterior em .minifazendinha-checkpoints/after-v38-20261022.zip.
