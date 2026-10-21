using System;
using UnityEngine;
using TMPro;

namespace MiniFazendinha.Planting
{
    public sealed class FarmContextMenu : MonoBehaviour
    {
        [SerializeField] RectTransform panel, seedChoices, harvestChoice, progressTrack, progressFill;
        [SerializeField] TMP_Text title, subtitle;
        [SerializeField] SeedButtonBinding[] seedButtons;
        [SerializeField] UnityEngine.UI.Button harvestButton, closeButton;
        FarmPlotInteractor interactor;FarmInventory inventory;RectTransform canvas;
        public FarmPlot BoundPlot {get;private set;}
        public RectTransform Panel=>panel;
        public SeedButtonBinding[] SeedButtons=>seedButtons;
        public UnityEngine.UI.Button HarvestButton=>harvestButton;
        public UnityEngine.UI.Button CloseButton=>closeButton;
        public bool IsOpen=>panel.gameObject.activeSelf;
        public void Bind(FarmPlotInteractor input,FarmInventory stock,Action<CropDefinition,FarmPlot> plant,Action<FarmPlot> harvest,Action close)
        {
            interactor=input;inventory=stock;canvas=(RectTransform)transform;
            foreach(var b in seedButtons){var crop=b.crop;b.button.onClick.AddListener(()=>plant(crop,BoundPlot));}
            harvestButton.onClick.AddListener(()=>harvest(BoundPlot));closeButton.onClick.AddListener(()=>close());panel.gameObject.SetActive(false);
        }
        public void Refresh()
        {
            if(interactor==null)return;BoundPlot=interactor.ContextMenuOpen?interactor.SelectedPlot:null;
            panel.gameObject.SetActive(BoundPlot!=null);if(BoundPlot==null)return;
            bool empty=BoundPlot.State==FarmPlotState.Empty,ready=BoundPlot.State==FarmPlotState.ReadyToHarvest;
            seedChoices.gameObject.SetActive(empty);harvestChoice.gameObject.SetActive(ready);progressTrack.gameObject.SetActive(!empty&&!ready);
            panel.sizeDelta=new Vector2(empty?340:270,empty?210:ready?205:140);
            title.text=empty?"O que vamos plantar?":BoundPlot.Crop.DisplayName;
            subtitle.text=empty?"Escolha um cultivo":ready?"Pronto para colher":Math.Ceiling(BoundPlot.RemainingSeconds)+" s restantes";
            progressFill.anchorMax=new Vector2(BoundPlot.Progress,1);
            foreach(var b in seedButtons){b.label.text=b.crop.DisplayName+"\n<b>"+inventory.Quantity(b.crop)+"</b>";b.button.interactable=inventory.Quantity(b.crop)>0;}
        }
        void LateUpdate(){Refresh();if(IsOpen)Reposition();}
        public void Reposition()
        {
            if(BoundPlot==null)return;
            Vector2 screen=interactor.FarmCamera.WorldToScreenPoint(BoundPlot.transform.position+Vector3.up*1.5f);
            var surface=canvas.GetComponent<Canvas>();RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,surface.renderMode==RenderMode.ScreenSpaceOverlay?null:surface.worldCamera,out var local);
            var area=canvas.rect;var half=panel.sizeDelta*0.5f;local.y+=half.y+24;
            local.x=Mathf.Clamp(local.x,area.xMin+half.x+16,area.xMax-half.x-16);
            local.y=Mathf.Clamp(local.y,area.yMin+half.y+16,area.yMax-half.y-16);panel.anchoredPosition=local;
        }
    }
}