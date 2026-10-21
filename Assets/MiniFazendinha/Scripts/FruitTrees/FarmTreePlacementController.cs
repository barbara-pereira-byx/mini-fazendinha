using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using MiniFazendinha.Planting;

namespace MiniFazendinha.FruitTrees
{
    [DefaultExecutionOrder(80)]
    public sealed class FarmTreePlacementController : MonoBehaviour
    {
        [SerializeField] Camera farmCamera;
        [SerializeField] FarmInventory inventory;
        [SerializeField] Collider groundCollider;
        [SerializeField] Vector2 farmMin=new Vector2(-19,-19),farmMax=new Vector2(19,19);
        [SerializeField,Min(.1f)] float groundY=.03f;
        [SerializeField] LayerMask blockedLayers=~0;
        FruitTreeDefinition selected;
        GameObject preview;
        readonly Collider[] overlaps=new Collider[64];
        readonly List<Material> previewMaterials=new List<Material>();
        public bool IsPlacing=>selected!=null;
        void Awake(){if(farmCamera==null)farmCamera=Camera.main;if(inventory==null)inventory=FindAnyObjectByType<FarmInventory>();if(groundCollider==null){var ground=GameObject.Find("Terreno_Verde_40x40");if(ground!=null)groundCollider=ground.GetComponent<Collider>();}}
        public void Begin(FruitTreeDefinition definition)
        {
            Cancel();if(definition==null||!definition.IsValid)return;selected=definition;
            preview=Instantiate(definition.ModelPrefab);preview.name="TreePlacementPreview";StripColliders(preview);
            foreach(var r in preview.GetComponentsInChildren<Renderer>(true))
            {
                var materials=r.materials;for(int i=0;i<materials.Length;i++){var m=materials[i];previewMaterials.Add(m);if(m.HasProperty("_Surface"))m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
            }
            SetPreviewColor(new Color(.2f,.9f,.35f,.52f));
        }
        void Update()
        {
            if(selected==null||Mouse.current==null)return;
            var mouse=Mouse.current;if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Cancel();return;}
            Vector2 p=mouse.position.ReadValue();if(farmCamera==null)return;
            if(!new Rect(0,0,farmCamera.pixelWidth,farmCamera.pixelHeight).Contains(p))return;
            if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;
            var ray=farmCamera.ScreenPointToRay(p);
            if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out var distance)){SetPreviewColor(Color.red);return;}
            Vector3 point=ray.GetPoint(distance);point.y=groundY;
            preview.transform.position=point;
            bool valid=Valid(point);SetPreviewColor(valid?new Color(.2f,.9f,.35f,.52f):new Color(1f,.18f,.12f,.55f));
            if(mouse.leftButton.wasPressedThisFrame&&valid)
            {
                var tree=Instantiate(selected.ModelPrefab,point,Quaternion.identity);
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="FrutaPronta";marker.transform.SetParent(tree.transform,false);marker.transform.localPosition=Vector3.up*2.4f;marker.transform.localScale=Vector3.one*.2f;Destroy(marker.GetComponent<Collider>());
                tree.AddComponent<SphereCollider>().radius=selected.SpacingRadius;
                tree.AddComponent<FruitTree>().Configure(selected,inventory,tree.transform.GetChild(0),marker);
                Cancel();
            }
        }
        bool Valid(Vector3 p)
        {
            if(p.x<farmMin.x||p.x>farmMax.x||p.z<farmMin.y||p.z>farmMax.y)return false;
            int count=Physics.OverlapSphereNonAlloc(p+Vector3.up*.65f,selected.SpacingRadius,overlaps,blockedLayers,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var c=overlaps[i];if(c==null||c==groundCollider)continue;
                if(c.GetComponentInParent<FruitTree>()!=null||c.GetComponentInParent<FarmPlot>()!=null||c.GetComponentInParent<FarmTreePlacementController>()!=null)return false;
                if(c.GetComponentInParent<MiniFazendinha.Animals.FarmAnimalController>()!=null)return false;
                string n=(c.name+" "+c.transform.root.name).ToLowerInvariant();
                if(n.Contains("caminho")||n.Contains("cerca")||n.Contains("casa")||n.Contains("celeiro")||n.Contains("tree")||n.Contains("arvore"))return false;
                if(c.bounds.size.y<.25f&&c.bounds.max.y<=groundY+.12f)continue;
            }
            return true;
        }
        void SetPreviewColor(Color color)
        {
            if(preview==null)return;foreach(var r in preview.GetComponentsInChildren<Renderer>(true)){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetColor("_BaseColor",color);block.SetColor("_Color",color);r.SetPropertyBlock(block);}
        }
        static void StripColliders(GameObject root){foreach(var c in root.GetComponentsInChildren<Collider>(true))Destroy(c);}
        public void Cancel(){if(preview!=null)Destroy(preview);preview=null;selected=null;foreach(var m in previewMaterials)if(m!=null)Destroy(m);previewMaterials.Clear();}
        void OnDisable(){Cancel();}
    }
}
