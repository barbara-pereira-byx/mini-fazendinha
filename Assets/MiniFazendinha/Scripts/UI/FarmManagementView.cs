using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiniFazendinha.Planting;
using MiniFazendinha.Animals;
using MiniFazendinha.FruitTrees;
using static MiniFazendinha.UI.FarmWidgets;
namespace MiniFazendinha.UI
{
    // Presentation only: inventory and animal commands remain owned by the existing models.
    public sealed class FarmManagementView
    {
        readonly Canvas canvas;readonly FarmInventory inventory;readonly Action closeOthers;
        AnimalShelter selectedShelter;int shelterPage;RectTransform root,content;Action refresh;float nextRefresh;AnimalShelter parentShelter;
        FeedingPoint trough;int amount=1;bool lastCropConfirmed;Vector2 anchor;
        public bool IsOpen=>root!=null&&root.gameObject.activeSelf;
        public RectTransform Panel=>root;
        public FarmManagementView(Canvas canvas,FarmInventory inventory,Action closeOthers){this.canvas=canvas;this.inventory=inventory;this.closeOthers=closeOthers;}
        public void Close(){if(root!=null){root.gameObject.SetActive(false);UnityEngine.Object.Destroy(root.gameObject);root=null;}refresh=null;}
        public void Back(){if(parentShelter!=null){var s=parentShelter;ShowShelter(s);}else Close();}
        public void Tick(){if(IsOpen&&Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.5f;refresh?.Invoke();}}
        RectTransform Begin(string title,float width,float height,bool side=false)
        {Close();closeOthers();var cr=(RectTransform)canvas.transform;height=Mathf.Min(height,cr.rect.height-32);width=Mathf.Min(width,cr.rect.width-32);root=Box(canvas.transform,title,width,height,Cream);root.gameObject.AddComponent<FarmScrollCapture>();root.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(0,-4);root.anchorMin=root.anchorMax=root.pivot=side?new Vector2(1,.5f):new Vector2(.5f,.5f);root.anchoredPosition=side?new Vector2(-16,0):Vector2.zero;Text(root,title,20,18,width-85,38,23,true);Button(root,"×","Fechar",width-52,14,36,Back);return root;}
        void ClearContent(){if(content==null)return;foreach(Transform t in content)UnityEngine.Object.Destroy(t.gameObject);}
        public void ShowWarehouse()
        {
            parentShelter=null;Begin("Armazém",760,650);Text(root,"Todos os produtos da sua fazenda",22,55,600,26,15);
            var storage=Text(root,"",22,94,680,25,17,true);var track=Box(root,"Storage",700,9,new Color(.84f,.84f,.75f));At(track,22,124);var fill=Box(track,"Used",1,9,Green);At(fill,0,0);
            content=Scroll(root,20,155,root.sizeDelta.x-40,root.sizeDelta.y-175,out var scroll);int columns=Mathf.Max(1,Mathf.FloorToInt(content.sizeDelta.x/140));var quantities=new System.Collections.Generic.List<(InventoryItemDefinition item,TMP_Text label)>();int index=0;
            foreach(var item in inventory.Items.OrderBy(i=>i is CropDefinition?0:1).ThenBy(i=>i.DisplayName))
            {var card=Box(content,item.DisplayName,130,142,Color.white);At(card,8+(index%columns)*140,8+(index/columns)*152);Icon(card,item.Icon,26,8,78);var name=Text(card,item.DisplayName,5,89,120,26,15,true);name.alignment=TextAlignmentOptions.Center;var qty=Text(card,"",5,115,120,23,16);qty.alignment=TextAlignmentOptions.Center;quantities.Add((item,qty));index++;}
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.CeilToInt(index/(float)columns)*152+16);
            refresh=()=>{storage.text="Espaço  "+inventory.Used+" / "+inventory.Capacity+"  •  Livre: "+(inventory.Capacity-inventory.Used);fill.sizeDelta=new Vector2(700*inventory.Used/(float)inventory.Capacity,9);foreach(var row in quantities)row.label.text="× "+inventory.Quantity(row.item);};refresh();
        }
        public void ShowShelter(AnimalShelter shelter)
        {
            if(selectedShelter!=shelter){selectedShelter=shelter;shelterPage=0;}
            parentShelter=null;Begin(shelter.DisplayName,760,650);
            Icon(root,Sprite(shelter.PermittedSpecies.Identifier),22,18,46);
            root.GetComponentsInChildren<TMP_Text>().First().rectTransform.anchoredPosition=new Vector2(82,-20);
            var capacity=Text(root,"",82,61,360,26,16);
            Button(root,"✎","Renomear curral",root.sizeDelta.x-108,58,42,()=>Rename(shelter.DisplayName,shelter.Rename,()=>ShowShelter(shelter)));
            var gate=Button(root,shelter.GateOpen?"▥":"▱",shelter.GateOpen?"Fechar porteira":"Abrir porteira",22,100,60,()=>
            {
                bool opening=!shelter.GateOpen;shelter.SetGate(opening);
                if(shelter.GateOpen==opening){if(opening)shelter.ReleaseAll();}else Notice("Aguarde a passagem ficar livre");
                ShowShelter(shelter);
            });
            Button(root,"♪","Chamar moradores",92,100,60,()=>{if(!shelter.GateOpen){Notice("Abra a porteira antes de chamar");return;}shelter.RecollectAll();Notice("Chamando os moradores");});
            var gateStatus=Text(root,"",170,110,440,26,16,true);
            float width=root.sizeDelta.x-40;
            content=Scroll(root,20,160,width,root.sizeDelta.y-180,out var scroll);
            var updates=new System.Collections.Generic.List<Action>();string signature="";
            void Populate()
            {
                ClearContent();updates.Clear();
                var residents=shelter.Residents.Where(a=>a!=null).ToArray();
                signature=shelter.Capacity+"|"+string.Join("|",residents.Select(a=>a.AnimalId+":"+a.ProductsAvailable));
                int cells=Mathf.Max(shelter.Capacity,residents.Length),pages=Mathf.Max(1,Mathf.CeilToInt(cells/6f));shelterPage=Mathf.Clamp(shelterPage,0,pages-1);
                Text(content,"Moradores",8,3,320,26,18,true);
                if(pages>1)
                {
                    var previous=Button(content,"◀","Página anterior de moradores",width-150,0,36,()=>{shelterPage--;Populate();});previous.interactable=shelterPage>0;
                    Text(content,(shelterPage+1)+" / "+pages,width-108,7,56,25,14);
                    var next=Button(content,"▶","Próxima página de moradores",width-50,0,36,()=>{shelterPage++;Populate();});next.interactable=shelterPage<pages-1;
                }
                float cellWidth=(width-40)/6;
                for(int slot=0;slot<6;slot++)
                {
                    int index=shelterPage*6+slot;if(index>=cells)continue;
                    var card=Box(content,"Morador",cellWidth-6,102,Color.white);At(card,8+slot*cellWidth,37);
                    if(index>=residents.Length){var empty=Text(card,"+",0,29,cellWidth-6,40,28);empty.alignment=TextAlignmentOptions.Center;continue;}
                    var animal=residents[index];Icon(card,Sprite(animal.Definition.Identifier),(cellWidth-70)/2,3,64);
                    var label=Text(card,animal.DisplayName,5,69,cellWidth-16,23,13,true);label.alignment=TextAlignmentOptions.Center;
                    var fill=Box(card,"Felicidade",1,4,Green);At(fill,6,95);
                    var attention=Text(card,"!",cellWidth-26,4,18,22,17,true);
                    updates.Add(()=>{fill.sizeDelta=new Vector2((cellWidth-18)*animal.Happiness,4);attention.gameObject.SetActive(animal.Health!=AnimalHealthState.Healthy);});
                    card.gameObject.AddComponent<FarmTooltip>().Value=animal.DisplayName+" • "+animal.HealthLabel;
                    card.gameObject.AddComponent<Button>().onClick.AddListener(()=>ShowAnimal(animal,shelter));
                }
                if(residents.Length==0)Text(content,"Sem moradores",8,164,width-20,40,16);
                else
                {
                    float column=(width-30)/2;
                    updates.Add(LiveBar(content,"Felicidade",()=>residents.Average(a=>a.Happiness),8,158,column,"ActionHeart"));
                    updates.Add(LiveBar(content,"Alimentação",()=>residents.Average(a=>a.FeedingLevel),column+20,158,column,"ActionFeed"));
                    updates.Add(LiveBar(content,"Água",()=>residents.Average(a=>a.HydrationLevel),8,202,column,"Water"));
                    updates.Add(LiveBar(content,"Saúde",()=>residents.Average(a=>a.Health==AnimalHealthState.Healthy?1f:a.Health==AnimalHealthState.Attention?.5f:.25f),column+20,202,column,"ActionHealth"));
                }
                Text(content,"Recursos",8,245,width-20,25,18,true);
                var foodCard=Box(content,"Alimento",(width-30)/2,65,Color.white);At(foodCard,8,277);Icon(foodCard,shelter.Food.AcceptedItem.Icon,10,9,46);
                Text(foodCard,shelter.Food.AcceptedItem.DisplayName,66,7,240,23,15,true);var food=Text(foodCard,"",66,33,240,25,14);updates.Add(()=>food.text=shelter.Food.Units+" / "+shelter.Food.Capacity+" no comedouro");
                var waterCard=Box(content,"Água",(width-30)/2,65,Color.white);At(waterCard,(width-30)/2+20,277);Icon(waterCard,shelter.Water.AcceptedItem.Icon,10,9,46);
                Text(waterCard,"Água",66,7,240,23,15,true);var water=Text(waterCard,"",66,33,240,25,14);updates.Add(()=>water.text=shelter.Water.Units+" / "+shelter.Water.Capacity+" no bebedouro");
                Text(content,"Produção",8,356,width-20,25,18,true);
                var product=shelter.PermittedSpecies.Product;
                if(product==null)Text(content,"Sem produto configurado",8,395,width-20,40,15);
                else
                {
                    var card=Box(content,"Produção disponível",width-20,64,Color.white);At(card,8,389);Icon(card,product.Icon,12,7,48);
                    var amount=Text(card,"",76,7,width-210,25,17,true);var time=Text(card,"",76,34,width-210,23,14);
                    var collect=CollectionButton(card,"Coleta","Coletar "+product.DisplayName,width-130,12,90,()=>{int n=0;foreach(var a in shelter.Residents)if(a!=null)n+=a.CollectProducts();Notice("+"+n+" "+product.DisplayName,Camera.main.WorldToScreenPoint(shelter.transform.position),product.Icon);});
                    updates.Add(()=>{int n=shelter.ProductsAvailable;amount.text=n+" "+product.DisplayName;time.text=n>0?"Pronto para coletar":residents.Length==0?"Sem moradores":"Próximo ciclo em "+Mathf.CeilToInt(residents.Min(a=>a.SecondsToNextProduct))+" s";collect.interactable=n>0;});
                }
                content.sizeDelta=new Vector2(width,462);if(scroll.verticalScrollbar!=null)scroll.verticalScrollbar.gameObject.SetActive(content.sizeDelta.y>scroll.viewport.rect.height);foreach(var update in updates)update();
            }
            Populate();
            refresh=()=>
            {
                capacity.text=shelter.ResidentCount+" / "+shelter.Capacity+" moradores";
                gateStatus.text=shelter.Residents.Any(a=>a!=null&&a.IsReturningHome)?"Animais retornando":shelter.GateOpen?"Porteira aberta":"Porteira fechada";
                string current=shelter.Capacity+"|"+string.Join("|",shelter.Residents.Where(a=>a!=null).Select(a=>a.AnimalId+":"+a.ProductsAvailable));
                if(current!=signature)Populate();foreach(var update in updates)update();
            };refresh();
        }
        public void ShowAnimal(FarmAnimalController animal,AnimalShelter from=null)
        {
            parentShelter=from;Begin(animal.DisplayName,360,560,from!=null);Icon(root,Sprite(animal.Definition.Identifier),22,58,88);Text(root,animal.Definition.DisplayName,122,68,200,28,17,true);Text(root,animal.Home.DisplayName,122,99,200,35,14);
            Button(root,"✎","Renomear animal",272,105,44,()=>Rename(animal.DisplayName,animal.Rename,()=>ShowAnimal(animal,from)));
            var status=Text(root,"",22,151,310,26,15);
            var happiness=LiveBar(root,"Felicidade",()=>animal.Happiness,22,187,310,"ActionHeart");var food=LiveBar(root,"Alimentação",()=>animal.FeedingLevel,22,231,310,"ActionFeed");var water=LiveBar(root,"Hidratação",()=>animal.HydrationLevel,22,275,310,"Water");var health=LiveBar(root,"Saúde",()=>animal.Health==AnimalHealthState.Healthy?1f:animal.Health==AnimalHealthState.Attention?.5f:.25f,22,319,310,"ActionHealth");
            refresh=()=>{status.text=animal.StateLabel+" • "+animal.HealthLabel;happiness();food();water();health();};refresh();            if(animal.Definition.Product!=null){Icon(root,animal.Definition.Product.Icon,22,366,52);var product=Text(root,"",96,366,225,60,16,true);var old=refresh;refresh=()=>{old();product.text=animal.ProductsAvailable+" disponíveis\n"+Mathf.CeilToInt(animal.SecondsToNextProduct)+" s • eficiência "+Mathf.RoundToInt(animal.ProductionEfficiency*100)+"%";};refresh();}
            Button(root,"♥","Cuidar do animal",22,433,70,()=>{Notice(animal.Care()?"Carinho recebido":"Aguarde para cuidar novamente");});Button(root,"+","Alimentar uma unidade",102,433,70,()=>Notice(animal.Feed()?"Animal alimentado":"Sem alimento ou já alimentado"));Button(root,"⌂","Voltar ao curral",182,433,70,()=>{if(!animal.Home.GateOpen&&!animal.IsInsideShelter){Notice("Abra a porteira para retornar");return;}animal.ReturnToShelter(animal.Home);});CollectionButton(root,"Coleta","Coletar produção",262,433,78,()=>Notice("+"+animal.CollectProducts()+" produtos"));
        }
        public void ShowTrough(FeedingPoint point,Vector2 screen)
        {parentShelter=null;trough=point;amount=1;lastCropConfirmed=false;anchor=screen;DrawTrough();}
        Button CollectionButton(Transform parent,string label,string tip,float x,float y,float width,Action action)
        {
            var button=Button(parent,label,tip,x,y,width,action);
            button.targetGraphic.color=Green;button.GetComponentInChildren<TMP_Text>().color=Color.white;
            return button;
        }
        TMP_Text CollectionPanel(string title,Sprite icon)
        {
            parentShelter=null;Begin(title,300,206);Icon(root,icon,22,66,58);
            return Text(root,"",96,67,182,60,16,true);
        }
        public void ShowWell(WaterWellController well,Vector2 screen)
        {var status=CollectionPanel("Poço de água",well.WaterItem.Icon);var button=CollectionButton(root,"Coletar","Coletar água",92,146,116,()=>{int n=well.Available;if(well.TryCollect(inventory))Notice("+"+n+" água",screen,well.WaterItem.Icon);});refresh=()=>{status.text=well.Available>0?well.Available+" disponíveis":"Recarga: "+Mathf.CeilToInt(well.SecondsToReady)+" s";button.interactable=well.Available>0&&inventory.CanReceive(well.WaterItem,well.Available);};refresh();}
        public void ShowTree(FruitTree tree,Vector2 screen)
        {var status=CollectionPanel(tree.Definition.DisplayName,tree.Definition.Fruit.Icon);var button=CollectionButton(root,"Coletar","Colher frutas",92,146,116,()=>{int n=tree.AvailableFruit;if(tree.TryHarvest())Notice("+"+n+" "+tree.Definition.Fruit.DisplayName,screen,tree.Definition.Fruit.Icon);});refresh=()=>{status.text=tree.State==FruitTreeState.ReadyToHarvest?tree.AvailableFruit+" disponíveis":tree.State==FruitTreeState.Growing||tree.State==FruitTreeState.Young?"Crescendo • "+Mathf.RoundToInt(tree.GrowthProgress*100)+"%":"Produzindo • "+Mathf.RoundToInt(tree.ProductionProgress*100)+"%";button.interactable=tree.State==FruitTreeState.ReadyToHarvest;};refresh();}
        void DrawTrough()
        {
            Begin(trough.Kind==FeedingPointKind.Food?"Comedouro":"Bebedouro",330,345);var cr=(RectTransform)canvas.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(cr,anchor,null,out var local);root.anchoredPosition=new Vector2(Mathf.Clamp(local.x,cr.rect.xMin+173,cr.rect.xMax-173),Mathf.Clamp(local.y-110,cr.rect.yMin+181,cr.rect.yMax-181));
            int max=Mathf.Min(trough.Capacity-trough.Units,inventory.Quantity(trough.AcceptedItem));amount=Mathf.Clamp(amount,0,max);Icon(root,trough.AcceptedItem.Icon,20,63,65);Text(root,trough.AcceptedItem.DisplayName,96,67,210,28,20,true);Text(root,trough.Units+" / "+trough.Capacity+" no recipiente\n"+inventory.Quantity(trough.AcceptedItem)+" no armazém",96,101,210,54,15);
            Button(root,"−","Diminuir uma unidade",20,167,56,()=>{amount=Mathf.Max(0,amount-1);lastCropConfirmed=false;DrawTrough();});var number=Text(root,amount.ToString(),84,166,84,40,26,true);number.alignment=TextAlignmentOptions.Center;Button(root,"+","Adicionar uma unidade",176,167,56,()=>{amount=Mathf.Min(max,amount+1);lastCropConfirmed=false;DrawTrough();});Button(root,"MAX","Quantidade máxima",242,167,68,()=>{amount=max;lastCropConfirmed=false;DrawTrough();});
            bool last=trough.AcceptedItem is CropDefinition&&amount>0&&amount==inventory.Quantity(trough.AcceptedItem);Text(root,last?"Você ficará sem este cultivo para replantar.":"Escolha quanto deseja transferir.",20,217,290,46,14,true);
            Button(root,"×","Cancelar sem abastecer",20,281,70,Close);var confirm=Button(root,lastCropConfirmed?"Confirmar uso final":"✓ Abastecer","Confirmar quantidade selecionada",100,281,210,()=>{int limit=Mathf.Min(trough.Capacity-trough.Units,inventory.Quantity(trough.AcceptedItem));if(amount<=0||amount>limit){lastCropConfirmed=false;DrawTrough();return;}if(trough.AcceptedItem is CropDefinition&&amount==inventory.Quantity(trough.AcceptedItem)&&!lastCropConfirmed){lastCropConfirmed=true;DrawTrough();return;}int n=trough.RefillFromInventory(inventory,amount);Notice("−"+n+" "+trough.AcceptedItem.DisplayName,anchor,trough.AcceptedItem.Icon);Close();});confirm.interactable=amount>0;
        }
        void Rename(string current,Func<string,bool> apply,Action back)
        {Begin("Renomear",340,220);var box=Box(root,"Nome",300,48,Color.white);At(box,20,70);var input=box.gameObject.AddComponent<TMP_InputField>();var t=Text(box,current,8,5,284,38,20);input.textViewport=box;input.textComponent=t;input.characterLimit=24;input.text=current;input.ActivateInputField();Button(root,"✓","Salvar nome",20,145,140,()=>{if(apply(input.text))back();else Notice("Informe de 1 a 24 caracteres");});Button(root,"×","Cancelar",180,145,140,back);}
        public void Notice(string message,Vector2? screen=null,Sprite icon=null)
        {var r=Box(canvas.transform,"Feedback",360,48,Ink);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,28);if(screen.HasValue){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);var cr=(RectTransform)canvas.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(cr,screen.Value,null,out var p);r.anchoredPosition=new Vector2(Mathf.Clamp(p.x,cr.rect.xMin+185,cr.rect.xMax-185),Mathf.Clamp(p.y+55,cr.rect.yMin+30,cr.rect.yMax-30));}if(icon!=null)Icon(r,icon,8,5,38);var t=Text(r,message,icon==null?12:50,7,icon==null?336:298,34,15,true);r.GetComponent<Image>().raycastTarget=false;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;UnityEngine.Object.Destroy(r.gameObject,2.5f);}
    }
}
