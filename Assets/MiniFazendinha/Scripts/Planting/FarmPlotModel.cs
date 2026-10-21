using System;

namespace MiniFazendinha.Planting
{
    public enum FarmPlotState { Empty, Growing, ReadyToHarvest }
    public enum PlotActionResult { Planted, Harvested, StillGrowing, NoStock, NoSelection, InvalidCrop, InventoryFull, Occupied, NotReady }
    public sealed class FarmPlotModel
    {
        public FarmPlotState State { get; private set; }
        public CropDefinition Crop { get; private set; }
        double startedAt, readyAt;
        int plantedYield;
        public bool Advance(double now)
        {
            if(State!=FarmPlotState.Growing || now<readyAt)return false;
            State=FarmPlotState.ReadyToHarvest;return true;
        }
        public float Progress(double now) => State==FarmPlotState.Empty?0:State==FarmPlotState.ReadyToHarvest?1:(float)Math.Clamp((now-startedAt)/(readyAt-startedAt),0,1);
        public double Remaining(double now) => State==FarmPlotState.Empty?0:Math.Max(0,readyAt-now);
        public PlotActionResult TryPlant(CropDefinition crop,FarmInventoryModel inventory,double now)
        {
            if(State!=FarmPlotState.Empty)return PlotActionResult.Occupied;
            if(crop==null)return PlotActionResult.NoSelection;
            if(!crop.IsValid || inventory==null || double.IsNaN(now) || double.IsInfinity(now))return PlotActionResult.InvalidCrop;
            // Reserve the plot BEFORE inventory notifications; reentrant clicks cannot spend twice.
            Crop=crop;State=FarmPlotState.Growing;startedAt=now;readyAt=now+crop.GrowthSeconds;plantedYield=crop.YieldAmount;
            if(!inventory.TrySpend(crop)) {Clear();return PlotActionResult.NoStock;}
            return PlotActionResult.Planted;
        }
        public PlotActionResult TryHarvest(FarmInventoryModel inventory,double now)
        {
            Advance(now);
            if(State!=FarmPlotState.ReadyToHarvest)return PlotActionResult.NotReady;
            if(inventory==null || !inventory.CanReceive(Crop,plantedYield))return PlotActionResult.InventoryFull;
            var harvested=Crop;int amount=plantedYield;
            // Empty the plot BEFORE notifications; a second harvest cannot credit this crop again.
            Clear();inventory.TryAdd(harvested,amount);return PlotActionResult.Harvested;
        }
        void Clear() {State=FarmPlotState.Empty;Crop=null;startedAt=readyAt=0;plantedYield=0;}
    }
}