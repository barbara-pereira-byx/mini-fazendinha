> HISTORICO: a interface e o estoque foram atualizados. Consulte ../ContextUX/README.md. A foice agora existe somente no contexto maduro; o saldo de cultivo e unico e reutilizavel, e o scroll normalizado foi corrigido.

# Ferramentas de plantio, colheita e navegacao

Cena: Assets/MiniFazendinha/Scenes/FarmMain.unity. Atualizacao de 21/10/2026.

## Como jogar

O menu inferior inicia oculto. Clique brevemente em qualquer canteiro para abrir; esse primeiro clique nao planta. Escolha Cenoura, Milho, Trigo ou FOICE. A ferramenta fica selecionada ate trocar ou fechar. Clique ou segure o botao esquerdo e atravesse somente os canteiros desejados. Sementes plantam apenas em vazios; a foice colhe apenas maduros. Clique fora da area dos canteiros, use X ou Esc para fechar e cancelar. Arraste direito tambem fecha a ferramenta e navega.

Arraste com o botao direito para mover a camera. O esquerdo tambem move quando nao ha ferramenta ativa ou quando o arraste comeca fora dos canteiros. Com uma ferramenta ativa, o esquerdo sobre um canteiro aplica a ferramenta. Movimento acima de 6 pixels distingue arraste de clique: navegar nao abre o menu ao soltar. Scroll aproxima/afasta entre 7 e 23; o zoom acompanha o ponto do cursor. O foco fica dentro dos limites da fazenda com margem de 2 unidades, permitindo navegar mesmo no zoom mais afastado. A inclinacao existente da camera (38,37 graus) e o zoom padrao 23 foram preservados.

O contorno azul indica hover valido, o contorno discreto indica alvo indisponivel e o dourado indica maduro. Nome, segundos restantes e progresso aparecem somente ao passar sobre uma cultura. A colheita tem pulsacao, particulas e texto com a quantidade efetivamente creditada. Contagens e selecao destacada ficam no menu contextual.

## Componentes reutilizados

- FarmPlotInteractor: controlador explicito None/Plant/Harvest, menu contextual, Input System, hover e arraste. Interpola o caminho a cada 4 pixels e registra canteiros ja visitados por gesto. O raycast consulta diretamente apenas os colliders dos 12 FarmPlot registrados; decoracoes nao interceptam. UI bloqueia inicio e interrompe gestos que a atravessam. Soltura, troca de ferramenta e perda de foco cancelam o arraste.
- FarmPlotModel, FarmInventoryModel, FarmPlot e FarmInventory: arquivos preservados byte por byte. Continuam responsaveis pelas regras, estados, debito unitario e credito atomico. Plantio duplo e colheita duplicada permanecem bloqueados.
- CropDefinition: mantem identificadores, duracoes e producao; acrescenta IntermediateVisual. Assets Carrot/Corn/Wheat em Settings/Crops conservam 20/35/50 segundos e producao 3. Nenhuma cultura depende do indice do canteiro.
- FarmPlotView: agrupamentos em grade, estagios inicial/intermediario/maduro, alinhamento ao solo, hover e efeitos temporarios. Progresso permanente no mundo desativado.
- FarmHUD: barra contextual com sementes, foice, X, estoque e produtos; painel de hover sem bloquear raycasts; mensagens e quantidade colhida. Usa o Canvas, TMP e InputSystemUIInputModule existentes.
- FarmCameraController: arraste esquerdo/direito, distincao de gesto, prioridade das ferramentas e limite de foco que permite movimento no zoom maximo.

## Assets criados e utilizados

Nove prefabs compostos em Assets/MiniFazendinha/Prefabs/Crops/: Carrot_Initial, Carrot_Intermediate, Carrot_Mature, Corn_Initial, Corn_Intermediate, Corn_Mature, Wheat_Initial, Wheat_Intermediate e Wheat_Mature, todos com extensao .prefab.

Cenoura e milho usam grades 4x4 (16 plantas/canteiro); trigo 5x5 (25). Escalas ajustadas individualmente aos canteiros de 5x5, com margem de pelo menos 0,08 unidade e base visual a 0,61 acima da origem. O teste verifica limites e altura de todos os 12 agrupamentos. Materiais novos: Materials/Crop_Leaves.mat e Crop_YoungWheat.mat. Materiais URP existentes reutilizados.

Recursos reais originais referenciados:

- Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/food_Carrot.prefab
- Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Kitchen Food/Prefabs/Food_Corn.prefab
- Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Env_Wheat.prefab
- Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Env_GrassPlant_06.prefab
- Os 12 canteiros mantem o vinculo a Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Env_FarmLand_03.prefab.

Limite do pacote: nao foi encontrado um pe completo de milho. O visual provisoriamente combina a espiga real, folhas existentes e um caule simples. Cenouras combinam o produto e folhas; trigo combina modelos existentes. Os tres estagios sao composicoes locais, sem afirmar que sao prefabs originais de estagios Pandazole. Nenhum original foi sobrescrito.

## Checkpoint e preservacao

Sem repositorio Git configurado. Checkpoint: .minifazendinha-checkpoints/before-drag-tools-20261021/ (copia de MiniFazendinha e hashes). FarmMain mantem caminho/GUID, 12 canteiros e colliders, inventario e camera existente. Hashes conferem 6887 arquivos protegidos da Pandazole, SampleScene, ProjectSettings e manifests de pacotes. Os quatro componentes de negocio acima permanecem identicos. A alteracao do script da camera foi solicitada nesta etapa. Nenhuma dependencia foi instalada. A opcao temporaria de testes runInBackground foi restaurada ao arquivo original.

## Validacao executada

- DragToolsTest.txt: Play Mode com gestos por 1/3/5/12 canteiros, interpolacao rapida, retorno pelo mesmo alvo, estoque insuficiente, ocupado, troca de ferramenta, cancelamento por foco/soltura, raycast limitado a canteiros, cada cultura em todos os 12, colheita mista e repeticao sem duplicacao. Inclui eventos sinteticos reais do Input System nos botoes e no arraste. Verifica menu inicialmente oculto, clique curto para abrir sem plantar, selecao, clique fora e X. Confere tres estagios, numero de plantas, limites e altura de todos os agrupamentos, hover e desaparecimento do painel.
- CameraRegressionTest.txt: Play Mode, scroll, zoom minimo/maximo, arraste direito, arraste esquerdo no zoom maximo, soltura de ambos, limites e viewport vertical.
- PlantingRulesTest.txt: regras de negocio, callbacks reentrantes, bloqueio de duplicacoes e quarta cultura criada apenas por dados.
- PlantingPlayTest.txt: ciclo natural a timeScale=1, tres culturas observadas em 20,01/35,01/50,01 segundos de jogo, colheita e replantio, estoque e UI corretos. A preparacao abre explicitamente o menu; o teste de gestos verifica a abertura por clique. As fixtures de colheita mista usam o relogio do modelo para preparar maduros/crescentes, sem reduzir tempos dos assets.
- SceneValidation.txt: referencias, 12 MeshColliders e vinculos aos canteiros originais, estoque inicial 12 por cultura, um inventario/interactor/HUD/EventSystem, ausencia de scripts faltantes.
- OriginalIntegrity.txt e ConsoleValidation.txt: preservacao e estado final do Editor.

Capturas reais pela camera no zoom 23: Screenshots/MiniFazendinha-DragTools-Review.png (menu aberto) e Screenshots/MiniFazendinha-ContextMenu-Closed.png (menu fechado). Sao fixtures de revisao em Play Mode; nao ficam gravadas como cultivos na cena.

Utilitarios novos apenas de Editor: FarmDragToolsUpgrade (migracao protegida, ja aplicada), FarmDragToolsTests, FarmDragReview e FarmTestInputScope (clone temporario de roteamento de input, restaura settings/dispositivos ao concluir). FarmPlantingTests e FarmCameraPlayTest foram adaptados aos novos controles. Todos em Scripts/Editor; excluidos do build.

Para repetir: novo Play Mode > MiniFazendinha/Drag Tools/Test Gestures In Play Mode. O teste pausa para revisao visual. Camera: MiniFazendinha/Test Camera In Play Mode em Play Mode nao pausado. Regras: MiniFazendinha/Planting/Test Business Rules. Ciclo natural: Test Full Cycle In Play Mode, aguarde READY_FOR_VISUAL_REVIEW no relatorio Library/MiniFazendinha-PlantingPlayTest.txt e execute Continue Test Harvest. Nao execute a migracao novamente.

Validacao manual restante: conforto com mouse fisico, tecla Esc, diferentes resolucoes/proporcoes e comportamento ao alternar janelas durante um gesto real. Eventos sinteticos e verificacao visual nao substituem essa avaliacao de uso. Nao foram adicionados salvamento, loja, moedas, animais, multiplayer nem producao.