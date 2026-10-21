using System;
using UnityEngine;

namespace MiniFazendinha.Planting
{
    [DisallowMultipleComponent]
    public sealed class FarmPlot : MonoBehaviour
    {
        readonly FarmPlotModel model = new FarmPlotModel();
        public FarmPlotState State => model.State;
        public CropDefinition Crop => model.Crop;
        public float Progress => model.Progress(Time.timeAsDouble);
        public double RemainingSeconds => model.Remaining(Time.timeAsDouble);
        public event Action Changed;
        void Update() {if(model.Advance(Time.timeAsDouble))Changed?.Invoke();}
        public PlotActionResult TryPlant(CropDefinition crop,FarmInventory inventory)
        {
            var result=model.TryPlant(crop,inventory==null?null:inventory.Model,Time.timeAsDouble);
            if(result==PlotActionResult.Planted)Changed?.Invoke();return result;
        }
        public PlotActionResult TryHarvest(FarmInventory inventory)
        {
            var result=model.TryHarvest(inventory==null?null:inventory.Model,Time.timeAsDouble);
            if(result==PlotActionResult.Harvested)Changed?.Invoke();return result;
        }
    }
}