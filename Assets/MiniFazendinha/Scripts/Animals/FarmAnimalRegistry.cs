using System.Collections.Generic;

namespace MiniFazendinha.Animals
{
    public static class FarmAnimalRegistry
    {
        static readonly HashSet<FarmAnimalController> ActiveAnimals = new HashSet<FarmAnimalController>();
        public static IEnumerable<FarmAnimalController> All => ActiveAnimals;
        public static void Register(FarmAnimalController animal) { if (animal != null) ActiveAnimals.Add(animal); }
        public static void Unregister(FarmAnimalController animal) { if (animal != null) ActiveAnimals.Remove(animal); }
    }
}