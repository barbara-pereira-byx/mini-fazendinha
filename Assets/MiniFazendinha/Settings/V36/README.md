# MiniFazendinha V3.5 / V3.6 — revisão de interfaces e recursos

Cena: `Assets/MiniFazendinha/Scenes/FarmMain.unity`.
Editor utilizado: Unity 6000.6.7f1, URP, acesso via Unity CLI.

## Experiência de uso

- A aba lateral chama-se **Cultivos**. **Horta** contém somente os nove cultivos plantáveis; água, leite, ovos, lã e frutas armazenadas não se misturam com os botões de plantio. **Pomar** preserva a colocação de árvores já existente.
- Clicar no **Celeiro**, o prédio branco com telhado azul escuro, abre o armazém completo. O grid mostra ícone, nome e quantidade de todos os itens registrados no inventário, inclusive itens com saldo zero. Não inicia plantio.
- O armazém mostra capacidade efetiva de 2.000 unidades, ocupação e espaço livre. `FarmInventory.storageCapacity` permite ajustar o limite. A entrada de novos recursos respeita esse limite; não se inventou uma barra sem efeito no modelo.
- A roda sobre as listas pertence à interface. Fora da interface, o zoom ancorado no cursor permanece disponível. A rolagem usa tempo não escalado, suavização exponencial, limites, inércia de arraste e uma barra visível.
- O menu do canteiro contém exatamente três cartões por página, em uma linha. As páginas são calculadas pelo catálogo; as setas são sprites e ficam indisponíveis nos extremos.

## Currais e animais

Os cinco currais por espécie existentes foram preservados. Cada um tem cerca, entrada, comedouro, bebedouro e uma placa de madeira com retrato da espécie. As identificações textuais flutuantes anteriores foram desativadas. Não foram alterados modelos ou materiais originais dos fornecedores.

O painel do curral fica à direita, com largura de 300 unidades do Canvas existente (aproximadamente 380 pixels em Full HD), ocupação, renomeação, controles de porteira, sino, moradores reais, espaços vazios apenas informativos, médias de bem-estar, recipientes e produtos compatíveis com a espécie. O conteúdo excedente tem rolagem interna. Cartões abrem a ficha do residente; X/Esc nessa ficha retorna ao curral selecionado. A renomeação valida 1–24 caracteres, sem mudar identificadores.

A porteira tem estado lógico e visual. Fechar impede novas travessias, inclusive para quem já estiver fora. O sino não atravessa uma porteira fechada: a interface orienta abri-la. Enquanto uma passagem comandada estiver em curso, o fechamento é recusado com orientação curta. Só os residentes vinculados recebem o comando.

O retorno tem prioridade sobre o passeio. A saída usa a entrada física, um ponto central no corredor e um ponto que libera a boca da porteira. A entrada faz aproximação alinhada ao corredor. Há reserva da passagem por curral e reserva exclusiva por recipiente. Não foi introduzido teleporte como movimentação normal. O mecanismo anterior de recuperação de contato apenas caminha para fora de contatos rasos.

A navegação usa o raio configurado também para evasão de outros animais; antes usava fatores diferentes (65% para si e 50% para o vizinho). Os pontos iniciais foram separados, as aproximações aos recipientes afastadas e a grade refinada de 0,40 para 0,25 m. Isso permite representar o corredor existente sem aumentar simplesmente o tamanho dos currais.

## Abastecimento e coleta

Clicar diretamente no comedouro/bebedouro tem prioridade sobre a área do curral. O popup oferece −, +, máximo, quantidade, estoque do armazém, capacidade, confirmar e cancelar. A escolha inicial é uma unidade, limitada ao saldo e ao espaço.

Ao confirmar, os limites são recalculados. O modelo faz uma única transferência inteira, com bloqueio contra reentrada; ambos os saldos são atualizados antes dos eventos. Cancelar não altera recursos. Para consumir a última unidade de um cultivo, a primeira confirmação apenas pede confirmação adicional explícita. Água não recebe esse aviso de replantio.

