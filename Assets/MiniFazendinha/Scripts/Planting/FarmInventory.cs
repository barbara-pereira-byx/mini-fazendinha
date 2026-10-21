using System;
using UnityEngine;

namespace MiniFazendinha.Planting
{
    [DisallowMultipleComponent]
    public sealed class FarmInventory : MonoBehaviour
    {
        [SerializeField] InitialCropStock[] initialStock = Array.Empty<InitialCropStock>();
        FarmInventoryModel model;
        public FarmInventoryModel Model { get {EnsureInitialized();return model;} }
        public event Action Changed;
        public InitialCropStock[] InitialStock => initialStock;
        void Awake() {EnsureInitialized();}
        void EnsureInitialized()
        {
            if(model!=null)return;
            model=new FarmInventoryModel(initialStock);model.Changed+=Notify;
        }
        void Notify() {Changed?.Invoke();}
        void OnDestroy() {if(model!=null)model.Changed-=Notify;}
        public int Quantity(CropDefinition crop) => Model.Quantity(crop);
    }
}
