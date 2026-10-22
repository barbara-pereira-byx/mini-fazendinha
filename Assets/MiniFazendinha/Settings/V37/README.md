# MiniFazendinha V3.7 — produção, pomar e polimento

Cena preservada: `Assets/MiniFazendinha/Scenes/FarmMain.unity`.
Editor: Unity 6000.6.7f1, URP. Implementação usa os componentes existentes de animais, inventário, árvore, porteira e uGUI.

## Diagnóstico

1. Os tempos dos animais eram 28 s (galinha), 45 s (vaca), 60 s (ovelha), 33 s (pato) e 40 s (porco). Não havia corrotina duplicada nem multiplicador ativo de velocidade na versão atual. Patos e porcos tinham rendimento zero e nenhum produto configurado.
2. Os prefabs de árvores têm a escala 100 do FBX na raiz. A implementação antiga adicionava o collider a essa raiz, multiplicando o raio de 1,3 para cerca de 130 metros. Também criava um indicador como primeiro filho e usava esse filho como modelo a crescer, deixando o visual real fora do ciclo.
3. A validação de posicionamento precisava distinguir a superfície do gramado, a base de terra abaixo dele, caminhos concretos e os grupos da hierarquia. Não se deve interpretar o grupo `Terreno e Caminhos` como um caminho protegido.
4. O painel de curral tinha 300×760 unidades do Canvas, contra 760×650 do armazém. Os moradores, indicadores e recursos estavam empilhados em uma coluna. Havia duas ações de porteira e a folha aberta era apenas girada, deixando duas travessas aparentes.
5. A paginação usava botões de 98 pixels e tooltips que surgiam sobre os cartões.
6. A falha de entrada relatada antes desta etapa vinha de um mouse físico desabilitado e um dispositivo virtual de teste restante. `FarmTestInputScope` passou a restaurar dispositivos e configurações ao finalizar, interromper Play Mode e recarregar scripts. O mouse físico foi reabilitado.

## Produção comum a todas as espécies

| Espécie | Recurso | Intervalo-base | Quantidade base |
|---|---|---:|---:|
| Galinha | Ovo | 60 s | 1 |
| Vaca | Leite | 120 s | 2 |
| Ovelha | Lã | 180 s | 2 |
| Pato | Ovo de pato | 90 s | 1 |
| Porco | Trufa | 150 s | 1 |

`AnimalDefinition.productionSeconds` é editável por espécie, com mínimo 60 no Inspector e proteção de mínimo 60 também na leitura em runtime. A produção inicia com progresso zero; o temporizador soma tempo ativo não escalado e não avança quando `Time.timeScale` é zero. Aumentar `timeScale` não encurta o intervalo real. A ficha lê o mesmo progresso usado para produzir.

O bem-estar continua alterando **quantidade**, não a duração: 100%, 75%, 50% e 25%, editáveis por espécie. A fração do rendimento é conservada entre ciclos, usando piso para a quantidade inteira. Nenhum produto surge antes de concluir um ciclo. Valores de tempo inválidos ou negativos são ignorados.

`DuckEgg` e `Truffle` são itens próprios do inventário unificado, com identificadores distintos e ícones próprios. A mesma rotina do animal produz e coleta ovos, leite, lã, ovos de pato e trufas. Não foi criado um gerenciador separado. Não foi adicionada uma animação específica de procura de trufas; foram preservados os clips e modelos existentes.

## Árvores frutíferas

Macieira, laranjeira e limoeiro continuam usando os FBXs reais do Voxel Farm através dos prefabs e materiais próprios do projeto. Os arquivos dos fornecedores não foram editados.

O novo plantio cria uma raiz de jogo com escala 1, um filho `Visual` que preserva a escala importada do FBX, um collider em metros e um indicador de frutos independente. A prévia tem colliders imediatamente desativados, acompanha o cursor e usa verde/vermelho conforme a validade.

O terreno real e seus limites são consultados. A base abaixo do gramado é ignorada. Construções, canteiros, currais, animais, caminhos protegidos e outras árvores impedem posicionamento. A interação do cenário e o arraste esquerdo da câmera cedem prioridade ao modo de colocar árvores. O zoom e o arraste direito continuam disponíveis.

Foi preservada a regra de plantio existente, sem custo de mudas: não havia um recurso de muda para descontar. Não foi inventado um sistema de compra ou economia. As frutas coletadas entram no inventário unificado.

Configuração por definição:

- Maturação inicial: **60 segundos**.
- Frutificação: **90 segundos**.
- Quantidade: **3 frutas** por ciclo.

Os estados são jovem, crescendo, madura, produzindo e pronta para colher. A árvore possui quantidade coletável real. A interface mostra a fruta e o saldo disponível; a cesta coleta sem remover a árvore. A coleta limpa o saldo antes de notificar o inventário, bloqueia reentrada, reinicia somente a frutificação e não repete o crescimento inicial. Se faltar espaço no armazém, o saldo é preservado. Um indicador pequeno sinaliza a disponibilidade.

## Porteira e gerenciador de curral

