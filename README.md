# MiniFazendinha

Versão publicada: **3.0.0**. Projeto casual de fazenda em Unity 6.6 (6000.6.7f1), URP, com câmera ortográfica, plantio/colheita, inventário unificado, árvores frutíferas e gerenciamento de animais e currais.

## Branches e versionamento

| Branch | Conteúdo |
|---|---|
| `main` | Linha principal, preservada nesta publicação |
| `v1.0.0` | Checkpoint histórico da primeira versão (`v1.0-checkpoint`) |
| `v2.0.0` | Checkpoint histórico da segunda versão (`v2.0-prototype`) |
| `v3.0.0` | Terceira versão publicada, incluindo as etapas internas V3.5 a V3.8 |

Nomes seguem `vMAJOR.MINOR.PATCH`, sem espaços: alterações incompatíveis/novas versões principais incrementam MAJOR; funcionalidades compatíveis incrementam MINOR; correções compatíveis incrementam PATCH. Exemplos futuros: `v3.0.1`, `v3.1.0`, `v4.0.0`. As versões históricas apontam para seus commits reais; não são cópias artificiais da versão atual. As denominações V3.5–V3.8 nos relatórios identificam etapas de desenvolvimento anteriores à primeira publicação formal da V3.

## Abrir o projeto

1. Instale Unity **6000.6.7f1** e clone o repositório.
2. Selecione a branch desejada; para esta entrega: `git switch v3.0.0`.
3. Importe suas cópias licenciadas dos assets descritos abaixo, preservando as pastas e os arquivos `.meta` versionados.
4. Abra a raiz pelo Unity Hub. As dependências UPM estão em `Packages/manifest.json`.
5. Abra `Assets/MiniFazendinha/Scenes/FarmMain.unity` e inicie Play Mode.

## Assets de terceiros

Os modelos/texturas originais **Pandazole Lowpoly Asset Bundle** e **Voxel Farm** não são distribuídos neste repositório. O `.gitignore` mantém apenas seus arquivos `.meta`, para preservar os GUIDs e configurações de importação. Importe suas cópias licenciadas em:

- `Assets/Pandazole_Lowpoly_Asset_Bundle/`
- `Assets/ThirdParty/VoxelFarm/`

Sem os pacotes locais, referências visuais ficam ausentes. Os materiais, prefabs de integração, scripts e ícones próprios da MiniFazendinha são versionados. Library, Temp, logs e checkpoints ZIP locais permanecem fora do Git.

## Entrega 3.0.0

- Currais por espécie, porteiras, moradores e gerenciamento individual.
- Coleta animal textual e produção configurável: ovos, leite, lã, ovos de pato e bacon.
- Poço e árvores com caixas compactas de coleta; frutificação mantém a árvore plantada.
- Cultivos com ícones e três opções por página; armazém completo pelo prédio.
- Correções de prioridade de entrada, scroll/zoom e retorno pelo sino.

Limitação conhecida: liberar todos os currais simultaneamente pode causar espera prolongada no corredor compartilhado. O recolhimento pelo sino foi testado também nessa situação. Não há salvamento de partida implementado.

## Documentação e validação

- [Últimos ajustes e instruções de validação](Assets/MiniFazendinha/Settings/V38/README.md)
- [Produção, árvores e interface](Assets/MiniFazendinha/Settings/V37/README.md)
- [Relatórios reais da entrega](Reports/V38/README.md)
- Testes pelo menu `MiniFazendinha` do Unity Editor. Relatórios distinguem observação em tempo real, testes determinísticos e limitações conhecidas.
