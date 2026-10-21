using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace MiniFazendinha.Planting
{
    [Serializable]
    public sealed class SeedButtonBinding
    {
        public CropDefinition crop;
        public UnityEngine.UI.Button button;
        public TMP_Text label;
    }
    public sealed class FarmHUD : MonoBehaviour
    {
        [SerializeField] FarmInventory inventory;
        [SerializeField] FarmPlotInteractor interactor;
        [SerializeField] SeedButtonBinding[] seedButtons=Array.Empty<SeedButtonBinding>();
        [SerializeField] TMP_Text productsLabel,statusLabel;
        [SerializeField] UnityEngine.UI.Button harvestButton,cancelButton;
        [SerializeField] TMP_Text harvestLabel;
        [SerializeField] RectTransform hoverPanel,hoverProgress;
        [SerializeField] TMP_Text hoverLabel;
        [SerializeField] Color selectedColor=new Color(0.12f,0.29f,0.21f),idleColor=new Color(0.97f,0.96f,0.91f);
        [SerializeField] FarmWarehousePanel warehouse;
        [SerializeField] FarmContextMenu contextMenu;
        [SerializeField] RectTransform activeToolBadge;
        [SerializeField] TMP_Text activeToolLabel;
        [SerializeField] UnityEngine.UI.Image activeToolIcon;
        [SerializeField] UnityEngine.UI.Button activeToolCancel;
        [SerializeField] Sprite[] cropIcons;
        [SerializeField] Sprite sickleIcon;
        public FarmWarehousePanel Warehouse=>warehouse;
        public FarmContextMenu ContextMenu=>contextMenu;
        public bool HasReadyPlots {get;private set;}
        FarmPlot[] livePlots;
        string message;
        float messageUntil;
        bool started;
        RectTransform canvasRect;
        public SeedButtonBinding[] SeedButtons=>seedButtons;
        public UnityEngine.UI.Button HarvestButton=>harvestButton;
        public RectTransform HoverPanel=>hoverPanel;
        void Start()
        {
            canvasRect=(RectTransform)transform;livePlots=FindObjectsByType<FarmPlot>();
            warehouse.Bind(ToggleWarehouse,()=>CloseMenus());contextMenu.Bind(interactor,inventory,SelectSeed,SelectHarvest,()=>CloseMenus());activeToolCancel.onClick.AddListener(()=>CloseMenus());
            foreach(var binding in seedButtons){var crop=binding.crop;binding.button.onClick.AddListener(()=>SelectSeed(crop,null));}
            if(harvestButton!=null)harvestButton.gameObject.SetActive(false);
            if(cancelButton!=null)cancelButton.onClick.AddListener(()=>CloseMenus());
            started=true;Subscribe();Refresh();
        }
        void OnEnable(){if(started){Subscribe();Refresh();}}
        void OnDisable(){if(started){inventory.Changed-=Refresh;interactor.SelectionChanged-=Refresh;interactor.Feedback-=ShowFeedback;interactor.PlotFeedback-=ShowPlotFeedback;}if(hoverPanel!=null)hoverPanel.gameObject.SetActive(false);}
        void Subscribe(){inventory.Changed+=Refresh;interactor.SelectionChanged+=Refresh;interactor.Feedback+=ShowFeedback;interactor.PlotFeedback+=ShowPlotFeedback;}
        void Style(UnityEngine.UI.Button button,TMP_Text label,bool selected)
        {
            label.color=selected?Color.white:new Color(0.10f,0.20f,0.14f);
            var colors=button.colors;colors.normalColor=selected?selectedColor:idleColor;colors.selectedColor=colors.normalColor;colors.highlightedColor=selected?new Color(0.20f,0.41f,0.30f):Color.white;colors.pressedColor=new Color(0.27f,0.49f,0.36f);colors.fadeDuration=0.06f;button.colors=colors;
        }
        void Refresh()
        {
            var products=new StringBuilder("Plante, cuide e multiplique");
            for(int i=0;i<seedButtons.Length;i++)
            {
                var binding=seedButtons[i];binding.label.text=binding.crop.DisplayName+"\n<b>"+inventory.Quantity(binding.crop)+"</b>";
                Style(binding.button,binding.label,interactor.Mode==FarmInteractionMode.Plant&&interactor.SelectedCrop==binding.crop);
            }
            productsLabel.text=products.ToString();
            bool selected=interactor.Mode!=FarmInteractionMode.None;activeToolBadge.gameObject.SetActive(selected);
            activeToolLabel.text=interactor.Mode==FarmInteractionMode.Harvest?"Colher":interactor.SelectedCrop!=null?interactor.SelectedCrop.DisplayName:"";
            int cropIndex=Array.IndexOf(interactor.Crops,interactor.SelectedCrop);activeToolIcon.sprite=interactor.Mode==FarmInteractionMode.Harvest?sickleIcon:cropIndex>=0&&cropIndex<cropIcons.Length?cropIcons[cropIndex]:null;
            if(interactor.ContextMenuOpen)warehouse.HideImmediately();
            contextMenu.Refresh();
            if(harvestButton!=null){harvestLabel.text="Foice\nColher";Style(harvestButton,harvestLabel,interactor.Mode==FarmInteractionMode.Harvest);}
        }
        public void ToggleWarehouse()
        {
            bool open=!warehouse.IsOpen;interactor.CloseContextMenu();warehouse.SetOpen(open);
        }
        public void CloseMenus(bool cancelTool=true)
        {
            warehouse.SetOpen(false);if(cancelTool)interactor.CancelTool();else interactor.CloseContextMenu();
        }
        public void SelectSeed(CropDefinition crop,FarmPlot origin)
        {
            warehouse.SetOpen(false);interactor.SelectCrop(crop);
            if(origin!=null)interactor.ApplyToolToPlot(origin);Refresh();
        }
        public void SelectHarvest(FarmPlot origin)
        {
            if(origin==null || origin.State!=FarmPlotState.ReadyToHarvest)return;
            warehouse.SetOpen(false);interactor.SelectHarvestTool();
            if(origin!=null)interactor.ApplyToolToPlot(origin);Refresh();
        }
        void ShowFeedback(PlotActionResult result,CropDefinition crop)
        {
            string name=crop==null?"":crop.DisplayName;
            switch(result)
            {
                case PlotActionResult.Planted:message=name+" plantado. Continue arrastando nos canteiros vazios.";break;
                case PlotActionResult.Harvested:message="Colheita de "+name+" recebida!";break;
                case PlotActionResult.StillGrowing:message="Ainda crescendo. A foice colhe apenas cultivos maduros.";break;
                case PlotActionResult.NoStock:message="Sem estoque de "+name+". Escolha outro cultivo.";break;
                case PlotActionResult.Occupied:message="Canteiro ocupado: o cultivo atual foi preservado.";break;
                case PlotActionResult.NotReady:message="Passe a foice sobre os canteiros com borda dourada.";break;
                default:message="Escolha uma semente ou a foice.";break;
            }
            messageUntil=Time.unscaledTime+2f;Refresh();
        }
        void Update()
        {
            if(UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)CloseMenus();
            var mouse=UnityEngine.InputSystem.Mouse.current;
            if(mouse!=null&&mouse.leftButton.wasPressedThisFrame&&!interactor.IsOverUI(mouse.position.ReadValue()))warehouse.SetOpen(false);
            var cameraController=interactor.FarmCamera.GetComponent<MiniFazendinha.FarmCameraController>();
            if(cameraController.IsPanning)CloseMenus(false);
            bool ready=false;foreach(var p in livePlots)if(p!=null&&p.State==FarmPlotState.ReadyToHarvest){ready=true;break;}
            HasReadyPlots=ready;if(!ready&&interactor.Mode==FarmInteractionMode.Harvest)interactor.CancelTool();harvestButton.gameObject.SetActive(false);activeToolBadge.gameObject.SetActive(interactor.Mode==FarmInteractionMode.Plant || interactor.Mode==FarmInteractionMode.Harvest&&ready);
            if(Time.unscaledTime<messageUntil)statusLabel.text=message;
            else statusLabel.text=interactor.Mode==FarmInteractionMode.None?"Escolha uma semente ou a foice | Arraste esquerdo/direito: mover | Scroll: zoom":interactor.Mode==FarmInteractionMode.Plant?"PLANTAR: arraste sobre canteiros vazios | Clique fora ou Esc: fechar":"COLHER: arraste a foice sobre cultivos maduros | Clique fora ou Esc: fechar";
            var plot=interactor.HoveredPlot;bool show=plot!=null&&plot.Crop!=null&&plot.State!=FarmPlotState.Empty&&!interactor.ContextMenuOpen&&!warehouse.IsOpen;
            if(hoverPanel==null)return;hoverPanel.gameObject.SetActive(show);if(!show)return;
            hoverLabel.text="<b>"+plot.Crop.DisplayName+"</b>\n"+(plot.State==FarmPlotState.ReadyToHarvest?"Pronto para colher":"Crescendo | "+Math.Ceiling(plot.RemainingSeconds)+" s | "+Mathf.RoundToInt(plot.Progress*100)+"%");
            hoverProgress.anchorMax=new Vector2(plot.Progress,1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,interactor.PointerPosition,null,out var point);
            point+=new Vector2(135,88);var size=canvasRect.rect.size;var half=hoverPanel.sizeDelta*0.5f;
            point.x=Mathf.Clamp(point.x,-size.x/2+half.x+10,size.x/2-half.x-10);point.y=Mathf.Clamp(point.y,-size.y/2+half.y+16,size.y/2-half.y-10);hoverPanel.anchoredPosition=point;
        }
        void ShowPlotFeedback(FarmPlot plot,PlotActionResult result,CropDefinition crop,int amount)
        {if(result==PlotActionResult.Harvested&&amount>0)StartCoroutine(Popup(plot,crop,amount));}
        IEnumerator Popup(FarmPlot plot,CropDefinition crop,int amount)
        {
            var go=new GameObject("HarvestAmount_Runtime",typeof(RectTransform));go.transform.SetParent(transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(260,46);
            var text=go.AddComponent<TextMeshProUGUI>();text.font=productsLabel.font;text.fontSize=28;text.fontStyle=FontStyles.Bold;text.text="+"+amount+" "+crop.DisplayName;text.color=new Color(0.06f,0.25f,0.11f);text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            var shadow=go.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(1f,1f,0.92f,0.95f);shadow.effectDistance=new Vector2(1,-2);
            float startedAt=Time.unscaledTime;
            while(Time.unscaledTime-startedAt<1.15f)
            {
                if(plot==null)break;float t=(Time.unscaledTime-startedAt)/1.15f;
                var screen=interactor.FarmCamera.WorldToScreenPoint(plot.transform.position+Vector3.up*2);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out var point);rect.anchoredPosition=point+Vector2.up*(30+55*t);text.alpha=1-t;yield return null;
            }
            Destroy(go);
        }
    }
}