A porteira tem uma única ação contextual: quando aberta, a ação é fechar; quando fechada, a ação é abrir. O ícone e o tooltip representam a ação. O sino permanece independente e exige abertura para recolher quem estiver fora. Uma travessia em andamento impede fechamento que prenda um animal no acesso.

Aberta: os renderers e colliders da folha móvel são desativados. As cercas laterais permanecem. Fechada: as travessas existentes são mostradas na orientação original e bloqueiam fisicamente o acesso. Não há segmento girado nem collider invisível na abertura.

O gerenciador utiliza os mesmos 760×650 do armazém, centralizado, com os mesmos cartões, cores, sombras e ícones. Os seis espaços iniciais de moradores cabem em uma linha. Lotações maiores usam páginas de seis espaços. Os quatro indicadores ficam em duas colunas; comida, água e produção têm cartões compactos. As seções iniciais cabem sem rolagem em Full HD. O painel limita-se à área do Canvas e mantém rolagem somente quando falta altura.

Foram preservados os vínculos reais de espécie/curral, nomes e identificadores. Cartões abrem a ficha individual; fechar retorna ao curral. Recursos continuam sendo abastecidos diretamente em seus recipientes, sem ação genérica de abastecer tudo.

## Poço e plantio

O poço usa um botão textual compacto e verde **Coletar**. Disponibilidade, capacidade do inventário, recarga, bloqueio de coleta repetida e feedback foram preservados.

O plantio mantém três cultivos por página, estoque único, arraste, colheita e fechamento existentes. As setas agora têm **40×32**, ícones centralizados e nenhum componente de tooltip. O contador continua discreto e os extremos ficam desabilitados. Os demais tooltips de ações foram mantidos.

## Validação e arquivos

Relatórios: `Reports/V37`.
Capturas reais: `Screenshots/ContextUX/V37-*`.
Lista detalhada de arquivos comparada ao checkpoint: `Reports/V37/changed-files.txt`.
Checkpoint anterior: `.minifazendinha-checkpoints/before-v37-20261022.zip`.

Foram testadas as fronteiras dos temporizadores chamando o mesmo método de produção do runtime, um segundo antes e no fim do ciclo, sem produção antecipada. A quantidade reduzida pelo bem-estar foi testada separadamente. Uma observação adicional de 185 segundos usa os Updates reais de cinco animais de teste isolados, sem chamadas manuais de produção nem aceleração. O relatório diferencia essa observação dos testes determinísticos.

O plantio e a colheita das três árvores foram testados com eventos reais de mouse: prévia, criação, colisão, clique na copa, cesta, saldo, árvore preservada e nova frutificação. Nos testes de crescimento/frutificação, o relógio do próprio componente foi avançado de forma determinística; não se afirma que esses ciclos foram aguardados integralmente em tempo real.

## Como validar manualmente

1. Abra FarmMain e inicie Play. Clique na área Game para dar foco ao Editor. Confirme clique, zoom e arraste normais.
2. Abra um curral e confira a largura semelhante ao armazém, as seções completas, moradores, a única ação de porteira e sino. Abra/feche e veja a passagem na cena.
3. Aguarde os tempos da tabela. Confira a ficha individual, produção do curral e os novos itens no armazém. Colete novamente para confirmar que não duplica.
4. Em Cultivos > Pomar, selecione cada árvore. Posicione no gramado livre; tente um canteiro, caminho, construção e outra árvore. Esc cancela a prévia.
5. Após 60 s de maturação e 90 s de produção, clique na copa e na cesta. A árvore deve permanecer e produzir novamente em 90 s.
6. Abra o poço, use Coletar e confira água no armazém e abastecimento do bebedouro.
7. Abra o menu de um canteiro. Percorra páginas pelas setas compactas, plante por clique/arraste e colha com foice.
8. Repita em 1920×1080, 1600×900 e 1366×768. Confira hover, digitação, Esc, clique fora e limites dos menus.

## Limites

Não havia salvamento de partida; esta etapa não cria persistência paralela. Estoques e nomes continuam sendo estado da sessão. Os checkpoints recuperam o projeto.

A evasão de animais mantém aproximação por raios e a navegação existente. Não foi feita nova auditoria de todos os clips de animação nem de todas as combinações de lotação. A procura de trufas é produção simplificada, sem simulação de escavação.

Asserções e capturas nas três resoluções não substituem avaliação humana de conforto em todos os tamanhos de janela. Depois de recompilar scripts durante Play Mode, reinicie a sessão para validar a interface dinâmica.

### Observação real e navegação V3.7

Em 185,06 s de Updates reais, sem aceleração: primeiro produto da galinha em 60,003 s; pato em 90,024 s; vaca em aproximadamente 120 s; porco em 150,078 s; ovelha em 180,069 s. As pequenas diferenças são resolução de frame/observação. O bem-estar não reduz o intervalo.

A regressão de retorno encontrou disputa pelo ponto interno da entrada. O residente que obstrui o acesso recebe prioridade temporária para caminhar até um ponto livre afastado da porteira e aguarda a conclusão do retorno do companheiro. A solução conserva os raios, as cercas e a passagem única; não usa teleporte.
