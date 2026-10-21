# Plantio, colheita e estoque da MiniFazendinha

Documentacao atual: ../ContextUX/README.md. Cena: Assets/MiniFazendinha/Scenes/FarmMain.unity.

O Armazem lateral apresenta o saldo unico de cenoura, milho e trigo (12 de cada ao iniciar). Plantar gasta 1; colher acrescenta 3 ao mesmo saldo: 12 -> 11 -> 14. Recursos colhidos servem para replantar. Nao ha separacao entre sementes e produtos.

Clique em um canteiro vazio para escolher o cultivo e plantar nele; continue por clique ou arraste. Canteiros em crescimento mostram tempo/progresso. A foice aparece somente ao clicar em um canteiro maduro; nao aparece no armazem. X, Esc e clique fora fecham menus. Arraste direito ou esquerdo em terreno livre para navegar; scroll aproxima/afasta, com os controles visiveis em Screen Space.

Duracoes/producao: Settings/Crops/Carrot.asset (20 s), Corn.asset (35 s), Wheat.asset (50 s), YieldAmount=3. Estoque inicial no componente FarmInventory, Initial Stock, quantity=12. Os 12 FarmPlot e seus colliders foram reutilizados.

As pastas Planting e DragTools contem relatorios historicos das etapas anteriores. Os relatorios atuais, arquitetura, checkpoints, controles e limitacoes de teste estao em Settings/ContextUX. Para os modelos reais e suas composicoes visuais, consulte tambem DragTools/README.md; as antigas descricoes de barra fixa e estoques separados foram substituidas nesta etapa.