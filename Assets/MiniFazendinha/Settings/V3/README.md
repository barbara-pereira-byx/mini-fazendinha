# MiniFazendinha V3 — estado do protótipo

## Estado e validação

Projeto: Unity 6.6.7f1, URP. Cena: `Assets/MiniFazendinha/Scenes/FarmMain.unity`.

Esta entrega registra a etapa **V3.1 — animais e recursos**, ainda como protótipo incremental. V3.2 (economia/loja), V3.3 (posicionamento e edição) e V3.4 (expansão e salvamento) não estão implementadas.

A cena existente foi preservada e atualizada com cinco áreas por espécie, usando os dez animais já presentes. A compilação foi verificada no Unity Editor e concluiu sem erros. Em Play Mode, um teste direto verificou: o poço concede 10 águas e não concede novamente antes da recarga; o cocho aceita milho do inventário, consome o estoque abastecido e rejeita consumo do item errado. O registro original do teste está em `Settings/V3_CareTest.txt`.

Interação visual por mouse, animações de alimentação, navegação e colisão entre os currais, ciclo completo de produção animal e reabertura da cena precisam de validação manual no Editor. A correção de paleta dos animais foi aplicada aos materiais e renderizadores, mas a aparência final de todas as espécies em close-up ainda precisa de inspeção visual. Não há captura confirmada de alimentação ou colheita animal nesta etapa.

## Implementação V3.1

- `Scripts/Animals/WaterWellController.cs`: produção limitada de água, coleta única por ciclo e recarga configurável.
- `Scripts/Animals/FeedingPoint.cs`: capacidade configurável, item aceito, abastecimento a partir de `FarmInventory` e consumo por eventos.
- `Scripts/Animals/AnimalDefinition.cs`: referências de alimento e água configuradas por espécie.
- `Scripts/Animals/FarmAnimalController.cs`: hidratação e pausa da produção enquanto faltar alimento ou água; não há morte nem penalidade permanente.
- `Scripts/Animals/FarmAnimalContextUI.cs`: informação de inventário/nível do cocho, poço e estado básico do animal.
- `Scripts/Editor/FarmV3ResourceSetup.cs`: configuração dos recursos, poço e materiais URP dos cochos.
- `Scripts/Editor/FarmV3PenSetup.cs`: montagem repetível dos cinco currais na cena.
- `Scripts/Editor/FarmV3MaterialRepair.cs`: atribuição de materiais próprios URP às malhas dos animais.
- `Materials/Animals/`: cópias de materiais compatíveis com URP; materiais do fornecedor não foram sobrescritos.
- `Settings/Inventory/Resources/Water.asset`: item de água no inventário único.

Alimentos configurados com itens existentes: vacas/trigo, galinhas/milho, ovelhas/trigo, porcos/abóbora, patos/milho. Os porcos e patos ainda não têm produto nesta etapa. Os cochos começam vazios, com capacidade de 20; o poço começa com 10 águas e recarrega em 30 segundos. Esses valores ficam configuráveis nos componentes/definições.

## Recursos e integridade

Modelos originais utilizados: `Assets/ThirdParty/VoxelFarm/FBX/Animals/` (animais existentes) e estruturas/prefabs Pandazole já presentes na cena. As texturas originais de paleta foram reutilizadas em materiais novos. As fontes PNG/FBX originais não foram editadas. Os importadores de textura foram configurados sem mipmaps, com filtro point e wrap clamp para preservar paletas UV.

A licença/termos de redistribuição do Voxel Farm não foram encontrados no pacote local e permanecem pendentes. Não presumir permissão para redistribuir os arquivos de fornecedor. O repositório já ignora os arquivos-fonte do pacote; um checkout exige que o usuário obtenha e importe os assets conforme a licença aplicável.

## Limitações conhecidas

- Os abrigos são representações visuais simples reaproveitando recursos presentes; interiores e animações de entrada não foram implementados.
- A separação por cerca foi montada no espaço limitado do pasto atual; o caminho dos animais e as colisões devem ser observados manualmente.
- O sistema visual de menus é protótipo; não há confirmação de todos os fluxos por mouse.
- Economia, loja, vendas, compras, posicionamento/edição, expansão de terreno e persistência ainda não foram implementados.
- V3.1 não possui commit/checkpoint novo nesta documentação. O baseline Git anterior é `v2.0-prototype`; a branch V3.1 será registrada após validação e revisão do escopo.

## Recursos futuros

Alimento de cavalo (cenoura) é apenas uma preparação para espécie futura; cavalo ainda não foi adicionado. A água usa um item de inventário único. Cultivos continuam sem separação entre semente e produto, conforme a regra existente.
