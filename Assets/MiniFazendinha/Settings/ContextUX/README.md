# MiniFazendinha: interface contextual, navegacao e estoque unico

Versao atual de 21/10/2026. Cena: Assets/MiniFazendinha/Scenes/FarmMain.unity.

## Uso

O botao Armazem abre um painel lateral com os icones e a quantidade disponivel de cenoura, milho e trigo. A foice nunca aparece no armazem. O painel abre/fecha suavemente sem alterar a camera. X, Esc ou clique fora fecham os menus.

Clique brevemente em um canteiro sem ferramenta ativa. Se vazio, o menu oferece os cultivos disponiveis. Escolher um deles planta imediatamente no canteiro de origem e mantem o modo Plant ativo para novos cliques/arrastes. Se estiver crescendo, o menu mostra nome, segundos restantes e progresso. Apenas um canteiro maduro oferece o botao de foice. Seleciona-lo colhe o canteiro e permite continuar a colheita por arraste. Quando nao restam cultivos maduros, Harvest termina automaticamente. O desenho atual da foice foi mantido; a importacao do GLB foi cancelada conforme a ultima orientacao do usuario.

Com botao esquerdo pressionado sobre canteiro e ferramenta ativa, somente os canteiros atravessados sao afetados. A interpolacao a cada 4 pixels evita saltos em gestos rapidos; alvos visitados sao registrados por gesto. Plantios existentes e cultivos em crescimento sao preservados. UI tem prioridade e um gesto iniciado nela nao pinta a fazenda.

Arraste direito para mover a camera, independentemente da ferramenta. Arraste esquerdo iniciado em terreno livre tambem move. Clique e arraste sao separados por um limite de 6 pixels. Navegar fecha menus contextuais. Scroll aproxima/afasta suavemente, inclusive sobre os paineis. Zoom minimo 7, maximo 23; velocidade e suavizacao editaveis no FarmCameraController. A regiao sob o cursor permanece ancorada quando os limites da fazenda permitem. O ponto de interesse fica dentro dos limites configurados, com margem de 2 unidades. FarmMin/FarmMax podem ser ampliados no controller e no interactor.

O Canvas continua em Screen Space Overlay com CanvasScaler Scale With Screen Size (1440x900, match 0.5). Botoes e menus permanecem legiveis na tela ao dar zoom. O menu do canteiro acompanha sua projecao de mundo e se ajusta as bordas da tela; seu tamanho nao escala com o modelo 3D. O botao do armazem permanece no canto inferior direito, conforme a ultima orientacao de manter os controles visiveis durante o zoom.

## Estoque unico e multiplicacao

Existe UM saldo por CropDefinition. Nao existe saldo separado de sementes e de produtos colhidos.

Cada cultura comeca com 12 unidades. Plantar custa 1. Colher acrescenta YieldAmount ao mesmo saldo. Com a configuracao atual (3 por colheita), o ciclo e 12 -> 11 ao plantar -> 14 ao colher. Os recursos colhidos podem ser imediatamente replantados. Uma unica unidade plantada produz o necessario para plantar tres novos canteiros. Os tempos continuam editaveis nos assets Carrot/Corn/Wheat: 20/35/50 segundos. Nao ha salvamento: sair de Play Mode reinicia a sessao com 12 de cada.

FarmInventoryModel.Quantity/TrySpend/TryAdd operam sobre o mesmo contador, validam a cultura, evitam negativos e overflow. InitialCropStock.quantity tem FormerlySerializedAs("seeds") para preservar os 12 ja serializados na cena. FarmPlotModel continua reservando o canteiro antes do debito e esvaziando antes do credito, bloqueando duplicacoes por callbacks reentrantes. Duracao e producao sao capturadas por ciclo, como antes.

## Arquitetura e arquivos

Componentes existentes refatorados em Scripts/Planting:

- FarmInventoryModel.cs e FarmInventory.cs: saldo unico por cultura e notificacoes de estoque.
- FarmPlotModel.cs: usa as operacoes unificadas; preserva estados e validacoes atomicas. FarmPlot.cs continua fornecendo o relogio e as operacoes do canteiro.
- FarmPlotInteractor.cs: unico controlador None/Plant/Harvest, Input System, raycast apenas dos colliders dos canteiros, hover, gestos interpolados e comandos. Menu contextual tem estado explicito e aplica a ferramenta no canteiro que originou a escolha.
- FarmHUD.cs: vinculos de interface, saldo, selecao e feedback; foice global retirada. Nomes serializados antigos como seedButtons foram mantidos para preservar referencias, mas representam botoes de cultivos e nao um segundo tipo de estoque.
- FarmPlotView.cs e CropDefinition.cs: visuais agrupados e tres estagios existentes preservados. Descricao dos modelos compostos em ../DragTools/README.md (historico visual).

Componentes novos:

- FarmWarehousePanel.cs: painel lateral, animacao, botao de abertura/fechamento e bloqueio de raycasts quando oculto.
- FarmContextMenu.cs: apresenta opcoes conforme estado real, ancora ao canteiro, atualiza progresso e limita a posicao a tela.
- FarmCameraController.cs (Scripts): navegacao, prioridade do gesto, zoom suavizado e limites. Corrigida a conversao do scroll: Input System 1.20.1 usa UniformAcrossAllPlatforms por padrao, com deltas normalizados. O codigo anterior dividia novamente por 120, tornando o zoom quase imperceptivel. ScrollSteps agora respeita o modo configurado; a divisao por 120 so se aplica ao modo nativo no Windows.

