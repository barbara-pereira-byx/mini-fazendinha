# mini-fazendinha

Projeto de fazenda casual desenvolvido na Unity 6.6 (6000.6.7f1), URP. A primeira versão inclui a cena principal, câmera ortográfica com pan e zoom, armazém contextual, plantio por arraste, crescimento e colheita com estoque unificado.

## Abrir o projeto

1. Instale Unity 6000.6.7f1 com suporte a URP.
2. Clone este repositório e abra a pasta raiz pelo Unity Hub.
3. Os pacotes necessários estão declarados em `Packages/manifest.json`.
4. A cena principal é `Assets/MiniFazendinha/Scenes/FarmMain.unity`.

### Assets Pandazole

O conteúdo original de `Assets/Pandazole_Lowpoly_Asset_Bundle` não é distribuído neste repositório público. Os arquivos `.meta` dessa pasta são mantidos para preservar os GUIDs usados pela cena. Para visualizar e executar a cena, importe sua cópia licenciada do pacote Pandazole para `Assets/Pandazole_Lowpoly_Asset_Bundle/`, mantendo a estrutura original de pastas. Sem esses arquivos, referências visuais da cena ficam ausentes.

## Documentação e validação

- `Assets/MiniFazendinha/Settings/README.md` — índice da documentação.
- `Assets/MiniFazendinha/Settings/ContextUX/README.md` — arquitetura, interação, inventário e câmera.
- Testes Edit Mode e Play Mode estão disponíveis nos menus `MiniFazendinha/Planting` e `MiniFazendinha/Context UX`.
