using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MiniFazendinha.Planting;
using MiniFazendinha.FruitTrees;

namespace MiniFazendinha.Animals
{
    [DefaultExecutionOrder(150)]
    public sealed class FarmAnimalContextUI : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] FarmInventory inventory;
        Canvas canvas;
        RectTransform panel;
        TMP_Text title, body;
        readonly List<Button> buttons=new List<Button>();
        bool open;
        RectTransform canvasRect;
        void Start()
        {
            if(viewCamera==null)viewCamera=Camera.main;
            if(inventory==null)inventory=FindAnyObjectByType<FarmInventory>();
            BuildUI();
        }
        void Update()
        {
            if(Mouse.current==null)return;
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Close();return;}
            var p=Mouse.current.position.ReadValue();
            if(Mouse.current.leftButton.wasPressedThisFrame&&!OverUI(p))
            {
                if(open){Close();return;}
                if(viewCamera==null)return;
                var ray=viewCamera.ScreenPointToRay(p);
                if(Physics.Raycast(ray,out var hit,viewCamera.farClipPlane,~0,QueryTriggerInteraction.Ignore))
                {
                    var animal=hit.collider.GetComponentInParent<FarmAnimalController>();
                    var shelter=hit.collider.GetComponentInParent<AnimalShelter>();
                    var trough=hit.collider.GetComponentInParent<FeedingPoint>();
                    var fruitTree=hit.collider.GetComponentInParent<FruitTree>();
                    if(animal!=null)ShowAnimal(animal,p);
                    else if(shelter!=null)ShowShelter(shelter,p);
                    else if(trough!=null)ShowTrough(trough,p);
                    else if(fruitTree!=null)ShowTree(fruitTree,p);
                }
            }
            else if(open&&Mouse.current.leftButton.wasPressedThisFrame&&!OverUI(p))Close();
        }
        bool OverUI(Vector2 p)
        {
            if(EventSystem.current==null)return false;
            var data=new PointerEventData(EventSystem.current){position=p};var results=new List<RaycastResult>();EventSystem.current.RaycastAll(data,results);return results.Count>0;
        }
        void BuildUI()
        {
            canvas=FindAnyObjectByType<Canvas>();
            if(canvas==null)return;
            canvasRect=canvas.transform as RectTransform;
            var root=new GameObject("AnimalContextPanel",typeof(RectTransform),typeof(Image));root.transform.SetParent(canvas.transform,false);
            panel=root.GetComponent<RectTransform>();panel.sizeDelta=new Vector2(290,195);panel.anchorMin=panel.anchorMax=new Vector2(.5f,.5f);panel.pivot=new Vector2(.5f,.5f);
            var bg=root.GetComponent<Image>();bg.color=new Color(.96f,.91f,.79f,.98f);
            title=Text("Title",new Vector2(12,-10),22,true);
            body=Text("Body",new Vector2(12,-44),16,false);
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("Action_"+i,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(root.transform,false);
                var rt=go.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,0);rt.anchorMax=new Vector2(1,0);rt.pivot=new Vector2(.5f,0);rt.sizeDelta=new Vector2(-20,36);rt.anchoredPosition=new Vector2(0,10+i*42);
                go.GetComponent<Image>().color=new Color(.32f,.53f,.32f);
                var label=Text("Label",Vector2.zero,16,true);label.alignment=TextAlignmentOptions.Center;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
                buttons.Add(go.GetComponent<Button>());
            }
            root.SetActive(false);
        }
        TMP_Text Text(string n,Vector2 pos,float size,bool bold)
        {
            var go=new GameObject(n,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(panel,false);
            var t=go.GetComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.color=new Color(.16f,.2f,.13f);t.alignment=TextAlignmentOptions.Left;
            var rt=t.rectTransform;rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=pos;rt.sizeDelta=new Vector2(-24,32);return t;
        }
        void Configure(string heading,string description,Vector2 screen,int actionCount)
        {
            if(panel==null)return;title.text=heading;body.text=description;
            foreach(var b in buttons){b.onClick.RemoveAllListeners();b.gameObject.SetActive(false);}
            panel.sizeDelta=new Vector2(290,74+actionCount*42);
            for(int i=0;i<actionCount;i++)buttons[i].gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
            var area=canvasRect.rect;var half=panel.sizeDelta*.5f;local.x=Mathf.Clamp(local.x,area.xMin+half.x+4,area.xMax-half.x-4);
            local.y=Mathf.Clamp(local.y-half.y,area.yMin+half.y+4,area.yMax-half.y-4);
            Vector2 at=local;
            panel.anchoredPosition=at;panel.gameObject.SetActive(true);open=true;
        }
        void SetAction(int i,string label,UnityEngine.Events.UnityAction action)
        {buttons[i].GetComponentInChildren<TMP_Text>().text=label;buttons[i].onClick.AddListener(action);}
        void ShowAnimal(FarmAnimalController animal,Vector2 p)
        {
            int actions=animal.ProductsAvailable>0?1:0;
            Configure(animal.DisplayName+" · "+animal.Definition.DisplayName,animal.StateLabel+"\nAlimentação: "+Mathf.RoundToInt(animal.FeedingLevel*100)+"%\n"+(animal.ProductsAvailable>0?"Produtos: "+animal.ProductsAvailable:"Próximo produto: "+Mathf.CeilToInt(animal.SecondsToNextProduct)+" s"),p,actions);
            if(actions>0)SetAction(0,"Coletar produto",()=>{animal.CollectProducts();Close();});
        }
        void ShowShelter(AnimalShelter shelter,Vector2 p)
        {
            int ready=0;foreach(var a in shelter.Residents)if(a!=null)ready+=a.ProductsAvailable;
            Configure("Celeiro","Animais: "+shelter.ResidentCount+"\nProdutos disponíveis: "+ready,p,3);
            SetAction(0,"Recolher animais",()=>shelter.RecollectAll());
            SetAction(1,"Soltar animais",()=>shelter.ReleaseAll());
            SetAction(2,"Coletar produtos",()=>{foreach(var a in shelter.Residents)if(a!=null)a.CollectProducts();});
        }
        void ShowTrough(FeedingPoint trough,Vector2 p)
        {
            Configure(trough.Kind==FeedingPointKind.Food?"Comedouro":"Bebedouro","Unidades: "+trough.Units+"/"+trough.Capacity,p,1);
            SetAction(0,"Reabastecer +8",()=>{trough.Refill(8);Close();});
        }
        void ShowTree(FruitTree tree,Vector2 p)
        {
            bool ready=tree.State==FruitTreeState.ReadyToHarvest;
            Configure(tree.Definition.DisplayName,ready?"Frutas maduras para colher":"Crescendo: "+Mathf.RoundToInt(tree.GrowthProgress*100)+"%",p,ready?1:0);
            if(ready)SetAction(0,"Colher "+tree.Definition.Fruit.DisplayName,()=>{tree.TryHarvest();Close();});
        }
        void Close(){if(panel!=null)panel.gameObject.SetActive(false);open=false;}
    }
}