Icones provisiorios locais em UI/Icons: Carrot.png, Corn.png, Wheat.png, Sickle.png, Warehouse.png e RoundedCard.png. Foram criados por codigo, pois nao havia icones agricolas adequados entre as imagens existentes. Sao substituiveis e nao utilizam emojis. Os arquivos originais da Pandazole nao foram alterados. Nenhum pacote externo foi instalado.

A cena reutiliza o mesmo Canvas, EventSystem/InputSystemUIInputModule e botoes de cultivos. A antiga SeedBar/PlotStatus permanece desativada apenas como estrutura legada com referencias historicas; nao ha barra inferior fixa em uso. O botao antigo de foice fica inativo na estrutura legada, fora da hierarquia do armazem. O unico acesso de UI a Harvest e o menu contextual de um canteiro maduro.

## Checkpoints

Nao havia repositorio Git configurado; nenhum repositorio foi inicializado. Copias recuperaveis de MiniFazendinha:

- .minifazendinha-checkpoints/before-context-ux-20261021/
- .minifazendinha-checkpoints/before-unified-stock-20261021/

FarmMain mantem o caminho/GUID e seus 12 canteiros, colliders e vinculos ao prefab original Env_FarmLand_03. O saldo inicial foi migrado para quantity=12 por cultura. As alteracoes de inventario sao intencionais e substituem as antigas afirmacoes de arquivos de negocio inalterados nas documentacoes historicas.

## Validacao real

ContextUXTest.txt registra a suite executada em Play Mode com eventos sinteticos do Input System: abrir/fechar armazem por botao/X/Esc/clique fora, contexto vazio/crescente/maduro, plantar no alvo de origem, culturas em quaisquer 12 canteiros, trajetos 1/3/5/12, passagem rapida, retorno, estoque insuficiente, alvos ocupados, troca de modo e foco, bloqueio de UI, colheita mista/duplicada, navegacao esquerda/direita, scroll normalizado +1 e -1, scroll sobre UI, limites, tamanho constante e posicionamento do contexto. Verifica que o armazem nao oferece foice nem quando ha colheitas prontas. Fixtures de misturas usam timestamps do modelo para preparar estados, sem modificar duracoes ou timeScale.

PlantingRulesTest.txt registra o saldo unico 12 -> 11 -> 14 -> 13, reutilizacao da colheita para novos plantios, falta de estoque, duplicacoes, reentrancia, overflow e quarta cultura apenas por dados. SceneValidation.txt confere 12 MeshColliders originais, referencias, estoque inicial, um inventario/interactor/HUD/EventSystem e ausencia de scripts faltantes.

Capturas reais renderizadas pela camera e pelo Canvas, inspecionadas visualmente, em Screenshots/ContextUX/: 01-Closed, 02-Warehouse, 03-Warehouse-16x10, 04/05-Context-Empty, 06-Planting, 07-Context-Growing, 08-Context-Ready, 09-Harvesting, 10/11-Overview-Warehouse, 12-Overview-Closed e 13-Zoomed-Warehouse, todas .png. As capturas 1600x900 (16:9) e 1600x1000 (16:10) verificam composicao e limites do Canvas via render offscreen; nao equivalem a testes fisicos em dois monitores.

Utilitarios somente de Editor: FarmContextUXUpgrade (migracao ja aplicada e finalizacao segura em Edit Mode), FarmContextUXTests, FarmContextUXReview, FarmTestInputScope. O scope de testes clona temporariamente o roteamento do Input System e restaura settings/dispositivos; nao altera o asset global. FarmDragToolsTests encaminha seu menu para a suite atual. FarmPlantingTests foi adaptado ao novo armazem e saldo unico.

Para repetir: Play Mode novo e nao pausado > MiniFazendinha/Context UX/Test In Play Mode. A suite pausa para revisao. Regras: MiniFazendinha/Planting/Test Business Rules. Ciclo natural: Test Full Cycle In Play Mode, aguarde READY_FOR_VISUAL_REVIEW no relatorio Library/MiniFazendinha-PlantingPlayTest.txt e execute Continue Test Harvest. Nao reaplique as migracoes.

Validacao manual restante: sensibilidade com o mouse fisico do usuario no Game View e em build, alternancia real de foco entre janelas, conforto em outras resolucoes e sessao prolongada. Os testes de mouse acima usam eventos sinteticos reais do Input System, nao operacao fisica do mouse. Sem animais, loja, moedas, multiplayer ou salvamento.
## Resultado final adicional

Ciclo natural PlantingPlayTest.txt: PASS, timeScale=1, crescimento observado em 20,01/35,01/50,01 segundos. A colheita levou o saldo de cada cultura a 14; o replantio consumiu 1 trigo, deixando 13. Algumas mensagens de asserts antigos ainda usam a palavra seed, mas as operacoes e comparacoes usam somente Quantity. Console final: zero erros e zero avisos presentes; compilacao concluida sem falha. Veja ConsoleValidation.txt para distinguir o historico da migracao da verificacao atual. OriginalIntegrity.txt registra a comparacao dos arquivos protegidos.