O efeito de consumo usa a quantidade transferida e o ícone do recurso, próximo ao ponto selecionado. O nível dos recipientes tem uma representação própria, sem editar os modelos comerciais.

O poço mantém **10 unidades por coleta** e **30 segundos de recarga**, configuráveis em `WaterWellController`. A coleta credita água no inventário e bloqueia reentrada e repetição durante a recarga. A mesma definição de água abastece os bebedouros.

## Necessidades e produção

A causa da queda rápida era a duração única de 55 segundos. Agora cada `AnimalDefinition` possui `nutritionDuration` e `hydrationDuration`, ambos configurados em **1.800 segundos**. O início da sessão dá barras cheias. O relógio é o de jogo (`Time.timeAsDouble`), portanto a pausa do tempo de jogo também pausa as necessidades.

A necessidade interna é independente das unidades do recipiente. Uma chegada válida para comer/beber consome uma unidade e recupera a barra; animações e frames não debitam continuamente o estoque. O recipiente permanece reservado durante a ação. Cuidar aumenta felicidade em 0,10, com intervalo de 30 segundos, ambos configuráveis. A felicidade também se aproxima lentamente da condição de alimentação, hidratação e saúde.

Saúde: saudável, atenção quando alimentação ou hidratação estão em 20% ou menos, e estado explícito de doença. Não há doença aleatória nem morte. `Treat()` é o ponto de extensão funcional para um futuro serviço de tratamento; não se criou construção nem botão fictício de medicamento.

Produção por ciclo:

| Espécie | Rendimento base | Período |
|---|---:|---:|
| Galinha | 1 ovo | 28 s |
| Vaca | 2 leites | 45 s |
| Ovelha | 2 lãs | 60 s |
| Porco | sem produto configurado | 40 s, sem emissão |
| Pato | sem produto configurado | 33 s, sem emissão |

Multiplicadores editáveis por espécie: saudável/feliz 1,00; intermediário 0,75; atenção 0,50; doente 0,25. Saudável/feliz exige felicidade >=70% e necessidades >=40%. Os demais animais saudáveis usam o nível intermediário.

O rendimento fracionário é acumulado entre ciclos: em cada ciclo soma-se `rendimentoBase × eficiência`, emite-se o piso inteiro e conserva-se a fração. Por exemplo, um ovo a 25% rende um ovo a cada quatro ciclos. Assim não se arredonda todo ciclo para um produto e não se produz quantidade negativa. A coleta zera o coletável antes de notificar o inventário e restaura em caso de falta de capacidade. Conserva-se o limite anterior de 99 produtos coletáveis por animal.

## Ícones e árvores

Os ícones ilustrados de todos os produtos foram reutilizados. A biblioteca compartilhada em `Resources/FarmUI` inclui cartões, retratos renderizados e sprites próprios de ações; não utiliza emojis. `FarmWidgets` concentra botões, tooltips, barras e listas, e `FarmManagementView` apresenta os dados dos modelos existentes.

As três árvores apontavam para materiais URP sem `_BaseMap`. Foram criados prefabs e materiais próprios em `MiniFazendinha/Prefabs/Trees` e `MiniFazendinha/Materials/Trees`, associados às texturas originais de maçã, limão e laranja. Os FBXs, texturas, materiais e importadores dos fornecedores não foram editados. O preview do plantio conserva a paleta, com a coloração translúcida de validade já existente.

## Testes e artefatos

Os resultados efetivos ficam em `Reports/V36`. Cada relatório distingue callbacks, eventos de ponteiro, observação em tempo real e simulação analítica do relógio. Não se aguardaram 30 minutos reais: os pontos de 30, 60, 300 e 1.800 segundos foram simulados nos getters de necessidades; houve também observação de 30 segundos ativos com quatro animais.

| Tempo a partir de 100% | Alimentação / hidratação sem reposição |
|---|---:|
| 30 s | 98,333% |
| 60 s | 96,667% |
| 300 s | 83,333% |
| 1.800 s | 0% |

A navegação foi acompanhada com duas vacas saindo e retornando pelo acesso, e com quatro galinhas caminhando e descansando. Distâncias mínimas medidas: 1,506 m entre vacas (soma dos raios 1,50 m) e 0,800 m entre galinhas (soma 0,80 m).

