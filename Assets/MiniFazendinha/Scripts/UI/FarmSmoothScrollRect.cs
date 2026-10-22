using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MiniFazendinha.UI
{
    // Wheel pixels are smoothed using unscaled time; dragging retains ScrollRect inertia.
    public sealed class FarmSmoothScrollRect:ScrollRect
    {
        bool wheel;float target;
        public override void OnScroll(PointerEventData e)
        {if(!IsActive()||content==null||viewport==null)return;float overflow=Mathf.Max(0,content.rect.height-viewport.rect.height);if(overflow<=.01f)return;if(!wheel)target=verticalNormalizedPosition;target=Mathf.Clamp01(target+e.scrollDelta.y*scrollSensitivity/overflow);wheel=true;velocity=Vector2.zero;e.Use();}
        public override void OnBeginDrag(PointerEventData e){wheel=false;base.OnBeginDrag(e);}
        protected override void LateUpdate(){base.LateUpdate();if(!wheel)return;float p=Mathf.Lerp(verticalNormalizedPosition,target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));if(Mathf.Abs(p-target)<.0001f){p=target;wheel=false;}verticalNormalizedPosition=p;}
    }
}
