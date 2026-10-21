> ATUAL: interface contextual, zoom e estoque unico documentados em ContextUX/README.md. Plantio consome 1 do mesmo recurso que a colheita repoe; a foice so aparece no contexto de canteiros maduros.

# Estado atual: sistema de plantio implementado

A FarmMain agora inclui plantio, crescimento, colheita, sementes, produtos e barra de selecao. Veja [Planting/README.md](Planting/README.md) para componentes, configuracoes, caminhos reais e testes. Tempos editaveis em Crops/Carrot.asset (20 s), Crops/Corn.asset (35 s) e Crops/Wheat.asset (50 s). Nao ha salvamento de estado.

O conteudo abaixo documenta a etapa visual inicial e permanece como historico dessa montagem.

---
# MiniFazendinha - primeira cena visual

Abra Assets/MiniFazendinha/Scenes/FarmMain.unity e entre em Play Mode.

## Controles
- Arraste segurando o botao direito do mouse para deslocar a camera.
- Scroll para aproximar/afastar, ancorado na posicao do cursor.
- Zoom ortografico limitado entre 7 e 23. Na visao geral a camera fica centralizada; aproxime para explorar.
- Inclinacao de 60 graus e rotacao horizontal de 20 graus. O foco permanece dentro da fazenda e considera o tamanho da tela.
- Input System 1.20.1 ja existente, sem uso de UnityEngine.Input e sem dependencias adicionais.

## Cena
Terreno plano 40x40, caminhos, casa, celeiro, 12 canteiros de 5x5 em grade 4x3, 15 segmentos de cerca com passagem, 11 arvores, 12 arbustos, 16 flores, poco, carrinho de mao, regador, barril, caixotes e feno. As 54 mudas sao decoracao estatica, sem sistema de plantio.

Os modelos mantem a escala original. As instancias continuam vinculadas aos prefabs Pandazole; alinhamento vertical considera os limites dos renderizadores. Os materiais Standard originais receberam copias URP/Lit em Materials, usando as texturas originais. Nao aplique esses overrides aos prefabs de origem.

## Arquivos
- Scenes/FarmMain.unity: cena principal.
- Scripts/FarmCameraController.cs: camera em runtime.
- Scripts/Editor/FarmAssetInspection.cs: inspecao isolada de assets.
- Scripts/Editor/FarmSceneBuilder.cs: montagem e validacao; recusa sobrescrever FarmMain existente.
- Scripts/Editor/FarmCameraPlayTest.cs: teste de mouse sintetico no Play Mode, removido ao finalizar; nao entra no build.
- Materials/Grass_Mint.mat, Earth_Edge.mat, Path_Sand.mat, Pasture_Grass.mat: terreno e caminhos.
- Materials/Pandazole_Farm_URP.mat e Pandazole_Nature_URP.mat: copias compativeis com URP.
- Settings/FarmMain-Resources.txt: inventario com os caminhos reais e verificacao de integridade apos montagem.
- Settings/FarmMain-CameraTest.txt: resultado do teste da camera.
- Prefabs/: pasta preparada; esta etapa usa diretamente os prefabs de origem.

## Validacao
A montagem conferiu 96 arquivos originais/de configuracao sem alteracoes, 12 canteiros, 131 instancias de prefabs, zero scripts ausentes e zero materiais incompativeis.
O teste em Play Mode verifica avanco de frames, scroll, arraste, soltura do mouse, limites de zoom e limites de movimento, incluindo tela vertical.
Nao foram criados sistemas de economia, inventario, animais, multiplayer ou plantio funcional. SampleScene foi preservada. Nao foram instalados pacotes nem alteradas configuracoes globais para o visual.

Previa: Assets/Screenshots/MiniFazendinha-FarmMain.png.