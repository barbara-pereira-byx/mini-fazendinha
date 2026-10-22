# Entrega — MiniFazendinha

Cena: `Assets/MiniFazendinha/Scenes/FarmMain.unity`. Editor validado: Unity 6000.6.7f1, URP.

## Correções

- Armazém com imagens, nome e quantidade para os 16 itens existentes. Os desenhos originais de cenoura, milho e trigo foram preservados; os demais seguem o mesmo estilo ilustrado. A lista usa duas colunas e rolagem, com quantidades atualizadas pelo evento do inventário.
- Menu do canteiro com quatro cartões por página e acesso aos nove cultivos em três páginas. A escolha continua usando o interactor existente, incluindo a ferramenta de arraste e a colheita.
- Cinco currais identificados: vacas e ovelhas à esquerda; galinhas, porcos e patos à direita. Cercas proporcionais, corredor central, saídas distintas, cochos visíveis e espaço de circulação. Casinhas internas desativadas e fardos de feno transferidos para junto do celeiro.
- Cada curral tem espécie permitida, capacidade configurável (6 nesta cena), residentes, área, alimento, água e referências de entrada e saída. O registro rejeita espécies incompatíveis e não transfere o animal para outro curral por um comando de retorno.
- Animais recolhidos continuam visíveis e circulam dentro da área do curral. Ao liberar, caminham pelo portão e passam a explorar a área da fazenda; ao recolher, voltam por pontos de passagem ao curral de origem. A navegação considera cercas, construções, canteiros e outros animais, sem ignorar colisões nem teleportar para destravar.
- Painéis do animal, curral, cochos e poço corrigidos: os textos pertencem aos botões corretos, as ações são vinculadas e as informações se atualizam enquanto o painel está aberto. Clique na cerca também identifica o curral.
- Cuidado aumenta felicidade com intervalo entre ações. Há estados de saúde saudável/atenção/doente, alimentação, hidratação e produção. Alimentar diretamente desconta o cultivo configurado do inventário.
- Comedouros e bebedouros consomem recursos de verdade. Galinhas e patos usam milho; vacas e ovelhas, trigo; porcos, abóbora. Os alimentos continuam definidos nos dados das espécies. Os cochos começam vazios.
- Poço fornece 10 unidades de água, com recarga de 30 segundos após a coleta. Água vai ao inventário e abastece bebedouros. Também foi completado o cadastro de leite, ovos, lã e frutas no inventário, que antes só registrava água entre os recursos adicionais.
- Animator ligado à hierarquia correta de `Visual`, com todos os canais dos clips encontrando os ossos (150–230 canais conforme espécie). Clips com repetição, root motion desativado, transição suave e movimento/rotação por código. Marcadores de produção têm material URP salvo.

## Arquivos

A relação completa, comparada com o checkpoint inicial, está em [Arquivos-alterados.txt](Arquivos-alterados.txt). Os principais são:

- `Scripts/Animals/AnimalShelter.cs`, `FarmAnimalController.cs`, `FarmAnimalNavigation.cs`, `FarmAnimalContextUI.cs`, `WaterWellController.cs`.
- `Scripts/Planting/FarmWarehouseCatalog.cs`, `FarmContextMenu.cs`, `FarmHUD.cs`.
- `Scripts/Editor/FarmPolishSetup.cs`, `FarmInventoryIcons.cs` e os três verificadores `FarmPolishTests`, `FarmPointerChecks`, `FarmDeepChecks`.
- `Settings/Crops/*.asset`, `Settings/Inventory/**/*.asset`, `UI/Icons/*.png`, materiais novos e `Scenes/FarmMain.unity`.
- Metadados de importação dos clips em `Assets/ThirdParty/VoxelFarm/Animations/Animals`.

## Verificação

Os registros reais estão em:

- [FarmPolish-tests.txt](FarmPolish-tests.txt): inventário, painéis, água, cultivos, páginas, arraste, crescimento/colheita, câmera/zoom e saída/passeio/retorno por espécie.
- [FarmPointer-tests.txt](FarmPointer-tests.txt): eventos de mouse pelo Input System para poço/coleta, animal, curral, abertura/fechamento e rolagem do armazém, canteiro, paginação e plantio.
- [FarmDeep-tests.txt](FarmDeep-tests.txt): evolução da animação e dos ossos, consumo automático, hidratação, produção, estado básico de doença e circulação conjunta.
- [Animation-bindings.txt](Animation-bindings.txt): verificação dos canais de animação.

O teste de arraste da suíte principal chama os métodos de gesto do interactor. Os testes de clique e rolagem enfileiram eventos reais do Input System. Os testes de produção usam uma condição perto do prazo de produção; não esperam cada ciclo completo de leite. As quantidades extras e os estados de fome usados nos testes são fixtures de Play Mode, sem alterar os saldos iniciais salvos da fazenda.

Capturas em [Screenshots/Entrega](../Screenshots/Entrega): fazenda, curral, animal, poço, armazém e três páginas de plantio. Armazém também inspecionado em 1600×900 e 1600×1000; Game View capturada em 1280×720.

## Limites explícitos

- Doença é uma base de estado e apresentação; não há epidemias, evolução automática de doenças nem tratamento complexo.
- Os portões são aberturas funcionais; não foi criada uma animação de abrir/fechar portões.
- Não foram realizados testes prolongados de horas, testes em aparelhos móveis ou avaliação de todos os tamanhos de tela. O ajuste estético final pode ser revisto manualmente no Editor.
- As funcionalidades grandes excluídas do pedido não foram iniciadas.

## Recuperação

Checkpoint anterior às alterações: `.minifazendinha-checkpoints/before-farm-polish-20261022.zip`, contendo a pasta MiniFazendinha, Packages e ProjectSettings com as mudanças locais que já existiam. Os metadados originais dos clips, que estavam sem alterações no início da tarefa, também foram preservados em `.minifazendinha-checkpoints/animation-import-original` a partir do Git local. Os arquivos dos assets licenciados não foram substituídos.

A saída coletiva é escalonada dentro de cada curral: o próximo animal começa a sair quando o anterior conclui a passagem. Os pontos de circulação dos currais opostos são diferentes para evitar disputa pela mesma posição no corredor. Recolher um animal já dentro da área apenas cancela o passeio; não o obriga a disputar outro ponto interno.

## Resultado consolidado

As três verificações concluíram com sucesso: 62 checagens da suíte principal, 11 verificações com eventos de ponteiro e 10 verificações adicionais de consumo/animação/grupo. O teste coletivo confirmou saída e retorno dos dez animais aos currais corretos. A última consulta ao Console após essa rodada não apresentou erros. Houve falhas durante o desenvolvimento (referências de UI, feno no portão, produtos sem cadastro e contatos de navegação); os problemas foram corrigidos e os testes correspondentes repetidos.

Estado final: Editor fora do Play Mode, compilação sem erros e configuração original `Run In Background = false` restaurada. A cena salva mantém os estoques iniciais; as fixtures não foram gravadas na fazenda. Captura final: [armazém com ícones](../Screenshots/Entrega/Armazem-final.png).
