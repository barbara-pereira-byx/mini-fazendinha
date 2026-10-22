using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using MiniFazendinha.Planting;
using MiniFazendinha.FruitTrees;
using MiniFazendinha.UI;
namespace MiniFazendinha.Animals
{
    // One world-selection router. Specific objects always outrank the pen's broad trigger.
    [DefaultExecutionOrder(50)]
    public sealed class FarmAnimalContextUI:MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] FarmInventory inventory;
        FarmHUD hud;
        public FarmManagementView Management {get;private set;}
        void Start(){if(viewCamera==null)viewCamera=Camera.main;if(inventory==null)inventory=FindAnyObjectByType<FarmInventory>();hud=FindAnyObjectByType<FarmHUD>();Management=new FarmManagementView(hud.GetComponent<Canvas>(),inventory,()=>hud.CloseMenus());}
        public void CloseManagement()=>Management?.Close();
        void Update()
        {
            if(Management==null)return;Management.Tick();
            if(Management.IsOpen&&(hud.ContextMenu.IsOpen||hud.Warehouse.IsOpen))Management.Close();
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Management.Back();return;}
            if(FindAnyObjectByType<FarmTreePlacementController>()?.IsPlacing==true)return;if(Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame)return;
            var p=Mouse.current.position.ReadValue();if(FarmInputPriority.IsOverUI(p)||viewCamera==null)return;
            var hits=Physics.RaycastAll(viewCamera.ScreenPointToRay(p),viewCamera.farClipPlane,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).ToArray();
            foreach(var hit in hits){var a=hit.collider.GetComponentInParent<FarmAnimalController>();if(a!=null){Management.ShowAnimal(a);return;}}
            foreach(var hit in hits){var point=hit.collider.GetComponentInParent<FeedingPoint>();if(point!=null){Management.ShowTrough(point,p);return;}}
            foreach(var hit in hits){var w=hit.collider.GetComponentInParent<WaterWellController>();if(w!=null){Management.ShowWell(w,p);return;}var tree=hit.collider.GetComponentInParent<FruitTree>();if(tree!=null){Management.ShowTree(tree,p);return;}if(hit.collider.GetComponentInParent<FarmWarehouseBuilding>()!=null){Management.ShowWarehouse();return;}}
            foreach(var hit in hits){var s=hit.collider.GetComponentInParent<AnimalShelter>();if(s!=null&&s.PermittedSpecies!=null){Management.ShowShelter(s);return;}}
            Management.Close();
        }
    }
}
