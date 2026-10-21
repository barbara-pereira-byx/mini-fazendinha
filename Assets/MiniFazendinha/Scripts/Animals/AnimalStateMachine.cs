namespace MiniFazendinha.Animals
{
    public enum FarmAnimalState { Idle, Wander, Eating, Resting, ReturningHome, InsideShelter }

    public sealed class AnimalStateMachine
    {
        public FarmAnimalState State { get; private set; } = FarmAnimalState.Idle;
        bool initialized;
        public bool ChangeTo(FarmAnimalState next)
        {
            if (initialized && State == next) return false;
            State = next;
            initialized = true;
            return true;
        }
    }
}
