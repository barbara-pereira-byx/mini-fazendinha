# MiniFazendinha V2 — protótipo incremental

Atualizado em 21/10/2026. Cena: `Assets/MiniFazendinha/Scenes/FarmMain.unity`.

## Pacote Voxel Farm e licença

Os arquivos foram organizados em `Assets/ThirdParty/VoxelFarm` preservando as pastas originais de FBX, animações, texturas, OBJ e MTL. A auditoria `VoxelFarmAssetAudit.txt` registra nomes, caminhos e metadados. Os materiais importados já usam `Universal Render Pipeline/Lit`; fontes FBX/OBJ/PNG não foram modificadas.

**Licença pendente.** Não localizei LICENSE, LICENCE, README ou EULA na pasta extraída nem no ZIP disponível em Downloads. Não presumi permissão de uso, modificação ou redistribuição. O pacote permanece local; não fiz push para repositório remoto.

## Animais e abrigos

A cena tem 10 animais na pastagem delimitada: 2 vacas, 3 galinhas, 2 ovelhas, 2 porcos e 1 pato. O celeiro existente é o abrigo; dois recursos importados montam o comedouro e o bebedouro. Animais têm identificadores, configuração reutilizável por espécie, produto e tempos editáveis, alimentação leve, produção, descanso, passeio, retorno ao abrigo e soltura. O menu contextual permite consultar animal, recolher/soltar o grupo, coletar produtos disponíveis e reabastecer pontos de cuidado. A produção entra no inventário genérico existente. Leite, ovos e lã começam em zero; porcos e patos ainda não produzem itens.

Scripts em `Scripts/Animals`: `AnimalDefinition`, `AnimalStateMachine`, `FarmAnimalNavigation`, `FarmAnimalController`, `AnimalShelter`, `FeedingPoint`, `FarmAnimalRegistry` e `FarmAnimalContextUI`. Prefabs e controllers de animação estão em `Prefabs/Animals` e `Settings/V2/Animals`.

Os FBXs confirmam rig Generic. Caminhos dos clips de vaca, galinha, ovelha, porco e pato correspondem às hierarquias dos respectivos modelos. Galinhas também têm Peck. A vaca tem um clip de Walk cujos bindings coincidem, mas o FBX de animação exibe uma malha de prévia com outro nome; a semelhança visual da caminhada ainda exige inspeção. Goat, Rooster, Bull e Mallard não foram colocados; Bull só tem Idle, e Goat/Rooster/Mallard não têm clipes separados confirmados.

## Culturas e armazém

Cenoura, milho e trigo permanecem intactos. Foram acrescentadas Batata, Abóbora, Tomate, Morango, Mirtilo e Amora usando modelos reais em `Assets/ThirdParty/VoxelFarm/FBX/Environment`. Cada CropDefinition usa estoque comum de 12, yield 3 e duração de 22–34 segundos editável. `FarmWarehouseCatalog` cria a lista rolável de culturas com saldo e seleção e uma categoria de árvores frutíferas.

Para as culturas novas, três fases visuais reutilizam o mesmo modelo em escala crescente; não foram encontrados modelos próprios para todas as fases. Os ícones novos são amostras de cor temporárias, aguardando uma rodada de arte ilustrada.

## Árvores frutíferas

Foram criadas definições de macieira, laranjeira e limoeiro com FBXs reais, tempos configuráveis, escala de crescimento, intervalo e quantidade de frutas. A categoria do armazém inicia a prévia de posicionamento, verde/vermelha conforme os obstáculos, margens, canteiros, caminhos, cercas, árvores e construções. Após colocar, `FruitTree` cresce, produz, abre menu contextual para colher e reinicia a produção mantendo a árvore.

Scripts: `FruitTreeDefinition`, `FruitTree` e `FarmTreePlacementController`. O posicionamento e o ciclo foram compilados, mas ainda precisam de teste manual de validade em cada tipo de terreno, transparência da prévia, colheita e ciclo completo. O protótipo não cobra muda e árvores colocadas em Play Mode não são salvas ao sair. Não há persistência nesta tarefa.

## Validação nesta sessão

- Unity MCP confirmou o Editor principal e a cena ativa FarmMain.
- Auditoria localizou 54 FBXs no pacote, os nove modelos animais esperados, os modelos de cultivo/árvore usados e clipes confirmados.
- Unity recompilou os scripts V2 sem falha.
- Utilitários Editor criaram prefabs/configurações e salvaram a cena FarmMain.
- Play Mode iniciou; renderizei uma captura da fazenda e confirmei que a cena carrega com animais, cercado, canteiros e UI. Captura: `Captures/FarmMain-v2-playtest.png`.
- Simulador de ponteiro executou eventos, mas não consegui confirmar visualmente que o Game View abriu o armazém com os cliques enviados; não declaro a interface clicável como aprovada.
- Não executei validação completa de navegação sem colisão, entrada/saída do abrigo, alimentação, produção/coleta, árvores ou culturas adicionais. Esses cenários precisam de testes manuais no Editor.
- Não validei outras proporções de tela na V2.

## Ferramentas e próximos limites

Utilitários de Editor: `Scripts/Editor/FarmV2AssetAudit.cs` e `FarmV2Setup.cs`; menus `MiniFazendinha/V2/Audit Voxel Farm Assets`, `Build animal prototype`, `Build crop catalog` e `Build fruit tree prototype`.

Não foram adicionados animais extras à cena, reprodução, doenças, morte, loja, moedas, multiplayer, missões, economia complexa ou salvamento. A compra/estoque de mudas de árvore e ícones ilustrados ficam para uma próxima iteração.
