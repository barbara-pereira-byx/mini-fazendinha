# mini-fazendinha

Projeto casual de gerenciamento de fazenda em Unity 6.6 (6000.6.7f1), URP. Esta versao inclui a cena principal, camera ortografica com movimento e zoom, armazem contextual, plantio por arraste, crescimento e colheita com estoque unificado.

## Abrir o projeto

1. Instale Unity 6000.6.7f1 com suporte a URP.
2. Clone este repositorio e abra a pasta raiz pelo Unity Hub.
3. Os pacotes necessarios estao declarados em `Packages/manifest.json`.
4. A cena principal e `Assets/MiniFazendinha/Scenes/FarmMain.unity`.

### Assets Pandazole

Os arquivos originais de `Assets/Pandazole_Lowpoly_Asset_Bundle` nao sao distribuidos neste repositorio publico. Mantemos os arquivos `.meta` para preservar os GUIDs usados pela cena. Para visualizar e executar a cena, importe sua copia licenciada do pacote Pandazole em `Assets/Pandazole_Lowpoly_Asset_Bundle/`, mantendo a estrutura original de pastas. Sem esses arquivos, algumas referencias visuais ficam ausentes.

## Documentacao e testes

- `Assets/MiniFazendinha/Settings/README.md` — indice da documentacao.
- `Assets/MiniFazendinha/Settings/ContextUX/README.md` — arquitetura, interacao, inventario e camera.
- Testes Edit Mode e Play Mode estao disponiveis nos menus `MiniFazendinha/Planting` e `MiniFazendinha/Context UX`.
