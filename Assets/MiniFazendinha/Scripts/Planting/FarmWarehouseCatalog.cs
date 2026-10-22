using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiniFazendinha.FruitTrees;
namespace MiniFazendinha.Planting
{
    [DefaultExecutionOrder(60)]
    public sealed class FarmWarehouseCatalog : MonoBehaviour
    {
        [SerializeField] FarmWarehousePanel warehouse;
        [SerializeField] FarmPlotInteractor interactor;
        [SerializeField] FarmInventory inventory;
        [SerializeField] FruitTreeDefinition[] fruitTrees=System.Array.Empty<FruitTreeDefinition>();
        [SerializeField] FarmTreePlacementController treePlacement;
        [SerializeField] InventoryItemDefinition[] items=System.Array.Empty<InventoryItemDefinition>();
        readonly Dictionary<InventoryItemDefinition,TMP_Text> quantities=new Dictionary<InventoryItemDefinition,TMP_Text>();
        RectTransform content;
        void Start(){if(warehouse==null||interactor==null||inventory==null)return;Build();inventory.Changed+=Refresh;Refresh();}
        void OnDestroy(){if(inventory!=null)inventory.Changed-=Refresh;}
        void Build()
        {
            var parent=warehouse.Panel;
            // Retire old three-crop presentation; keep its bindings intact for the existing HUD.
            foreach(Transform child in parent)child.gameObject.SetActive(false);
            var heading=Text(parent,"Cultivos",25,true);var hr=heading.rectTransform;hr.anchorMin=new Vector2(0,1);hr.anchorMax=new Vector2(1,1);hr.pivot=new Vector2(0,1);hr.anchoredPosition=new Vector2(16,-12);hr.sizeDelta=new Vector2(-60,36);
            var close=warehouse.CloseButton;close.gameObject.SetActive(true);close.transform.SetAsLastSibling();
            var root=new GameObject("CatalogScroll",typeof(RectTransform),typeof(Image),typeof(MiniFazendinha.UI.FarmSmoothScrollRect));root.transform.SetParent(parent,false);var r=(RectTransform)root.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(10,12);r.offsetMax=new Vector2(-10,-100);root.GetComponent<Image>().color=new Color(.92f,.89f,.79f);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(RectMask2D));viewport.transform.SetParent(root.transform,false);var vr=(RectTransform)viewport.transform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;
            var go=new GameObject("Content",typeof(RectTransform),typeof(GridLayoutGroup),typeof(ContentSizeFitter));go.transform.SetParent(viewport.transform,false);content=(RectTransform)go.transform;content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var grid=go.GetComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.cellSize=new Vector2(112,116);grid.spacing=new Vector2(8,8);grid.padding=new RectOffset(5,5,5,5);grid.childAlignment=TextAnchor.UpperCenter;go.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=root.GetComponent<ScrollRect>();scroll.viewport=vr;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=75;scroll.inertia=true;scroll.decelerationRate=.085f;MiniFazendinha.UI.FarmWidgets.AttachScrollbar(scroll);
            MiniFazendinha.UI.FarmWidgets.Button(parent,"Horta","Cultivos para plantar",16,54,110,ShowCrops);
            MiniFazendinha.UI.FarmWidgets.Button(parent,"Pomar","Árvores para plantar",134,54,110,ShowTrees);
            ShowCrops();
        }
        void ClearCards(){quantities.Clear();foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}content.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;}
        public void ShowCrops()
        {
            ClearCards();var all=new List<InventoryItemDefinition>(interactor.Crops);
            foreach(var item in all)
            {
                if(item==null)continue;
                var card=Card(item.DisplayName,item.Icon);var label=Text(card.transform,item.DisplayName,16,true);Place(label.rectTransform,0,-72,106,22);label.alignment=TextAlignmentOptions.Center;
                var count=Text(card.transform,"",15,false);Place(count.rectTransform,0,-94,106,20);count.alignment=TextAlignmentOptions.Center;quantities[item]=count;count.text="× "+inventory.Quantity(item);
                var b=card.GetComponent<Button>();if(item is CropDefinition crop)b.onClick.AddListener(()=>{interactor.SelectCrop(crop);warehouse.SetOpen(false);});else b.interactable=false;
            }

        }
        public void ShowTrees()
        {
            ClearCards();foreach(var tree in fruitTrees){if(tree==null)continue;var card=Card(tree.DisplayName,tree.Fruit.Icon);var label=Text(card.transform,tree.DisplayName,15,true);Place(label.rectTransform,0,-75,106,36);label.alignment=TextAlignmentOptions.Center;card.GetComponent<Button>().onClick.AddListener(()=>{treePlacement.Begin(tree);warehouse.SetOpen(false);});}Refresh();
        }        GameObject Card(string name,Sprite sprite)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(content,false);var bg=go.GetComponent<Image>();bg.sprite=Resources.Load<Sprite>("FarmUI/RoundedCard");bg.color=new Color(.99f,.97f,.90f);var b=go.GetComponent<Button>();b.targetGraphic=bg;var colors=b.colors;colors.disabledColor=Color.white;b.colors=colors;
            var icon=new GameObject("Icon",typeof(RectTransform),typeof(Image));icon.transform.SetParent(go.transform,false);var image=icon.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;Place(image.rectTransform,0,-5,66,66);return go;
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
        void Refresh(){foreach(var row in quantities)row.Value.text="× "+inventory.Quantity(row.Key);}
        static TMP_Text Text(Transform parent,string value,float size,bool bold)
        {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.color=new Color(.18f,.24f,.14f);t.text=value;t.raycastTarget=false;return t;}
    }
}

