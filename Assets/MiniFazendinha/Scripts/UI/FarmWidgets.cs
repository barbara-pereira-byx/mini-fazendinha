using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MiniFazendinha.UI
{
    public static class FarmWidgets
    {
        public static readonly Color Ink=new Color(.23f,.28f,.19f),Green=new Color(.30f,.48f,.32f),Cream=new Color(.98f,.96f,.89f);
        public static RectTransform Box(Transform parent,string name,float width,float height,Color color)
        {var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.sizeDelta=new Vector2(width,height);var i=g.GetComponent<Image>();i.sprite=Resources.Load<Sprite>("FarmUI/RoundedCard");i.type=Image.Type.Sliced;i.color=color;return r;}
        public static void At(RectTransform r,float x,float y){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);}
        public static TMP_Text Label(Transform parent,string value,float size=16,bool bold=false)
        {var g=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(parent,false);var t=g.GetComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.color=Ink;t.raycastTarget=false;return t;}
        public static TMP_Text Text(Transform p,string value,float x,float y,float w,float h,float size=16,bool bold=false)
        {var t=Label(p,value,size,bold);At(t.rectTransform,x,y);t.rectTransform.sizeDelta=new Vector2(w,h);return t;}
        public static Image Icon(Transform p,Sprite sprite,float x,float y,float size)
        {var r=Box(p,"Icon",size,size,Color.white);At(r,x,y);var i=r.GetComponent<Image>();i.sprite=sprite;i.preserveAspect=true;i.raycastTarget=false;if(sprite==null)i.color=Color.clear;return i;}
        public static Sprite Sprite(string name)=>Resources.Load<Sprite>("FarmUI/"+name);
        public static Button Button(Transform p,string glyph,string tip,float x,float y,float w,Action action)
        {var r=Box(p,tip,w,38,new Color(.82f,.88f,.74f));At(r,x,y);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();var colors=b.colors;colors.highlightedColor=new Color(.8f,.94f,.72f);colors.disabledColor=new Color(.65f,.65f,.60f,.6f);b.colors=colors;b.onClick.AddListener(()=>action());
            string key=glyph=="×"?"Close":glyph=="♥"?"Heart":glyph=="+"?"Plus":glyph=="−"?"Minus":glyph=="✎"?"Pencil":glyph=="▱"?"GateOpen":glyph=="▥"?"GateClosed":glyph=="♪"?"Bell":glyph=="⌂"?"Home":glyph=="↓"?"Basket":glyph=="◀"?"Left":glyph=="▶"?"Right":glyph=="MAX"?"Max":glyph.StartsWith("✓")?"Check":null;
            var sprite=key==null?null:Sprite("Action"+key);if(sprite!=null){var icon=Icon(r,sprite,glyph.StartsWith("✓ ")?12:(w-28)/2,5,28);icon.color=Color.white;if(glyph.StartsWith("✓ ")){var label=Text(r,glyph.Substring(2),48,0,w-56,38,16,true);label.alignment=TextAlignmentOptions.Center;}}else{var t=Text(r,glyph,0,0,w,38,16,true);t.color=Ink;t.alignment=TextAlignmentOptions.Center;}r.gameObject.AddComponent<FarmTooltip>().Value=tip;return b;}
        public static void Bar(Transform p,string title,float value,float x,float y,float w)
        {Text(p,title+"  "+Mathf.RoundToInt(value*100)+"%",x,y,w,22,14);var track=Box(p,"Status",w,8,new Color(.85f,.85f,.75f));At(track,x,y+23);var fill=Box(track,"Fill",Mathf.Max(0,w*Mathf.Clamp01(value)),8,Green);At(fill,0,0);}
        public static Action LiveBar(Transform p,string title,Func<float> read,float x,float y,float w,string icon)
        {Icon(p,Sprite(icon),x,y,24);var text=Text(p,"",x+32,y,w-32,22,14);var track=Box(p,"Status",w-32,8,new Color(.85f,.85f,.75f));At(track,x+32,y+23);var fill=Box(track,"Fill",1,8,Green);At(fill,0,0);var tip=track.gameObject.AddComponent<FarmTooltip>();Action update=()=>{float value=Mathf.Clamp01(read());text.text=title+"  "+Mathf.RoundToInt(value*100)+"%";fill.sizeDelta=new Vector2((w-32)*value,8);tip.Value=text.text;};update();return update;}
        public static RectTransform Scroll(Transform p,float x,float y,float w,float h,out ScrollRect scroll)
        {var root=Box(p,"Scroll",w,h,new Color(.94f,.92f,.85f));At(root,x,y);scroll=root.gameObject.AddComponent<FarmSmoothScrollRect>();var viewport=Box(root,"Viewport",w,h,Color.white);At(viewport,0,0);viewport.GetComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport,false);var cr=(RectTransform)content.transform;At(cr,0,0);cr.sizeDelta=new Vector2(w,0);scroll.viewport=viewport;scroll.content=cr;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=75;scroll.inertia=true;scroll.decelerationRate=.085f;AttachScrollbar(scroll);return cr;}
        public static void AttachScrollbar(ScrollRect scroll)
        {var track=Box(scroll.transform,"Scrollbar",7,100,new Color(.78f,.80f,.69f));track.anchorMin=new Vector2(1,0);track.anchorMax=new Vector2(1,1);track.pivot=new Vector2(1,.5f);track.offsetMin=new Vector2(-8,8);track.offsetMax=new Vector2(-1,-8);var handle=Box(track,"Handle",7,30,Green);var bar=track.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<Image>();scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;}
    }
    public sealed class FarmTooltip:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public string Value; GameObject tip;
        public void OnPointerEnter(PointerEventData e){var c=GetComponentInParent<Canvas>();if(c==null||string.IsNullOrEmpty(Value))return;var r=FarmWidgets.Box(c.transform,"Tooltip",240,48,new Color(.23f,.28f,.19f,.97f));tip=r.gameObject;r.GetComponent<Image>().raycastTarget=false;var t=FarmWidgets.Text(r,Value,10,5,220,38,13);t.color=Color.white;t.alignment=TextAlignmentOptions.Center;var canvas=(RectTransform)c.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,e.position,null,out var point);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(Mathf.Clamp(point.x,canvas.rect.xMin+125,canvas.rect.xMax-125),Mathf.Clamp(point.y+46,canvas.rect.yMin+28,canvas.rect.yMax-28));}
        public void OnPointerExit(PointerEventData e)=>Hide();void OnDisable()=>Hide();void Hide(){if(tip!=null)Destroy(tip);}
    }
}
