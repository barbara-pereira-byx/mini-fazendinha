using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace MiniFazendinha.Planting
{
    public sealed class FarmContextMenu : MonoBehaviour
    {
        [SerializeField] RectTransform panel,seedChoices,harvestChoice,progressTrack,progressFill;
        [SerializeField] TMP_Text title,subtitle;
        [SerializeField] SeedButtonBinding[] seedButtons;
        [SerializeField] Button harvestButton,closeButton;
        FarmPlotInteractor interactor;FarmInventory inventory;RectTransform canvas;
        Action<CropDefinition,FarmPlot> plantAction;int page;const int PageSize=3;
        Button previous,next;TMP_Text pageLabel;bool wasOpen;
        public FarmPlot BoundPlot{get;private set;}
        public RectTransform Panel=>panel;
        public SeedButtonBinding[] SeedButtons=>seedButtons;
        public Button HarvestButton=>harvestButton;
        public Button CloseButton=>closeButton;
        public bool IsOpen=>panel.gameObject.activeSelf;
        public int Page=>page;
        public int PageCount=>interactor==null?1:Mathf.Max(1,Mathf.CeilToInt(interactor.Crops.Length/(float)PageSize));
        public void Bind(FarmPlotInteractor input,FarmInventory stock,Action<CropDefinition,FarmPlot> plant,Action<FarmPlot> harvest,Action close)
        {
            interactor=input;inventory=stock;canvas=(RectTransform)transform;plantAction=plant;
            var template=seedButtons[0];
            for(int i=0;i<seedButtons.Length;i++)seedButtons[i].button.gameObject.SetActive(false);
            var bindings=new SeedButtonBinding[PageSize];
            for(int i=0;i<PageSize;i++)
            {
                var go=Instantiate(template.button.gameObject,seedChoices);go.name="CropChoice_"+i;go.SetActive(true);
                var b=go.GetComponent<Button>();b.onClick=new Button.ButtonClickedEvent();var label=go.GetComponentInChildren<TMP_Text>();bindings[i]=new SeedButtonBinding{button=b,label=label};
                var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(96,112);r.anchoredPosition=new Vector2((i-1)*104,0);
                int slot=i;b.onClick.AddListener(()=>{var c=bindings[slot].crop;if(c!=null)plantAction(c,BoundPlot);});
            }
            seedButtons=bindings;seedChoices.anchorMin=seedChoices.anchorMax=new Vector2(.5f,1);seedChoices.pivot=new Vector2(.5f,1);seedChoices.anchoredPosition=new Vector2(0,-75);seedChoices.sizeDelta=new Vector2(310,150);
            previous=PageButton("◀",new Vector2(-64,-140),()=>ChangePage(-1));next=PageButton("▶",new Vector2(64,-140),()=>ChangePage(1));
            var pg=new GameObject("PageLabel",typeof(RectTransform),typeof(TextMeshProUGUI));pg.transform.SetParent(seedChoices,false);pageLabel=pg.GetComponent<TextMeshProUGUI>();pageLabel.font=TMP_Settings.defaultFontAsset;pageLabel.fontSize=15;pageLabel.color=new Color(.18f,.24f,.14f);pageLabel.alignment=TextAlignmentOptions.Center;pageLabel.raycastTarget=false;var pr=(RectTransform)pg.transform;pr.anchorMin=pr.anchorMax=new Vector2(.5f,1);pr.anchoredPosition=new Vector2(0,-140);pr.sizeDelta=new Vector2(70,32);
            harvestButton.onClick.AddListener(()=>harvest(BoundPlot));closeButton.onClick.AddListener(()=>close());panel.gameObject.SetActive(false);
        }
        Button PageButton(string label,Vector2 pos,Action action)
        {
            var r=MiniFazendinha.UI.FarmWidgets.Box(seedChoices,label,40,32,new Color(.34f,.52f,.30f));
            r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;
            var icon=MiniFazendinha.UI.FarmWidgets.Icon(r,MiniFazendinha.UI.FarmWidgets.Sprite(label=="◀"?"ActionLeft":"ActionRight"),6,2,28);icon.color=Color.white;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.onClick.AddListener(()=>action());return button;
        }        public void ChangePage(int delta){page=Mathf.Clamp(page+delta,0,PageCount-1);Refresh();}
        public void Refresh()
        {
            if(interactor==null)return;BoundPlot=interactor.ContextMenuOpen?interactor.SelectedPlot:null;panel.gameObject.SetActive(BoundPlot!=null);if(BoundPlot==null){wasOpen=false;return;}if(!wasOpen){page=0;wasOpen=true;}
            bool empty=BoundPlot.State==FarmPlotState.Empty,ready=BoundPlot.State==FarmPlotState.ReadyToHarvest;
            seedChoices.gameObject.SetActive(empty);harvestChoice.gameObject.SetActive(ready);progressTrack.gameObject.SetActive(!empty&&!ready);
            panel.sizeDelta=new Vector2(empty?340:270,empty?260:ready?205:140);
            title.text=empty?"O que vamos plantar?":BoundPlot.Crop.DisplayName;subtitle.text=empty?"Todos os cultivos · escolha um":ready?"Pronto para colher":Math.Ceiling(BoundPlot.RemainingSeconds)+" s restantes";progressFill.anchorMax=new Vector2(BoundPlot.Progress,1);
            for(int i=0;i<seedButtons.Length;i++)
            {
                int index=page*PageSize+i;var b=seedButtons[i];bool valid=index<interactor.Crops.Length;b.button.gameObject.SetActive(valid);if(!valid)continue;b.crop=interactor.Crops[index];b.label.text=b.crop.DisplayName+"\n<b>"+inventory.Quantity(b.crop)+"</b>";b.button.interactable=inventory.Quantity(b.crop)>0;
                var icon=b.button.transform.Find("ToolIcon")?.GetComponent<Image>();if(icon!=null){icon.sprite=b.crop.Icon;icon.gameObject.SetActive(true);icon.preserveAspect=true;}
            }
            if(previous!=null){previous.interactable=page>0;next.interactable=page<PageCount-1;pageLabel.text=(page+1)+" / "+PageCount;}
        }
        void LateUpdate(){Refresh();if(IsOpen)Reposition();}
        public void Reposition()
        {
            if(BoundPlot==null)return;Vector2 screen=interactor.FarmCamera.WorldToScreenPoint(BoundPlot.transform.position+Vector3.up*1.5f);var surface=canvas.GetComponent<Canvas>();RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,surface.renderMode==RenderMode.ScreenSpaceOverlay?null:surface.worldCamera,out var local);var area=canvas.rect;var half=panel.sizeDelta*.5f;local.y+=half.y+24;local.x=Mathf.Clamp(local.x,area.xMin+half.x+16,area.xMax-half.x-16);local.y=Mathf.Clamp(local.y,area.yMin+half.y+16,area.yMax-half.y-16);panel.anchoredPosition=local;
        }
    }
}
