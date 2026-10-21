using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiniFazendinha.Planting
{
    public enum FarmInteractionMode { None, Plant, Harvest }
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class FarmPlotInteractor : MonoBehaviour
    {
        [SerializeField] Camera farmCamera;
        [SerializeField] FarmInventory inventory;
        [SerializeField] CropDefinition[] crops = Array.Empty<CropDefinition>();
        [SerializeField] FarmPlot[] plots = Array.Empty<FarmPlot>();
        [SerializeField, Range(1,12)] float sampleSpacingPixels = 4f;
        [SerializeField] Vector2 farmMin = new Vector2(-20,-20), farmMax = new Vector2(20,20);
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        readonly List<(FarmPlot plot, Collider collider)> targets = new List<(FarmPlot,Collider)>();
        readonly HashSet<FarmPlot> visited = new HashSet<FarmPlot>();
        Vector2 previous;
        bool stockWarning;Vector2 leftPress;bool clickCandidate;
        public Camera FarmCamera => farmCamera;
        public CropDefinition[] Crops => crops;
        public CropDefinition SelectedCrop { get; private set; }
        public FarmPlot SelectedPlot { get; private set; }
        public FarmPlot HoveredPlot { get; private set; }
        public FarmInteractionMode Mode { get; private set; }
        public bool IsDragging { get; private set; }
        public bool ContextMenuOpen { get; private set; }
        public int StrokeAffectedCount { get; private set; }
        public Vector2 PointerPosition { get; private set; }
        public event Action SelectionChanged;
        public event Action HoverChanged;
        public event Action<PlotActionResult,CropDefinition> Feedback;
        public event Action<FarmPlot,PlotActionResult,CropDefinition,int> PlotFeedback;
        void Start() {RebuildTargets();SelectionChanged?.Invoke();}
        public void RebuildTargets()
        {
            targets.Clear();
            if(plots.Length==0)plots=FindObjectsByType<FarmPlot>();
            foreach(var plot in plots)if(plot!=null)foreach(var collider in plot.GetComponentsInChildren<Collider>())
                if(collider.GetComponentInParent<FarmPlot>()==plot)targets.Add((plot,collider));
        }
        void Update()
        {
            if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)CancelTool();
            var mouse=Mouse.current;if(mouse==null){CancelDrag();return;}
            var pointer=mouse.position.ReadValue();UpdateHover(pointer);
            if(mouse.rightButton.wasPressedThisFrame && !IsOverUI(pointer))NotifyCameraNavigation();
            if(mouse.rightButton.isPressed){CancelDrag();clickCandidate=false;return;}
            if(mouse.leftButton.wasPressedThisFrame)
            {
                leftPress=pointer;clickCandidate=!IsOverUI(pointer);
                if(clickCandidate && ContextMenuOpen)CloseContextMenu();
                if(clickCandidate && Mode!=FarmInteractionMode.None)
                {
                    if(PlotAt(pointer)!=null)BeginStroke(pointer);
                    else CancelTool();
                }
            }
            else if(IsDragging && mouse.leftButton.isPressed)ContinueStroke(pointer);
            if(mouse.leftButton.wasReleasedThisFrame)
            {
                EndStroke(pointer);
                var cameraController=farmCamera.GetComponent<MiniFazendinha.FarmCameraController>();
                if(clickCandidate && Mode==FarmInteractionMode.None && Vector2.Distance(leftPress,pointer)<6f &&
                    (cameraController==null || !cameraController.LeftGestureWasPan))
                {
                    var plot=PlotAt(pointer);if(plot!=null)OpenContextMenu(plot);else CancelTool();
                }
                clickCandidate=false;
            }
            else if(!mouse.leftButton.isPressed)CancelDrag();
        }
        void OnDisable() {CancelDrag();SetHover(null);}
        void OnApplicationFocus(bool focus) {if(!focus){CancelDrag();clickCandidate=false;SetHover(null);}}
        public void SelectCrop(CropDefinition crop)
        {
            if(crop==null || Array.IndexOf(crops,crop)<0)return;
            CancelDrag();ContextMenuOpen=false;SelectedCrop=crop;Mode=FarmInteractionMode.Plant;SelectionChanged?.Invoke();RefreshHoverView();
        }
        public void SelectHarvestTool() {CancelDrag();ContextMenuOpen=false;Mode=FarmInteractionMode.Harvest;SelectionChanged?.Invoke();RefreshHoverView();}
        public void CancelTool() {CancelDrag();ContextMenuOpen=false;Mode=FarmInteractionMode.None;SelectionChanged?.Invoke();RefreshHoverView();}
        public void OpenContextMenu(FarmPlot plot)
        {
            if(plot==null)return;CancelDrag();SelectedPlot=plot;Mode=FarmInteractionMode.None;ContextMenuOpen=true;SelectionChanged?.Invoke();
        }
        public void CloseContextMenu(){if(!ContextMenuOpen)return;ContextMenuOpen=false;SelectionChanged?.Invoke();}
        public void NotifyCameraNavigation(){CancelDrag();clickCandidate=false;CloseContextMenu();}
        public void ApplyToolToPlot(FarmPlot plot)
        {
            if(plot==null || inventory==null || Mode==FarmInteractionMode.None)return;
            visited.Clear();stockWarning=false;ApplyPlot(plot);
        }
        public void CancelDrag() {IsDragging=false;visited.Clear();}
        public bool IsOverUI(Vector2 screenPoint)
        {
            if(EventSystem.current==null)return false;
            uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screenPoint},uiHits);return uiHits.Count>0;
        }
        bool IsPlayable(Vector2 point)
        {
            if(farmCamera==null || !farmCamera.pixelRect.Contains(point) || IsOverUI(point))return false;
            var ray=farmCamera.ScreenPointToRay(point);
            if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))return false;
            var ground=ray.GetPoint(distance);
            return ground.x>=farmMin.x && ground.x<=farmMax.x && ground.z>=farmMin.y && ground.z<=farmMax.y;
        }
        public FarmPlot PlotAt(Vector2 point)
        {
            if(farmCamera==null || !farmCamera.pixelRect.Contains(point) || IsOverUI(point))return null;
            if(targets.Count==0)RebuildTargets();
            var ray=farmCamera.ScreenPointToRay(point);float nearest=float.PositiveInfinity;FarmPlot found=null;
            // Only registered plot colliders are queried. Buildings, plants and decorations cannot occlude them.
            foreach(var target in targets)
                if(target.plot!=null && target.plot.isActiveAndEnabled && target.collider!=null && target.collider.enabled && target.collider.gameObject.activeInHierarchy && !target.collider.isTrigger && target.collider.Raycast(ray,out var hit,farmCamera.farClipPlane) && hit.distance<nearest)
                {nearest=hit.distance;found=target.plot;}
            return found;
        }
        public void UpdateHover(Vector2 pointer) {PointerPosition=pointer;SetHover(PlotAt(pointer));}
        void SetHover(FarmPlot plot)
        {
            if(HoveredPlot==plot){RefreshHoverView();return;}
            if(HoveredPlot!=null)HoveredPlot.GetComponent<FarmPlotView>()?.SetHover(false,false);
            HoveredPlot=plot;RefreshHoverView();HoverChanged?.Invoke();
        }
        public bool CanAffect(FarmPlot plot) => plot!=null && (Mode==FarmInteractionMode.Plant ? plot.State==FarmPlotState.Empty && SelectedCrop!=null && inventory.Quantity(SelectedCrop)>0 : Mode==FarmInteractionMode.Harvest && plot.State==FarmPlotState.ReadyToHarvest);
        void RefreshHoverView() {if(HoveredPlot!=null)HoveredPlot.GetComponent<FarmPlotView>()?.SetHover(true,CanAffect(HoveredPlot));}
        public bool BeginStroke(Vector2 point)
        {
            CancelDrag();StrokeAffectedCount=0;stockWarning=false;
            if(Mode==FarmInteractionMode.None || inventory==null || !IsPlayable(point) || PlotAt(point)==null)return false;
            IsDragging=true;previous=point;ApplyAt(point);return true;
        }
        public void ContinueStroke(Vector2 point)
        {
            if(!IsDragging)return;
            if(!farmCamera.pixelRect.Contains(point) || IsOverUI(point)){CancelDrag();return;}
            int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(previous,point)/Mathf.Max(1,sampleSpacingPixels)));
            for(int i=1;i<=count;i++)
            {
                var sample=Vector2.Lerp(previous,point,(float)i/count);
                if(IsOverUI(sample)){CancelDrag();return;}
                ApplyAt(sample);
            }
            previous=point;
        }
        public void EndStroke(Vector2 point) {if(IsDragging)ContinueStroke(point);CancelDrag();}
        public bool HandleScreenClick(Vector2 point) {if(!BeginStroke(point))return false;bool affected=StrokeAffectedCount>0;EndStroke(point);return affected;}
        void ApplyAt(Vector2 point)
        {
            var plot=PlotAt(point);ApplyPlot(plot);
        }
        void ApplyPlot(FarmPlot plot)
        {
            if(plot==null || !visited.Add(plot))return;
            SelectedPlot=plot;var crop=Mode==FarmInteractionMode.Plant?SelectedCrop:plot.Crop;
            int before=crop!=null?inventory.Quantity(crop):0;
            PlotActionResult result;
            if(Mode==FarmInteractionMode.Plant)result=plot.State==FarmPlotState.Empty?plot.TryPlant(SelectedCrop,inventory):PlotActionResult.Occupied;
            else result=plot.State==FarmPlotState.ReadyToHarvest?plot.TryHarvest(inventory):plot.State==FarmPlotState.Growing?PlotActionResult.StillGrowing:PlotActionResult.NotReady;
            int amount=crop!=null?inventory.Quantity(crop)-before:0;
            if(result==PlotActionResult.Planted || result==PlotActionResult.Harvested)StrokeAffectedCount++;
            if(result==PlotActionResult.NoStock){if(stockWarning)return;stockWarning=true;}
            plot.GetComponent<FarmPlotView>()?.Pulse(result);
            if(result==PlotActionResult.Harvested)plot.GetComponent<FarmPlotView>()?.EmitHarvest();
            Feedback?.Invoke(result,crop);PlotFeedback?.Invoke(plot,result,crop,amount);SelectionChanged?.Invoke();RefreshHoverView();
        }
    }
}