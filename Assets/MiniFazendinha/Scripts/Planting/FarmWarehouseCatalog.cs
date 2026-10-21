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
        RectTransform panel, content;
        bool built;
        void Start(){if(warehouse==null||interactor==null||inventory==null)return;Build();}
        void Update(){if(panel!=null){panel.gameObject.SetActive(warehouse.IsOpen);if(warehouse.IsOpen)Refresh();}}
        void Build()
        {
            var parent=warehouse.Panel;
            var root=new GameObject("CropCatalog",typeof(RectTransform),typeof(Image));root.transform.SetParent(parent,false);
            panel=root.GetComponent<RectTransform>();panel.anchorMin=Vector2.zero;panel.anchorMax=Vector2.one;panel.offsetMin=Vector2.zero;panel.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color(.96f,.92f,.82f,1f);
            var header=Text(root.transform,"Cultivos",24,true);header.rectTransform.anchorMin=new Vector2(0,1);header.rectTransform.anchorMax=new Vector2(1,1);header.rectTransform.pivot=new Vector2(0,1);header.rectTransform.anchoredPosition=new Vector2(18,-14);header.rectTransform.sizeDelta=new Vector2(-78,38);
            var close=Button(root.transform,"X",new Color(.56f,.34f,.20f));var closeRect=close.GetComponent<RectTransform>();closeRect.anchorMin=closeRect.anchorMax=new Vector2(1,1);closeRect.pivot=new Vector2(1,1);closeRect.sizeDelta=new Vector2(42,38);closeRect.anchoredPosition=new Vector2(-12,-10);close.onClick.AddListener(()=>warehouse.SetOpen(false));
            var scrollGo=new GameObject("Scroll",typeof(RectTransform),typeof(Image),typeof(ScrollRect));scrollGo.transform.SetParent(root.transform,false);
            var sr=scrollGo.GetComponent<RectTransform>();sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=new Vector2(12,12);sr.offsetMax=new Vector2(-12,-58);
            scrollGo.GetComponent<Image>().color=new Color(.91f,.87f,.76f,.35f);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));viewport.transform.SetParent(scrollGo.transform,false);
            var vr=viewport.GetComponent<RectTransform>();vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;viewport.GetComponent<Image>().color=Color.white;viewport.GetComponent<Mask>().showMaskGraphic=false;
            var contentGo=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));contentGo.transform.SetParent(viewport.transform,false);content=contentGo.GetComponent<RectTransform>();content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;
            var layout=contentGo.GetComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(4,4,4,4);layout.spacing=6;layout.childControlWidth=true;layout.childControlHeight=false;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=scrollGo.GetComponent<ScrollRect>();scroll.viewport=vr;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
            foreach(var crop in interactor.Crops)if(crop!=null)CreateCropRow(crop);
            if(fruitTrees.Length>0){CreateHeader("Árvores frutíferas");foreach(var tree in fruitTrees)if(tree!=null)CreateTreeRow(tree);}
            built=true;
        }
        void CreateHeader(string label){var text=Text(content,label,17,true);var le=text.gameObject.AddComponent<LayoutElement>();le.preferredHeight=28;}
        void CreateTreeRow(FruitTreeDefinition tree)
        {
            var go=new GameObject(tree.name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));go.transform.SetParent(content,false);go.GetComponent<LayoutElement>().preferredHeight=58;go.GetComponent<Image>().color=new Color(.99f,.97f,.9f);
            var button=go.GetComponent<Button>();var iconGo=new GameObject("IconPlaceholder",typeof(RectTransform),typeof(Image));iconGo.transform.SetParent(go.transform,false);var icon=iconGo.GetComponent<RectTransform>();icon.anchorMin=new Vector2(0,0);icon.anchorMax=new Vector2(0,1);icon.sizeDelta=new Vector2(42,-12);icon.anchoredPosition=new Vector2(10,0);iconGo.GetComponent<Image>().color=new Color(.32f,.58f,.28f);
            var label=Text(go.transform,tree.DisplayName,17,true);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(64,4);label.rectTransform.offsetMax=new Vector2(-10,-4);
            button.onClick.AddListener(()=>{if(treePlacement!=null)treePlacement.Begin(tree);warehouse.SetOpen(false);});
        }
        void CreateCropRow(CropDefinition crop)
        {
            var go=new GameObject(crop.name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));go.transform.SetParent(content,false);
            go.GetComponent<LayoutElement>().preferredHeight=58;go.GetComponent<Image>().color=new Color(.99f,.97f,.9f);
            var button=go.GetComponent<Button>();var colors=button.colors;colors.highlightedColor=new Color(.86f,.92f,.76f);colors.pressedColor=new Color(.70f,.82f,.61f);button.colors=colors;
            var iconGo=new GameObject("Icon",typeof(RectTransform),typeof(Image));iconGo.transform.SetParent(go.transform,false);
            var icon=iconGo.GetComponent<Image>();icon.sprite=crop.Icon;icon.preserveAspect=true;icon.color=crop.Icon==null?crop.AccentColor:Color.white;
            var ir=icon.rectTransform;ir.anchorMin=new Vector2(0,0);ir.anchorMax=new Vector2(0,1);ir.pivot=new Vector2(0,0.5f);ir.sizeDelta=new Vector2(46,-10);ir.anchoredPosition=new Vector2(8,0);
            var text=Text(go.transform,crop.DisplayName,17,true);text.rectTransform.anchorMin=new Vector2(0,0);text.rectTransform.anchorMax=new Vector2(1,1);text.rectTransform.offsetMin=new Vector2(64,4);text.rectTransform.offsetMax=new Vector2(-12,-4);
            text.text=crop.DisplayName+"    "+inventory.Quantity(crop);
            button.onClick.AddListener(()=>{interactor.SelectCrop(crop);warehouse.SetOpen(false);});
        }
        void Refresh()
        {
            if(!built)return;
            foreach(Transform item in content)
            {
                var definition=System.Array.Find(interactor.Crops,c=>c!=null&&c.name==item.name);
                if(definition==null)continue;
                var text=item.GetComponentInChildren<TMP_Text>();if(text!=null)text.text=definition.DisplayName+"    "+inventory.Quantity(definition);
                var image=item.GetComponent<Image>();var colors=item.GetComponent<Button>().colors;
                image.color=interactor.SelectedCrop==definition&&interactor.Mode==FarmInteractionMode.Plant?new Color(.72f,.84f,.63f):new Color(.99f,.97f,.9f);
            }
        }
        static TMP_Text Text(Transform parent,string value,float size,bool bold)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var t=go.GetComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.color=new Color(.18f,.22f,.14f);t.text=value;t.alignment=TextAlignmentOptions.MidlineLeft;return t;
        }
        static Button Button(Transform parent,string label,Color color)
        {
            var go=new GameObject(label,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;
            var b=go.GetComponent<Button>();var text=Text(go.transform,label,18,true);text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;return b;
        }
    }
}
