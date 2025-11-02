using System.Collections.Generic;
using UFSM.Core;
using UnityEngine;

namespace UFSM.Runtime.Conditions
{
    // ========== PARAMETER CONDITIONS ==========

    /// <summary>
    /// Checks if a bool parameter equals a specific value
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Parameter/Bool Equals", order = 100)]
    public class ParameterBoolCondition : ScriptableCondition
    {
        public string ParameterName;
        public bool ExpectedValue = true;
        public bool UseGlobalParameter = false;

        public ParameterBoolCondition() { }
        public ParameterBoolCondition(string paramName, bool expectedValue)
        {
            ParameterName = paramName;
            ExpectedValue = expectedValue;
        }

        public IContext Context { get; internal set; }

        public override bool Evaluate()
        {
            if (UseGlobalParameter)
            {
                return GlobalParams.GetBool(ParameterName) == ExpectedValue;
            }
            return Parameters?.GetBool(ParameterName) == ExpectedValue;
        }
    }

    /// <summary>
    /// Checks if an int parameter meets a comparison condition
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Parameter/Int Comparison", order = 101)]
    public class ParameterIntCondition : ScriptableCondition
    {
        public enum Comparison { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }

        public string ParameterName;
        public Comparison ComparisonType = Comparison.Equal;
        public int Value = 0;
        public bool UseGlobalParameter = false;

        public IContext Context { get; internal set; }

        public ParameterIntCondition(string parameterName, InlineConditionDefinition.IntComparison intComparisonType, int intValue)
        {
            ParameterName = parameterName;
        }
        public ParameterIntCondition(string paramName, Comparison comparison, int value)
        {
            ParameterName = paramName;
            ComparisonType = comparison;
            Value = value;
        }

        public override bool Evaluate()
        {
            int paramValue = UseGlobalParameter
                ? GlobalParams.GetInt(ParameterName)
                : Parameters?.GetInt(ParameterName) ?? 0;

            return ComparisonType switch
            {
                Comparison.Equal => paramValue == Value,
                Comparison.NotEqual => paramValue != Value,
                Comparison.Greater => paramValue > Value,
                Comparison.GreaterOrEqual => paramValue >= Value,
                Comparison.Less => paramValue < Value,
                Comparison.LessOrEqual => paramValue <= Value,
                _ => false
            };
        }
    }

    /// <summary>
    /// Checks if a float parameter meets a comparison condition
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Parameter/Float Comparison", order = 102)]
    public class ParameterFloatCondition : ScriptableCondition
    {
        public enum Comparison { Greater, GreaterOrEqual, Less, LessOrEqual, InRange, OutOfRange }

        public string ParameterName;
        public Comparison ComparisonType = Comparison.Greater;
        public float Value = 0f;
        public float RangeMin = 0f;
        public float RangeMax = 1f;
        public bool UseGlobalParameter = false;

        public ParameterFloatCondition() { }
        public ParameterFloatCondition(string paramName, Comparison comparison, float value, float rangeMin = 0f, float rangeMax = 1f)
        {
            ParameterName = paramName;
            ComparisonType = comparison;
            Value = value;
            RangeMin = rangeMin;
            RangeMax = rangeMax;
        }

        public override bool Evaluate()
        {
            float paramValue = UseGlobalParameter
                ? GlobalParams.GetFloat(ParameterName)
                : Parameters?.GetFloat(ParameterName) ?? 0f;

            return ComparisonType switch
            {
                Comparison.Greater => paramValue > Value,
                Comparison.GreaterOrEqual => paramValue >= Value,
                Comparison.Less => paramValue < Value,
                Comparison.LessOrEqual => paramValue <= Value,
                Comparison.InRange => paramValue >= RangeMin && paramValue <= RangeMax,
                Comparison.OutOfRange => paramValue < RangeMin || paramValue > RangeMax,
                _ => false
            };
        }
    }

    /// <summary>
    /// Checks if a trigger parameter is set
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Parameter/Trigger Is Set", order = 103)]
    public class ParameterTriggerCondition : ScriptableCondition
    {
        public string TriggerName;
        public bool UseGlobalParameter = false;
        internal IContext Context;

        public ParameterTriggerCondition() { }
        public ParameterTriggerCondition(string triggerName)
        {
            TriggerName = triggerName;
        }

        public override bool Evaluate()
        {
            if (UseGlobalParameter)
            {
                return GlobalParams.IsTriggerSet(TriggerName);
            }
            return Parameters?.IsTriggerSet(TriggerName) ?? false;
        }
    }