As capturas em `Screenshots/ContextUX/V36-*` são renderizações reais da cena e do Canvas. Os painéis foram renderizados em 1920×1080, 1600×900 e 1366×768, com asserções de limites. A captura das árvores usa instâncias temporárias de validação, removidas sem salvar a cena.

## Validação manual recomendada

1. Abra `FarmMain`, entre em Play e abra Cultivos. Role até o último cultivo e verifique que o zoom só responde fora da lista. Alterne Horta/Pomar e cancele o preview com Esc.
2. Clique no prédio branco com telhado azul escuro. Confira produtos, imagens, quantidades e espaço. Role a lista até os ovos, no fim do catálogo.
3. Clique em um canteiro vazio. Percorra as três páginas; plante, arraste para outros canteiros e use a foice após o crescimento.
4. Clique em um curral, em seu morador e no lápis. Renomeie e teste nome vazio/limite, X, Esc, clique fora, retorno ao painel anterior e tooltips.
5. Abra a porteira, libere e toque o sino. Confirme a travessia real, fechamento da porteira e recusa do retorno enquanto fechada. Observe também animais grandes mudando de direção em lotações maiores.
6. Colete água no poço, abra um bebedouro e escolha uma quantidade. Cancele, confirme cinco unidades, tente máximo e teste a confirmação de uso do último cultivo em um comedouro.
7. Confira os saldos no armazém e os níveis dos recipientes. Cuide de um animal e confira felicidade, produção, coleta e cooldown.
8. Repita o uso interativo nas três resoluções. As capturas e asserções de layout não substituem avaliação manual de conforto, hover, digitação e todos os cruzamentos possíveis.

## Limites conhecidos

Não havia sistema de salvamento de partida: renomeações e estoques mudam durante a sessão, como os dados anteriores. Não foi criado um sistema paralelo de persistência. O checkpoint é recuperação do projeto, não save de jogador.

A saúde é uma base simples e verificável; não existe simulação clínica ou fabricação de medicamentos. Porcos e patos continuam sem produção automática configurada. Não se adicionou carne ou produtos incompatíveis para preencher a interface.

A evasão utiliza raios aproximados dos corpos. Foram testadas as lotações descritas, não todas as combinações de rotação, espécie e lotação. As animações existentes foram preservadas; não houve uma revisão completa de todos os clips/Animator nesta etapa.

Os testes antigos em `Polish` dependem do painel textual anterior e não são a suíte desta versão. Use os menus `MiniFazendinha/V3.6` e os relatórios correspondentes. Execute as suítes de navegação em uma sessão nova, antes das suítes que manipulam estoques para teste.

## Recuperação

Checkpoint anterior a qualquer alteração desta etapa: `.minifazendinha-checkpoints/before-v36-20261022.zip`. Preserva `MiniFazendinha`, `Packages`, `ProjectSettings` e relatórios anteriores. Para recuperar, feche o Editor, faça uma cópia do estado atual e restaure cada pasta na posição correspondente (`MiniFazendinha` dentro de `Assets`). Não foi feito reset ou commit de alterações anteriores do usuário.

A lista completa de arquivos comparada ao checkpoint está em `Reports/V36/changed-files.txt`; a verificação dos fornecedores está em `Reports/V36/vendor-differences.txt`.

## Resultado final registrado

Os relatórios 01–09 em Reports/V36 finalizaram com sucesso. A compilação final não apresentou falhas e o console final registrou zero erros e zero avisos. A colheita com foice foi executada com eventos reais de ponteiro, além do plantio por arraste. Coleta de quatro produtos de um animal e dez unidades de água resistiu a reentrada e repetição. A paginação foi verificada com catálogos temporários de 1, 2, 3, 4, 9 e 10 cultivos; o catálogo original foi restaurado.

Após recompilar scripts durante Play Mode, reinicie a sessão antes de validar a interface dinâmica. Os testes finais foram feitos em sessões iniciadas a partir da cena salva.
