using UnityEngine;
using UnityEngine.InputSystem;
using MiniFazendinha.Planting;

namespace MiniFazendinha
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class FarmCameraController : MonoBehaviour
    {
        [SerializeField] Vector2 farmMin = new Vector2(-20f, -20f);
        [SerializeField] Vector2 farmMax = new Vector2(20f, 20f);
        [SerializeField, Min(1f)] float minZoom = 7f;
        [SerializeField, Min(1f)] float maxZoom = 23f;
        [SerializeField, Min(0.01f)] float zoomSpeed = 1.6f;
        [SerializeField] Vector3 focusPoint = Vector3.zero;
        [SerializeField, Min(10f)] float cameraDistance = 32f;
        Camera view;
        Vector2 previousPointer;
        bool dragging;
        bool leftPan; Vector2 pressPointer; FarmPlotInteractor plotInteractor;
        public bool IsPanning => dragging && (!leftPan || LeftGestureWasPan);
        [SerializeField,Min(1)] float zoomSmoothness=16f;float zoomTarget;Vector2 zoomPointer;bool smoothingZoom;
        public bool LeftGestureWasPan { get; private set; }
        public Vector3 FocusPoint => focusPoint;
        public float MinimumZoom => minZoom;
        public float MaximumZoom => maxZoom;

        void Awake() { view = GetComponent<Camera>(); view.orthographic = true; ApplyPose(); }
        void OnDisable() { dragging = false; }
        void OnApplicationFocus(bool focused) { if (!focused) dragging = false; }
        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 pointer = mouse.position.ReadValue();
            if(plotInteractor==null)plotInteractor=Object.FindAnyObjectByType<FarmPlotInteractor>();
            bool inView=view.pixelRect.Contains(pointer);
            bool overUI=MiniFazendinha.UI.FarmInputPriority.IsOverUI(pointer);
            bool wheelUI=MiniFazendinha.UI.FarmInputPriority.CapturesScroll(pointer);
            if(wheelUI){smoothingZoom=false;zoomTarget=view.orthographicSize;}
            if(mouse.leftButton.wasPressedThisFrame)LeftGestureWasPan=false;
            if(mouse.rightButton.wasPressedThisFrame && inView && !overUI)
            {leftPan=false;dragging=true;pressPointer=previousPointer=pointer;}
            else if(mouse.leftButton.wasPressedThisFrame && inView && !overUI && Object.FindAnyObjectByType<MiniFazendinha.FruitTrees.FarmTreePlacementController>()?.IsPlacing!=true &&
                (plotInteractor==null || plotInteractor.Mode==FarmInteractionMode.None || plotInteractor.PlotAt(pointer)==null))
            {leftPan=true;dragging=true;pressPointer=previousPointer=pointer;}
            bool held=leftPan?mouse.leftButton.isPressed:mouse.rightButton.isPressed;
            if(dragging && held)
            {
                bool move=!leftPan || LeftGestureWasPan || Vector2.Distance(pressPointer,pointer)>=6f;
                if(move)
                {
                    if(leftPan)LeftGestureWasPan=true;plotInteractor?.NotifyCameraNavigation();
                    if(GroundPoint(previousPointer,out var previous)&&GroundPoint(pointer,out var current))PanGround(previous-current);
                }
                previousPointer=pointer;
            }
            if(!held)dragging=false;
            float scroll = mouse.scroll.ReadValue().y;
            if(Mathf.Abs(scroll)>0.001f && inView && !wheelUI)
            {if(!smoothingZoom)zoomTarget=view.orthographicSize;zoomTarget=Mathf.Clamp(zoomTarget-Mathf.Clamp(ScrollSteps(scroll),-4f,4f)*zoomSpeed,minZoom,maxZoom);zoomPointer=pointer;smoothingZoom=true;}
            if(smoothingZoom)
            {
                float next=Mathf.Lerp(view.orthographicSize,zoomTarget,1-Mathf.Exp(-zoomSmoothness*Time.unscaledDeltaTime));
                if(Mathf.Abs(next-zoomTarget)<0.005f){next=zoomTarget;smoothingZoom=false;}
                ApplyZoom(next,zoomPointer);
            }
            ClampFocus();
            ApplyPose();
        }
        public static float ScrollSteps(float delta)
        {
            if(InputSystem.settings.scrollDeltaBehavior==InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms)return delta;
            bool windows=Application.platform==RuntimePlatform.WindowsEditor||Application.platform==RuntimePlatform.WindowsPlayer;
            return windows?delta/120f:delta;
        }
        public void PanGround(Vector3 delta)
        {
            focusPoint += new Vector3(delta.x, 0f, delta.z);
            ClampFocus(); ApplyPose();
        }
        public void ZoomAt(float steps,Vector2 screenPoint)
        {
            if(view==null)view=GetComponent<Camera>();smoothingZoom=false;
            ApplyZoom(Mathf.Clamp(view.orthographicSize-steps*zoomSpeed,minZoom,maxZoom),screenPoint);
        }
        void ApplyZoom(float size,Vector2 screenPoint)
        {
            if(view==null)view=GetComponent<Camera>();
            bool anchored=GroundPoint(screenPoint,out var before);
            view.orthographicSize=size;ApplyPose();
            if(anchored&&GroundPoint(screenPoint,out var after))focusPoint+=before-after;
            ClampFocus();ApplyPose();
        }
        bool GroundPoint(Vector2 screen, out Vector3 hit)
        {
            var ray = view.ScreenPointToRay(screen);
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
            { hit = ray.GetPoint(distance); return true; }
            hit = default; return false;
        }
        void ClampFocus()
        {
            if (view == null) return;
            // Keep the point of interest on the farm without locking wide zooms to the center.
            float insetX=Mathf.Min(2f,(farmMax.x-farmMin.x)*0.1f);
            float insetZ=Mathf.Min(2f,(farmMax.y-farmMin.y)*0.1f);
            focusPoint.x=Mathf.Clamp(focusPoint.x,farmMin.x+insetX,farmMax.x-insetX);
            focusPoint.z=Mathf.Clamp(focusPoint.z,farmMin.y+insetZ,farmMax.y-insetZ);
            focusPoint.y = 0f;
        }
        void ApplyPose() { transform.position = focusPoint - transform.forward * cameraDistance; }
        void OnValidate()
        {
            minZoom = Mathf.Max(1f, minZoom); maxZoom = Mathf.Max(minZoom, maxZoom);
            if (farmMax.x <= farmMin.x) farmMax.x = farmMin.x + 1f;
            if (farmMax.y <= farmMin.y) farmMax.y = farmMin.y + 1f;
        }
    }
}
