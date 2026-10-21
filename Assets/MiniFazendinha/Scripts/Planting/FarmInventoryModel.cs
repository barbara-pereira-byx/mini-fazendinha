using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
namespace MiniFazendinha.Planting
{
    [Serializable]
    public sealed class InitialCropStock
    {
        public CropDefinition crop;
        [FormerlySerializedAs("seeds"),UnityEngine.Min(0)] public int quantity=12;
    }
    // A single resource balance: planting spends it; harvesting replenishes the same balance.
    public sealed class FarmInventoryModel
    {
        sealed class Stock {public CropDefinition crop;public int quantity;}
        readonly Dictionary<string,Stock> stocks=new Dictionary<string,Stock>(StringComparer.Ordinal);
        public event Action Changed;
        public FarmInventoryModel(IEnumerable<InitialCropStock> initial)
        {
            if(initial==null)throw new ArgumentNullException(nameof(initial));
            foreach(var item in initial)
            {
                if(item==null||item.crop==null||!item.crop.IsValid||item.quantity<0)throw new ArgumentException("Invalid initial crop stock.");
                if(stocks.ContainsKey(item.crop.Identifier))throw new ArgumentException("Duplicate crop identifier: "+item.crop.Identifier);
                stocks.Add(item.crop.Identifier,new Stock{crop=item.crop,quantity=item.quantity});
            }
        }
        bool Find(CropDefinition crop,out Stock stock){stock=null;return crop!=null&&stocks.TryGetValue(crop.Identifier,out stock)&&stock.crop==crop;}
        public int Quantity(CropDefinition crop)=>Find(crop,out var stock)?stock.quantity:0;
        public bool CanReceive(CropDefinition crop,int amount)=>amount>0&&Find(crop,out var stock)&&stock.quantity<=int.MaxValue-amount;
        public bool TrySpend(CropDefinition crop)
        {
            if(!Find(crop,out var stock)||stock.quantity<=0)return false;
            stock.quantity--;Changed?.Invoke();return true;
        }
        public bool TryAdd(CropDefinition crop,int amount)
        {
            if(!CanReceive(crop,amount))return false;
            Find(crop,out var stock);stock.quantity+=amount;Changed?.Invoke();return true;
        }
    }
}