    /// <summary>
    /// Checks if a string parameter equals a specific value
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Parameter/String Equals", order = 104)]
    public class ParameterStringCondition : ScriptableCondition
    {
        public string ParameterName;
        public string ExpectedValue = "";
        public bool CaseSensitive = true;
        public bool UseGlobalParameter = false;

        public override bool Evaluate()
        {
            string paramValue = UseGlobalParameter
                ? GlobalParams.GetString(ParameterName)
                : Parameters?.GetString(ParameterName) ?? "";

            if (CaseSensitive)
            {
                return paramValue == ExpectedValue;
            }
            return paramValue.Equals(ExpectedValue, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    // ========== TIMER CONDITIONS ==========

    /// <summary>
    /// Checks if a certain amount of time has elapsed since state entered
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Timer/Time Elapsed", order = 200)]
    public class TimerCondition : ScriptableCondition
    {
        public float Duration = 1f;
        internal IContext Context;
        private float elapsedTime = 0f;
        private bool isActive = false;

        public TimerCondition() { }
        public TimerCondition(float duration)
        {
            Duration = duration;
        }

        public override bool Evaluate()
        {
            if (!isActive)
            {
                isActive = true;
                elapsedTime = 0f;
            }

            elapsedTime += Time.deltaTime;
            return elapsedTime >= Duration;
        }

        public override void Reset()
        {
            elapsedTime = 0f;
            isActive = false;
        }
    }

    /// <summary>
    /// Cooldown condition - prevents transition until cooldown expires
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Timer/Cooldown", order = 201)]
    public class CooldownCondition : ScriptableCondition
    {
        public float CooldownDuration = 2f;
        private float lastTriggerTime = -999f;

        public override bool Evaluate()
        {
            float currentTime = Time.time;
            if (currentTime - lastTriggerTime >= CooldownDuration)
            {
                lastTriggerTime = currentTime;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            lastTriggerTime = -999f;
        }
    }

    /// <summary>
    /// Checks if current time is within a specific time window
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Timer/Time Window", order = 202)]
    public class TimeWindowCondition : ScriptableCondition
    {
        public float StartTime = 0f;
        public float EndTime = 5f;
        private float startTimestamp = 0f;
        private bool initialized = false;

        public override bool Evaluate()
        {
            if (!initialized)
            {
                startTimestamp = Time.time;
                initialized = true;
            }

            float elapsed = Time.time - startTimestamp;
            return elapsed >= StartTime && elapsed <= EndTime;
        }

        public override void Reset()
        {
            initialized = false;
        }
    }

    // ========== RANDOM CONDITIONS ==========

    /// <summary>
    /// Random chance condition (e.g., 50% chance to transition)
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Random/Random Chance", order = 300)]
    public class RandomCondition : ScriptableCondition
    {
        [Range(0f, 1f)]
        public float Chance = 0.5f;
        public bool EvaluateOnce = true; // If true, only rolls once per Reset

        private bool hasEvaluated = false;
        private bool cachedResult = false;

        public RandomCondition() { }
        public RandomCondition(float chance)
        {
            Chance = chance;
        }

        public IContext Context { get; internal set; }

        public override bool Evaluate()
        {
            if (EvaluateOnce && hasEvaluated)
            {
                return cachedResult;
            }

            cachedResult = Random.value <= Chance;
            hasEvaluated = true;
            return cachedResult;
        }

        public override void Reset()
        {
            hasEvaluated = false;
            cachedResult = false;
        }
    }

    /// <summary>
    /// Random chance with weighted options (returns true for specific outcome)
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Random/Weighted Random", order = 301)]
    public class WeightedRandomCondition : ScriptableCondition
    {
        [System.Serializable]
        public class WeightedOption
        {
            public string OptionName;
            public float Weight = 1f;
        }

        public List<WeightedOption> Options = new List<WeightedOption>();
        public string ExpectedOutcome;
        public bool EvaluateOnce = true;

        private bool hasEvaluated = false;
        private string cachedOutcome = "";

        public override bool Evaluate()
        {
            if (EvaluateOnce && hasEvaluated)
            {
                return cachedOutcome == ExpectedOutcome;
            }

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var option in Options)
            {
                totalWeight += option.Weight;
            }

            // Roll random value
            float roll = Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var option in Options)
            {
                cumulative += option.Weight;
                if (roll <= cumulative)
                {
                    cachedOutcome = option.OptionName;
                    break;
                }
            }

            hasEvaluated = true;
            return cachedOutcome == ExpectedOutcome;
        }

        public override void Reset()
        {
            hasEvaluated = false;
            cachedOutcome = "";
        }
    }
}