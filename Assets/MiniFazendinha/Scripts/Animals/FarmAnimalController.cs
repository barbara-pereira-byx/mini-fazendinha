using System;
using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.Animals
{
    [DisallowMultipleComponent, RequireComponent(typeof(FarmAnimalNavigation))]
    public sealed class FarmAnimalController : MonoBehaviour
    {
        [SerializeField] AnimalDefinition definition;
        [SerializeField] string animalId;
        [SerializeField] string displayName;
        [SerializeField] AnimalShelter home;
        [SerializeField] FarmInventory inventory;
        [SerializeField] Animator animator;
        [SerializeField] Transform visualRoot;
        [SerializeField] GameObject productReadyMarker;
        [SerializeField] FeedingPoint[] feedingPoints = Array.Empty<FeedingPoint>();
        Collider[] interactionColliders = Array.Empty<Collider>();

        readonly AnimalStateMachine stateMachine = new AnimalStateMachine();
        FarmAnimalNavigation navigation;
        FeedingPoint careGoal;
        double idleUntil;
        double eatingUntil;
        double fedUntil;
        double productionProgress;
        int productsAvailable;
        Vector3[] returnRoute;
        int returnIndex;
        bool started;

        public AnimalDefinition Definition => definition;
        public string AnimalId => animalId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? definition == null ? "Animal" : definition.DisplayName : displayName;
        public AnimalShelter Home => home;
        public FarmAnimalState State => stateMachine.State;
        public bool IsInsideShelter => State == FarmAnimalState.InsideShelter;
        public int ProductsAvailable => productsAvailable;
        public float ProductionProgress => definition == null ? 0f : Mathf.Clamp01((float)(productionProgress / definition.ProductionSeconds));
        public float SecondsToNextProduct => definition == null ? 0f : Mathf.Max(0f, (float)(definition.ProductionSeconds - productionProgress));
        public float FeedingLevel => Mathf.Clamp01((float)((fedUntil - Time.timeAsDouble) / (definition == null ? 1f : definition.FedForSeconds)));
        public string StateLabel => State switch
        {
            FarmAnimalState.Idle => "Descansando",
            FarmAnimalState.Wander => "Passeando",
            FarmAnimalState.Eating => "Comendo",
            FarmAnimalState.Resting => "Descansando",
            FarmAnimalState.ReturningHome => "Voltando ao abrigo",
            FarmAnimalState.InsideShelter => "Recolhido",
            _ => "Ativo"
        };

        void OnEnable() { FarmAnimalRegistry.Register(this); }
        void OnDisable() { FarmAnimalRegistry.Unregister(this); }

        void Awake()
        {
            navigation = GetComponent<FarmAnimalNavigation>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            interactionColliders = GetComponentsInChildren<Collider>(true);
            if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
        }

        void Start()
        {
            if (definition == null || !definition.IsValid)
            {
                Debug.LogError("Farm animal has an invalid definition.", this);
                enabled = false;
                return;
            }
            if (home != null)
            {
                home.Register(this);
                navigation.Configure(home.RoamingBounds, 0.85f, definition.BodyRadius);
            }
            if (inventory == null) inventory = FindAnyObjectByType<FarmInventory>();
            if (animator != null && definition.AnimatorController != null)
                animator.runtimeAnimatorController = definition.AnimatorController;
            idleUntil = Time.timeAsDouble + UnityEngine.Random.Range(0.4f, 2.5f);
            ChangeState(FarmAnimalState.Idle);
            started = true;
        }

        void Update()
        {
            if (!started) return;
            Produce(Time.deltaTime);
            if (IsInsideShelter) { UpdateMarker(); return; }
            if (State == FarmAnimalState.ReturningHome)
            {
                AdvanceReturn();
                UpdateMarker();
                return;
            }
            if (State == FarmAnimalState.Eating)
            {
                if (Time.timeAsDouble >= eatingUntil)
                {
                    careGoal = null;
                    idleUntil = Time.timeAsDouble + UnityEngine.Random.Range(1f, 3f);
                    ChangeState(FarmAnimalState.Idle);
                }
                UpdateMarker();
                return;
            }
            if (State == FarmAnimalState.Resting)
            {
                if (Time.timeAsDouble >= idleUntil)
                {
                    idleUntil = Time.timeAsDouble + UnityEngine.Random.Range(1f, 3f);
                    ChangeState(FarmAnimalState.Idle);
                }
                UpdateMarker();
                return;
            }
            if (State == FarmAnimalState.Wander)
            {
                var others = FarmAnimalRegistry.All;
                bool arrived = navigation.Advance(definition.WalkSpeed, Time.deltaTime, definition.SeparationRadius, others);
                if (arrived)
                {
                    if (careGoal != null) BeginEating();
                    else
                    {
                        idleUntil = Time.timeAsDouble + UnityEngine.Random.Range(definition.IdleSecondsMin, definition.IdleSecondsMax);
                        ChangeState(UnityEngine.Random.value < 0.18f ? FarmAnimalState.Resting : FarmAnimalState.Idle);
                    }
                }
                else if (!navigation.HasDestination)
                {
                    careGoal = null;
                    idleUntil = Time.timeAsDouble + 1.2f;
                    ChangeState(FarmAnimalState.Idle);
                }
                UpdateMarker();
                return;
            }
            if (Time.timeAsDouble >= idleUntil) ChooseNextActivity();
            UpdateMarker();
        }

        void ChooseNextActivity()
        {
            if (UnityEngine.Random.value < definition.CareChance && TryChooseCarePoint()) return;
            if (UnityEngine.Random.value < definition.RestChance)
            {
                idleUntil = Time.timeAsDouble + UnityEngine.Random.Range(3f, 6f);
                ChangeState(FarmAnimalState.Resting);
                return;
            }
            if (navigation.ChooseWanderPoint())
                ChangeState(FarmAnimalState.Wander);
            else idleUntil = Time.timeAsDouble + 2f;
        }

        bool TryChooseCarePoint()
        {
            if (feedingPoints == null || feedingPoints.Length == 0) return false;
            FeedingPoint best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var point in feedingPoints)
            {
                if (point == null || point.Units <= 0) continue;
                float distance = (point.AnimalStandPoint - transform.position).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = point; }
            }
            if (best == null) return false;
            careGoal = best;
            navigation.SetDestination(best.AnimalStandPoint);
            ChangeState(FarmAnimalState.Wander);
            return true;
        }

        void BeginEating()
        {
            if (careGoal == null || !careGoal.TryConsume())
            {
                careGoal = null;
                idleUntil = Time.timeAsDouble + 1.5f;
                ChangeState(FarmAnimalState.Idle);
                return;
            }
            if (careGoal.Kind == FeedingPointKind.Food) fedUntil = Time.timeAsDouble + definition.FedForSeconds;
            eatingUntil = Time.timeAsDouble + UnityEngine.Random.Range(2.5f, 4f);
            ChangeState(FarmAnimalState.Eating);
        }

        void Produce(float deltaTime)
        {
            if (definition.Product == null || definition.ProductYield <= 0) return;
            float rate = Time.timeAsDouble < fedUntil ? definition.FedProductionMultiplier : 0.42f;
            productionProgress += deltaTime * rate;
            if (productionProgress < definition.ProductionSeconds) return;
            productionProgress -= definition.ProductionSeconds;
            productsAvailable = Mathf.Min(productsAvailable + definition.ProductYield, 99);
        }

        public int CollectProducts()
        {
            if (productsAvailable <= 0 || definition.Product == null || inventory == null) return 0;
            int amount = productsAvailable;
            if (!inventory.TryAdd(definition.Product, amount)) return 0;
            productsAvailable = 0;
            UpdateMarker();
            return amount;
        }

        public void ReturnToShelter(AnimalShelter shelter)
        {
            if (shelter == null || IsInsideShelter || State == FarmAnimalState.ReturningHome) return;
            home = shelter;
            returnRoute = new[] { shelter.GatePoint, shelter.PathPoint, shelter.EntrancePoint };
            returnIndex = 0;
            navigation.SetDestination(returnRoute[returnIndex]);
            ChangeState(FarmAnimalState.ReturningHome);
        }

        void AdvanceReturn()
        {
            if (home == null || returnRoute == null || returnRoute.Length == 0) return;
            if (!navigation.Advance(definition.WalkSpeed, Time.deltaTime, definition.SeparationRadius,
                    FindObjectsByType<FarmAnimalController>(FindObjectsSortMode.None), true)) return;
            returnIndex++;
            if (returnIndex < returnRoute.Length)
            {
                navigation.SetDestination(returnRoute[returnIndex]);
                return;
            }
            home.Register(this);
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
            SetInteractionColliders(false);
            navigation.ClearDestination();
            ChangeState(FarmAnimalState.InsideShelter);
        }

        public void ReleaseFromShelter(AnimalShelter shelter)
        {
            if (shelter == null || !IsInsideShelter) return;
            home = shelter;
            int index = shelter.ResidentIndex(this);
            transform.position = shelter.ReleasePoint(index);
            if (visualRoot != null) visualRoot.gameObject.SetActive(true);
            SetInteractionColliders(true);
            navigation.Configure(shelter.RoamingBounds, 0.85f, definition.BodyRadius);
            idleUntil = Time.timeAsDouble + UnityEngine.Random.Range(0.2f, 2f);
            ChangeState(FarmAnimalState.Idle);
            UpdateMarker();
        }

        public void Assign(AnimalDefinition species, string id, string name, AnimalShelter shelter,
            FarmInventory farmInventory, Animator anim, Transform visual, GameObject readyMarker, FeedingPoint[] carePoints)
        {
            definition = species;
            animalId = id;
            displayName = name;
            home = shelter;
            inventory = farmInventory;
            animator = anim;
            visualRoot = visual;
            productReadyMarker = readyMarker;
            feedingPoints = carePoints ?? Array.Empty<FeedingPoint>();
        }

        void ChangeState(FarmAnimalState state)
        {
            if (!stateMachine.ChangeTo(state)) return;
            if (animator == null || definition == null) return;
            string clip = state == FarmAnimalState.Wander || state == FarmAnimalState.ReturningHome
                ? definition.WalkState
                : state == FarmAnimalState.Eating ? definition.EatingState : definition.IdleState;
            animator.CrossFadeInFixedTime(clip, 0.2f);
        }

        void SetInteractionColliders(bool enabledState)
        {
            foreach (var collider in interactionColliders) if (collider != null) collider.enabled = enabledState;
        }

        void UpdateMarker()
        {
            if (productReadyMarker != null) productReadyMarker.SetActive(productsAvailable > 0);
        }
    }
}