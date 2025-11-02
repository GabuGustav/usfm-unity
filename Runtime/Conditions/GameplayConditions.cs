using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime.Conditions
{
    // ========== GAMEOBJECT CONDITIONS ==========

    /// <summary>
    /// Checks if GameObject has a specific component
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/GameObject/Has Component", order = 800)]
    public class HasComponentCondition : ScriptableCondition
    {
        public string ComponentTypeName = "Rigidbody";

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            var componentType = System.Type.GetType(ComponentTypeName);
            if (componentType == null)
            {
                componentType = System.Type.GetType($"UnityEngine.{ComponentTypeName}");
            }

            if (componentType == null) return false;

            return currentContext.Owner.GetComponent(componentType) != null;
        }
    }

    /// <summary>
    /// Checks if GameObject is active
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/GameObject/Is Active", order = 801)]
    public class IsActiveCondition : ScriptableCondition
    {
        public GameObject Target;
        public string TargetTag;
        public bool CheckSelf = true;

        public override bool Evaluate()
        {
            GameObject targetObject = null;

            if (CheckSelf && currentContext?.Owner != null)
            {
                targetObject = currentContext.Owner;
            }
            else if (Target != null)
            {
                targetObject = Target;
            }
            else if (!string.IsNullOrEmpty(TargetTag))
            {
                targetObject = GameObject.FindGameObjectWithTag(TargetTag);
            }

            return targetObject != null && targetObject.activeInHierarchy;
        }
    }

    /// <summary>
    /// Checks if GameObject has a specific tag
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/GameObject/Has Tag", order = 802)]
    public class HasTagCondition : ScriptableCondition
    {
        public string Tag = "Player";

        public override bool Evaluate()
        {
            return currentContext?.Owner != null && currentContext.Owner.CompareTag(Tag);
        }
    }

    /// <summary>
    /// Checks if GameObject is on a specific layer
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/GameObject/Is On Layer", order = 803)]
    public class IsOnLayerCondition : ScriptableCondition
    {
        public LayerMask Layer;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            int objectLayer = currentContext.Owner.layer;
            return ((1 << objectLayer) & Layer) != 0;
        }
    }

    /// <summary>
    /// Checks if there are GameObjects with tag in scene
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/GameObject/Tagged Objects Exist", order = 804)]
    public class TaggedObjectsExistCondition : ScriptableCondition
    {
        public string Tag = "Enemy";
        public int MinCount = 1;
        public int MaxCount = -1; // -1 = no max

        public override bool Evaluate()
        {
            var objects = GameObject.FindGameObjectsWithTag(Tag);
            int count = objects.Length;

            bool meetsMin = count >= MinCount;
            bool meetsMax = MaxCount < 0 || count <= MaxCount;

            return meetsMin && meetsMax;
        }
    }

    // ========== HEALTH & STATS CONDITIONS ==========

    /// <summary>
    /// Checks if health is below/above threshold
    /// Requires a component with "Health" or "CurrentHealth" property/field
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Stats/Health Check", order = 900)]
    public class HealthCheckCondition : ScriptableCondition
    {
        public enum Comparison { Below, BelowOrEqual, Above, AboveOrEqual, InRange }

        public Comparison ComparisonType = Comparison.Below;
        public float Threshold = 30f;
        public float RangeMin = 20f;
        public float RangeMax = 50f;
        public bool UsePercentage = false; // If true, threshold is 0-100%

        public string HealthComponentName = ""; // Optional: specific component name
        public string HealthPropertyName = "CurrentHealth";
        public string MaxHealthPropertyName = "MaxHealth";

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            // Try to find health component
            Component healthComponent = null;

            if (!string.IsNullOrEmpty(HealthComponentName))
            {
                var type = System.Type.GetType(HealthComponentName);
                if (type != null)
                {
                    healthComponent = currentContext.Owner.GetComponent(type);
                }
            }

            if (healthComponent == null)
            {
                // Try common health component types
                healthComponent = currentContext.Owner.GetComponent("Health") as Component;
                if (healthComponent == null)
                {
                    healthComponent = currentContext.Owner.GetComponent("HealthSystem") as Component;
                }
            }

            if (healthComponent == null) return false;

            // Get health value using reflection
            float currentHealth = GetFloatValue(healthComponent, HealthPropertyName);

            if (UsePercentage)
            {
                float maxHealth = GetFloatValue(healthComponent, MaxHealthPropertyName);
                if (maxHealth > 0)
                {
                    currentHealth = (currentHealth / maxHealth) * 100f;
                }
            }

            return ComparisonType switch
            {
                Comparison.Below => currentHealth < Threshold,
                Comparison.BelowOrEqual => currentHealth <= Threshold,
                Comparison.Above => currentHealth > Threshold,
                Comparison.AboveOrEqual => currentHealth >= Threshold,
                Comparison.InRange => currentHealth >= RangeMin && currentHealth <= RangeMax,
                _ => false
            };
        }

        private float GetFloatValue(Component component, string propertyName)
        {
            var type = component.GetType();

            // Try property
            var property = type.GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(component);
                if (value is float f) return f;
                if (value is int i) return i;
            }

            // Try field
            var field = type.GetField(propertyName);
            if (field != null)
            {
                var value = field.GetValue(component);
                if (value is float f) return f;
                if (value is int i) return i;
            }

            return 0f;
        }
    }

    /// <summary>
    /// Checks if health is depleted (dead)
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Stats/Is Dead", order = 901)]
    public class IsDeadCondition : ScriptableCondition
    {
        public string HealthPropertyName = "CurrentHealth";

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            var healthComponent = currentContext.Owner.GetComponent("Health") as Component;
            if (healthComponent == null)
            {
                healthComponent = currentContext.Owner.GetComponent("HealthSystem") as Component;
            }

            if (healthComponent == null) return false;

            var type = healthComponent.GetType();
            var property = type.GetProperty(HealthPropertyName);
            var field = type.GetField(HealthPropertyName);

            float health = 0f;
            if (property != null)
            {
                var value = property.GetValue(healthComponent);
                health = value is float f ? f : (value is int i ? i : 0f);
            }
            else if (field != null)
            {
                var value = field.GetValue(healthComponent);
                health = value is float f ? f : (value is int i ? i : 0f);
            }

            return health <= 0f;
        }
    }

    /// <summary>
    /// Generic stat check condition (works with any numeric property)
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Stats/Stat Check", order = 902)]
    public class StatCheckCondition : ScriptableCondition
    {
        public enum Comparison { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual, InRange }

        public string ComponentName;
        public string PropertyName = "Stamina";
        public Comparison ComparisonType = Comparison.Less;
        public float Value = 20f;
        public float RangeMin = 10f;
        public float RangeMax = 50f;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            Component component = null;

            if (!string.IsNullOrEmpty(ComponentName))
            {
                var type = System.Type.GetType(ComponentName);
                if (type != null)
                {
                    component = currentContext.Owner.GetComponent(type);
                }
            }

            if (component == null) return false;

            float statValue = GetFloatValue(component, PropertyName);

            return ComparisonType switch
            {
                Comparison.Equal => Mathf.Approximately(statValue, Value),
                Comparison.NotEqual => !Mathf.Approximately(statValue, Value),
                Comparison.Less => statValue < Value,
                Comparison.LessOrEqual => statValue <= Value,
                Comparison.Greater => statValue > Value,
                Comparison.GreaterOrEqual => statValue >= Value,
                Comparison.InRange => statValue >= RangeMin && statValue <= RangeMax,
                _ => false
            };
        }

        private float GetFloatValue(Component component, string propertyName)
        {
            var type = component.GetType();
            var property = type.GetProperty(propertyName);
            var field = type.GetField(propertyName);

            if (property != null)
            {
                var value = property.GetValue(component);
                if (value is float f) return f;
                if (value is int i) return i;
            }

            if (field != null)
            {
                var value = field.GetValue(component);
                if (value is float f) return f;
                if (value is int i) return i;
            }

            return 0f;
        }
    }

    // ========== ENVIRONMENTAL CONDITIONS ==========

    /// <summary>
    /// Checks if object is within a specific zone/trigger
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Environmental/In Zone", order = 1000)]
    public class InZoneCondition : ScriptableCondition
    {
        public string ZoneTag = "SafeZone";
        public Collider ZoneCollider;

        private bool isInZone = false;
        private ZoneTracker tracker;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            if (tracker == null)
            {
                tracker = currentContext.Owner.GetComponent<ZoneTracker>();
                if (tracker == null)
                {
                    tracker = currentContext.Owner.AddComponent<ZoneTracker>();
                }
                tracker.Initialize(this, ZoneTag);
            }

            return isInZone;
        }

        public void SetInZone(bool inZone)
        {
            isInZone = inZone;
        }

        public override void Reset()
        {
            isInZone = false;
        }

        public class ZoneTracker : MonoBehaviour
        {
            private InZoneCondition condition;
            private string zoneTag;

            public void Initialize(InZoneCondition cond, string tag)
            {
                condition = cond;
                zoneTag = tag;
            }

            private void OnTriggerEnter(Collider other)
            {
                if (other.CompareTag(zoneTag))
                {
                    condition?.SetInZone(true);
                }
            }

            private void OnTriggerExit(Collider other)
            {
                if (other.CompareTag(zoneTag))
                {
                    condition?.SetInZone(false);
                }
            }
        }
    }

    /// <summary>
    /// Checks game time (hours in a day/night cycle)
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Environmental/Time Of Day", order = 1001)]
    public class TimeOfDayCondition : ScriptableCondition
    {
        [Range(0f, 24f)]
        public float StartHour = 6f; // 6 AM
        [Range(0f, 24f)]
        public float EndHour = 18f; // 6 PM

        public string TimeManagerParameterName = "CurrentHour";
        public bool UseGlobalParameter = true;

        public override bool Evaluate()
        {
            float currentHour;

            if (UseGlobalParameter)
            {
                currentHour = GlobalParams.GetFloat(TimeManagerParameterName);
            }
            else
            {
                currentHour = Parameters?.GetFloat(TimeManagerParameterName) ?? 12f;
            }

            // Handle wrapping (e.g., 22:00 to 06:00 = night)
            if (StartHour <= EndHour)
            {
                return currentHour >= StartHour && currentHour <= EndHour;
            }
            else
            {
                return currentHour >= StartHour || currentHour <= EndHour;
            }
        }
    }

    /// <summary>
    /// Checks if it's daytime or nighttime
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Environmental/Is Daytime", order = 1002)]
    public class IsDaytimeCondition : ScriptableCondition
    {
        public bool CheckForDay = true; // True = check for day, False = check for night

        [Range(0f, 24f)]
        public float DayStartHour = 6f;
        [Range(0f, 24f)]
        public float NightStartHour = 18f;

        public string TimeParameterName = "CurrentHour";
        public bool UseGlobalParameter = true;

        public override bool Evaluate()
        {
            float currentHour;

            if (UseGlobalParameter)
            {
                currentHour = GlobalParams.GetFloat(TimeParameterName);
            }
            else
            {
                currentHour = Parameters?.GetFloat(TimeParameterName) ?? 12f;
            }

            bool isDaytime = currentHour >= DayStartHour && currentHour < NightStartHour;

            return CheckForDay ? isDaytime : !isDaytime;
        }
    }

    /// <summary>
    /// Checks weather condition from parameter
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Environmental/Weather Is", order = 1003)]
    public class WeatherCondition : ScriptableCondition
    {
        public string WeatherParameterName = "CurrentWeather";
        public string ExpectedWeather = "Sunny";
        public bool UseGlobalParameter = true;

        public override bool Evaluate()
        {
            string currentWeather;

            if (UseGlobalParameter)
            {
                currentWeather = GlobalParams.GetString(WeatherParameterName);
            }
            else
            {
                currentWeather = Parameters?.GetString(WeatherParameterName) ?? "";
            }

            return currentWeather.Equals(ExpectedWeather, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    // ========== GAMEPLAY CONDITIONS ==========

    /// <summary>
    /// Checks if player/object has a specific item in inventory
    /// Requires inventory component with HasItem method or Items property
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Gameplay/Has Item", order = 1100)]
    public class HasItemCondition : ScriptableCondition
    {
        public string ItemName = "Key";
        public int MinQuantity = 1;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            // Try to find inventory component
            var inventory = currentContext.Owner.GetComponent("Inventory");
            if (inventory == null) return false;

            var type = inventory.GetType();

            // Try HasItem method
            var hasItemMethod = type.GetMethod("HasItem", new[] { typeof(string) });
            if (hasItemMethod != null)
            {
                var result = hasItemMethod.Invoke(inventory, new object[] { ItemName });
                if (result is bool hasItem) return hasItem;
            }

            // Try GetItemCount method
            var getCountMethod = type.GetMethod("GetItemCount", new[] { typeof(string) });
            if (getCountMethod != null)
            {
                var result = getCountMethod.Invoke(inventory, new object[] { ItemName });
                if (result is int count) return count >= MinQuantity;
            }

            return false;
        }
    }

    /// <summary>
    /// Checks if a quest is complete
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Gameplay/Quest Complete", order = 1101)]
    public class QuestCompleteCondition : ScriptableCondition
    {
        public string QuestName = "Main Quest 1";
        public string QuestManagerParameterName = "CompletedQuests";
        public bool UseGlobalParameter = true;

        public override bool Evaluate()
        {
            string completedQuests;

            if (UseGlobalParameter)
            {
                completedQuests = GlobalParams.GetString(QuestManagerParameterName);
            }
            else
            {
                completedQuests = Parameters?.GetString(QuestManagerParameterName) ?? "";
            }

            return completedQuests.Contains(QuestName);
        }
    }

    /// <summary>
    /// Checks if in combat state
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Gameplay/Is In Combat", order = 1102)]
    public class IsInCombatCondition : ScriptableCondition
    {
        public string CombatParameterName = "InCombat";
        public bool UseGlobalParameter = false;

        public override bool Evaluate()
        {
            if (UseGlobalParameter)
            {
                return GlobalParams.GetBool(CombatParameterName);
            }
            return Parameters?.GetBool(CombatParameterName) ?? false;
        }
    }
}