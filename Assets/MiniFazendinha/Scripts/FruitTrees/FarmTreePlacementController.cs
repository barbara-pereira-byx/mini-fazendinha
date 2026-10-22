using UnityEngine;
using UnityEngine.InputSystem;
using MiniFazendinha.Planting;
using MiniFazendinha.Animals;
using MiniFazendinha.UI;
namespace MiniFazendinha.FruitTrees
{
    [DefaultExecutionOrder(80)]
    public sealed class FarmTreePlacementController:MonoBehaviour
    {
        [SerializeField] Camera farmCamera;
        [SerializeField] FarmInventory inventory;
        [SerializeField] Collider groundCollider;
        [SerializeField] Vector2 farmMin=new Vector2(-19,-19),farmMax=new Vector2(19,19);
        [SerializeField,Min(.001f)] float groundY=.03f;
        [SerializeField] LayerMask blockedLayers=~0;
        [SerializeField] Material readyMaterial;
        FruitTreeDefinition selected;
        GameObject preview;
        readonly Collider[] overlaps=new Collider[256];
        readonly System.Collections.Generic.List<Material> previewMaterials=new System.Collections.Generic.List<Material>();
        public bool IsPlacing=>selected!=null;
        public GameObject Preview=>preview;
        public string PlacementHint {get;private set;}
        void Awake(){if(farmCamera==null)farmCamera=Camera.main;if(inventory==null)inventory=FindAnyObjectByType<FarmInventory>();if(groundCollider==null)groundCollider=GameObject.Find("Terreno_Verde_40x40")?.GetComponent<Collider>();}
        public void Begin(FruitTreeDefinition definition)
        {
            FindAnyObjectByType<FarmHUD>()?.CloseMenus();FindAnyObjectByType<FarmAnimalContextUI>()?.CloseManagement();Cancel();
            if(definition==null||!definition.IsValid)return;selected=definition;
            preview=Instantiate(definition.ModelPrefab);preview.name="TreePlacementPreview";
            foreach(var c in preview.GetComponentsInChildren<Collider>(true)){c.enabled=false;Destroy(c);}
            foreach(var r in preview.GetComponentsInChildren<Renderer>(true))foreach(var m in r.materials){previewMaterials.Add(m);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
            Tint(new Color(.35f,1,.45f,.55f));
        }
        void Update()
        {
            if(!IsPlacing||Mouse.current==null)return;
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Cancel();return;}
            var p=Mouse.current.position.ReadValue();if(farmCamera==null||!farmCamera.pixelRect.Contains(p)||FarmInputPriority.IsOverUI(p))return;
            var ray=farmCamera.ScreenPointToRay(p);Vector3 point;
            if(groundCollider!=null){if(!groundCollider.Raycast(ray,out var hit,farmCamera.farClipPlane)){Tint(Color.red);return;}point=hit.point;}
            else {if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))return;point=ray.GetPoint(distance);}
            point.y=groundY;preview.transform.position=point;
            bool valid=CanPlace(point,out string reason);PlacementHint=reason;Tint(valid?new Color(.35f,1,.45f,.55f):new Color(1,.25f,.2f,.55f));
            if(Mouse.current.leftButton.wasPressedThisFrame&&valid)TryPlace(point,out _);
        }
        public bool CanPlace(Vector3 p,out string reason)
        {
            reason="";if(selected==null){reason="Selecione uma árvore";return false;}
            float radius=selected.SpacingRadius;
            if(p.x-radius<farmMin.x||p.x+radius>farmMax.x||p.z-radius<farmMin.y||p.z+radius>farmMax.y){reason="Fora do terreno";return false;}
            if(groundCollider!=null){var b=groundCollider.bounds;if(p.x-radius<b.min.x||p.x+radius>b.max.x||p.z-radius<b.min.z||p.z+radius>b.max.z){reason="Fora do terreno";return false;}}
            int count=Physics.OverlapSphereNonAlloc(p+Vector3.up*.7f,radius,overlaps,blockedLayers,QueryTriggerInteraction.Collide);
            if(count==overlaps.Length){reason="Área ocupada";return false;}
            for(int i=0;i<count;i++)
            {
                var c=overlaps[i];if(c==null||c==groundCollider||preview!=null&&c.transform.IsChildOf(preview.transform))continue;
                if(c.GetComponentInParent<AnimalShelter>()!=null||c.GetComponentInParent<FruitTree>()!=null||c.GetComponentInParent<FarmPlot>()!=null||c.GetComponentInParent<FarmAnimalController>()!=null){reason="Área ocupada";return false;}
                bool path=false;for(var t=c.transform;t!=null;t=t.parent){string n=t.name.ToLowerInvariant();if(n.StartsWith("caminho")||n.StartsWith("acesso_")||n.StartsWith("cerca")){path=true;break;}}
                if(path){reason="Caminho ou cerca protegidos";return false;}
                if(c.isTrigger)continue;
                if(c.bounds.max.y<=groundY+.02f)continue;
                reason="Área ocupada";return false;
            }
            return true;
        }
        public bool TryPlace(Vector3 point,out FruitTree result)
        {
            result=null;if(!CanPlace(point,out string reason)){PlacementHint=reason;return false;}
            var definition=selected;var root=new GameObject(definition.DisplayName);root.transform.position=new Vector3(point.x,groundY,point.z);
            try
            {
                var visual=Instantiate(definition.ModelPrefab,root.transform,false);visual.name="Visual";
                var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                visual.transform.position+=Vector3.up*(groundY-bounds.min.y);
                var collider=root.AddComponent<SphereCollider>();collider.center=Vector3.up*Mathf.Max(.7f,bounds.size.y*.5f);collider.radius=definition.SpacingRadius;
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="FrutaPronta";marker.transform.SetParent(root.transform,false);marker.transform.localPosition=Vector3.up*(bounds.size.y+.25f);marker.transform.localScale=Vector3.one*.16f;var markerCollider=marker.GetComponent<Collider>();markerCollider.enabled=false;Destroy(markerCollider);marker.GetComponent<Renderer>().sharedMaterial=readyMaterial;marker.SetActive(false);
                result=root.AddComponent<FruitTree>();result.Configure(definition,inventory,visual.transform,marker);Cancel();return true;
            }
            catch{Destroy(root);throw;}
        }
        void Tint(Color color){if(preview==null)return;foreach(var r in preview.GetComponentsInChildren<Renderer>()){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetColor("_BaseColor",color);r.SetPropertyBlock(b);}}
        public void Cancel(){if(preview!=null){preview.SetActive(false);Destroy(preview);}preview=null;selected=null;foreach(var m in previewMaterials)if(m!=null)Destroy(m);previewMaterials.Clear();}
        void OnDisable()=>Cancel();
    }
}
