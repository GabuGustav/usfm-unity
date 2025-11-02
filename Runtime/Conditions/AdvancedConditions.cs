using UnityEngine;
using UFSM.Core;
using System.Collections.Generic;

namespace UFSM.Runtime.Conditions
{
    // ========== DISTANCE & PROXIMITY CONDITIONS ==========

    /// <summary>
    /// Checks if object is within range of a target
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Distance/Within Range", order = 400)]
    public class IsWithinRangeCondition : ScriptableCondition
    {
        public Transform Target;
        public string TargetTag; // Alternative: find target by tag
        public float Range = 5f;
        public bool Use2D = false;

        private Transform cachedTarget;

        public override bool Evaluate()
        {
            // Get target
            if (cachedTarget == null)
            {
                if (Target != null)
                {
                    cachedTarget = Target;
                }
                else if (!string.IsNullOrEmpty(TargetTag))
                {
                    var targetObj = GameObject.FindGameObjectWithTag(TargetTag);
                    cachedTarget = targetObj?.transform;
                }
            }

            if (cachedTarget == null || currentContext?.Transform == null) return false;

            // Calculate distance
            float distance = Use2D
                ? Vector2.Distance(currentContext.Transform.position, cachedTarget.position)
                : Vector3.Distance(currentContext.Transform.position, cachedTarget.position);

            return distance <= Range;
        }

        public override void Reset()
        {
            cachedTarget = null;
        }
    }

    /// <summary>
    /// Checks if object is near any object with specific tag
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Distance/Near Tagged Object", order = 401)]
    public class IsNearTaggedObjectCondition : ScriptableCondition
    {
        public string Tag;
        public float Range = 5f;
        public bool Use2D = false;

        public override bool Evaluate()
        {
            if (currentContext?.Transform == null) return false;

            var objects = GameObject.FindGameObjectsWithTag(Tag);
            foreach (var obj in objects)
            {
                float distance = Use2D
                    ? Vector2.Distance(currentContext.Transform.position, obj.transform.position)
                    : Vector3.Distance(currentContext.Transform.position, obj.transform.position);

                if (distance <= Range) return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Checks distance comparison between two transforms
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Distance/Distance Comparison", order = 402)]
    public class DistanceComparisonCondition : ScriptableCondition
    {
        public enum Comparison { Less, LessOrEqual, Greater, GreaterOrEqual }

        public Transform Target;
        public string TargetTag;
        public Comparison ComparisonType = Comparison.Less;
        public float Distance = 5f;
        public bool Use2D = false;

        private Transform cachedTarget;

        public override bool Evaluate()
        {
            // Get target
            if (cachedTarget == null)
            {
                if (Target != null)
                {
                    cachedTarget = Target;
                }
                else if (!string.IsNullOrEmpty(TargetTag))
                {
                    var targetObj = GameObject.FindGameObjectWithTag(TargetTag);
                    cachedTarget = targetObj?.transform;
                }
            }

            if (cachedTarget == null || currentContext?.Transform == null) return false;

            // Calculate distance
            float actualDistance = Use2D
                ? Vector2.Distance(currentContext.Transform.position, cachedTarget.position)
                : Vector3.Distance(currentContext.Transform.position, cachedTarget.position);

            return ComparisonType switch
            {
                Comparison.Less => actualDistance < Distance,
                Comparison.LessOrEqual => actualDistance <= Distance,
                Comparison.Greater => actualDistance > Distance,
                Comparison.GreaterOrEqual => actualDistance >= Distance,
                _ => false
            };
        }
    }

    // ========== ANIMATION CONDITIONS ==========

    /// <summary>
    /// Checks if an animation has finished playing
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Animation/Animation Finished", order = 500)]
    public class AnimationFinishedCondition : ScriptableCondition
    {
        public string AnimationStateName;
        public int LayerIndex = 0;
        [Range(0f, 1f)]
        public float NormalizedTimeThreshold = 0.95f; // Consider finished at 95% completion

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            var animator = currentContext.Owner.GetComponent<Animator>();
            if (animator == null) return false;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(LayerIndex);
            bool isPlayingState = stateInfo.IsName(AnimationStateName);
            bool hasFinished = stateInfo.normalizedTime >= NormalizedTimeThreshold;

            return isPlayingState && hasFinished;
        }
    }

    /// <summary>
    /// Checks if a specific animation is currently playing
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Animation/Is Playing Animation", order = 501)]
    public class IsPlayingAnimationCondition : ScriptableCondition
    {
        public string AnimationStateName;
        public int LayerIndex = 0;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            var animator = currentContext.Owner.GetComponent<Animator>();
            if (animator == null) return false;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(LayerIndex);
            return stateInfo.IsName(AnimationStateName);
        }
    }

    /// <summary>
    /// Checks Animator parameter value
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Animation/Animator Parameter", order = 502)]
    public class AnimatorParameterCondition : ScriptableCondition
    {
        public enum ParameterType { Bool, Int, Float }
        public enum IntComparison { Equal, NotEqual, Greater, Less }
        public enum FloatComparison { Greater, Less, InRange }

        public string ParameterName;
        public ParameterType Type = ParameterType.Bool;

        // Bool
        public bool BoolValue = true;

        // Int
        public IntComparison IntComparisonType = IntComparison.Equal;
        public int IntValue = 0;

        // Float
        public FloatComparison FloatComparisonType = FloatComparison.Greater;
        public float FloatValue = 0f;
        public float FloatRangeMin = 0f;
        public float FloatRangeMax = 1f;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            var animator = currentContext.Owner.GetComponent<Animator>();
            if (animator == null) return false;

            return Type switch
            {
                ParameterType.Bool => animator.GetBool(ParameterName) == BoolValue,
                ParameterType.Int => EvaluateInt(animator.GetInteger(ParameterName)),
                ParameterType.Float => EvaluateFloat(animator.GetFloat(ParameterName)),
                _ => false
            };
        }

        private bool EvaluateInt(int value)
        {
            return IntComparisonType switch
            {
                IntComparison.Equal => value == IntValue,
                IntComparison.NotEqual => value != IntValue,
                IntComparison.Greater => value > IntValue,
                IntComparison.Less => value < IntValue,
                _ => false
            };
        }

        private bool EvaluateFloat(float value)
        {
            return FloatComparisonType switch
            {
                FloatComparison.Greater => value > FloatValue,
                FloatComparison.Less => value < FloatValue,
                FloatComparison.InRange => value >= FloatRangeMin && value <= FloatRangeMax,
                _ => false
            };
        }
    }

    // ========== INPUT CONDITIONS ==========

    /// <summary>
    /// Checks if a key is pressed/held/released
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Input/Key Input", order = 600)]
    public class KeyInputCondition : ScriptableCondition
    {
        public enum InputType { Down, Held, Up }

        public KeyCode Key = KeyCode.Space;
        public InputType Type = InputType.Down;

        public override bool Evaluate()
        {
            return Type switch
            {
                InputType.Down => Input.GetKeyDown(Key),
                InputType.Held => Input.GetKey(Key),
                InputType.Up => Input.GetKeyUp(Key),
                _ => false
            };
        }
    }

    /// <summary>
    /// Checks if a button is pressed/held/released
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Input/Button Input", order = 601)]
    public class ButtonInputCondition : ScriptableCondition
    {
        public enum InputType { Down, Held, Up }

        public string ButtonName = "Jump";
        public InputType Type = InputType.Down;

        public override bool Evaluate()
        {
            return Type switch
            {
                InputType.Down => Input.GetButtonDown(ButtonName),
                InputType.Held => Input.GetButton(ButtonName),
                InputType.Up => Input.GetButtonUp(ButtonName),
                _ => false
            };
        }
    }

    /// <summary>
    /// Checks if an axis value meets a condition
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Input/Axis Input", order = 602)]
    public class AxisInputCondition : ScriptableCondition
    {
        public enum Comparison { Greater, Less, InRange, OutOfRange }

        public string AxisName = "Horizontal";
        public Comparison ComparisonType = Comparison.Greater;
        public float Value = 0.1f;
        public float RangeMin = -0.1f;
        public float RangeMax = 0.1f;

        public override bool Evaluate()
        {
            float axisValue = Input.GetAxis(AxisName);

            return ComparisonType switch
            {
                Comparison.Greater => axisValue > Value,
                Comparison.Less => axisValue < Value,
                Comparison.InRange => axisValue >= RangeMin && axisValue <= RangeMax,
                Comparison.OutOfRange => axisValue < RangeMin || axisValue > RangeMax,
                _ => false
            };
        }
    }

    // ========== PHYSICS CONDITIONS ==========

    /// <summary>
    /// Checks if object is grounded (raycast downward)
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Physics/Is Grounded", order = 700)]
    public class IsGroundedCondition : ScriptableCondition
    {
        public float RayDistance = 0.1f;
        public LayerMask GroundLayer = ~0; // All layers by default
        public Vector3 RayOffset = Vector3.zero;

        public override bool Evaluate()
        {
            if (currentContext?.Transform == null) return false;

            Vector3 origin = currentContext.Transform.position + RayOffset;
            return Physics.Raycast(origin, Vector3.down, RayDistance, GroundLayer);
        }
    }

    /// <summary>
    /// Checks if object is grounded (2D version)
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Physics/Is Grounded 2D", order = 701)]
    public class IsGrounded2DCondition : ScriptableCondition
    {
        public float RayDistance = 0.1f;
        public LayerMask GroundLayer = ~0;
        public Vector2 RayOffset = Vector2.zero;

        public override bool Evaluate()
        {
            if (currentContext?.Transform == null) return false;

            Vector2 origin = (Vector2)currentContext.Transform.position + RayOffset;
            return Physics2D.Raycast(origin, Vector2.down, RayDistance, GroundLayer);
        }
    }

    /// <summary>
    /// Checks if object has velocity above threshold
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Physics/Has Velocity", order = 702)]
    public class HasVelocityCondition : ScriptableCondition
    {
        public enum Comparison { Greater, Less, InRange }
        public enum VelocityType { Magnitude, Horizontal, Vertical }

        public VelocityType Type = VelocityType.Magnitude;
        public Comparison ComparisonType = Comparison.Greater;
        public float Threshold = 0.1f;
        public float RangeMin = 0f;
        public float RangeMax = 10f;
        public bool Use2D = false;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            float velocity = 0f;

            if (Use2D)
            {
                var rb = currentContext.Owner.GetComponent<Rigidbody2D>();
                if (rb == null) return false;

                velocity = Type switch
                {
                    VelocityType.Magnitude => rb.linearVelocity.magnitude,
                    VelocityType.Horizontal => Mathf.Abs(rb.linearVelocity.x),
                    VelocityType.Vertical => Mathf.Abs(rb.linearVelocity.y),
                    _ => 0f
                };
            }
            else
            {
                var rb = currentContext.Owner.GetComponent<Rigidbody>();
                if (rb == null) return false;

                velocity = Type switch
                {
                    VelocityType.Magnitude => rb.linearVelocity.magnitude,
                    VelocityType.Horizontal => new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude,
                    VelocityType.Vertical => Mathf.Abs(rb.linearVelocity.y),
                    _ => 0f
                };
            }

            return ComparisonType switch
            {
                Comparison.Greater => velocity > Threshold,
                Comparison.Less => velocity < Threshold,
                Comparison.InRange => velocity >= RangeMin && velocity <= RangeMax,
                _ => false
            };
        }
    }

    /// <summary>
    /// Checks if object is colliding with specific tag/layer
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/Conditions/Physics/Is Colliding", order = 703)]
    public class IsCollidingCondition : ScriptableCondition
    {
        public enum CheckType { Tag, Layer, Both }

        public CheckType Type = CheckType.Tag;
        public string Tag = "Enemy";
        public LayerMask Layer = ~0;
        public bool Use2D = false;

        private bool isColliding = false;
        private CollisionTracker tracker;

        public override bool Evaluate()
        {
            if (currentContext?.Owner == null) return false;

            // Ensure collision tracker exists
            if (tracker == null)
            {
                tracker = currentContext.Owner.GetComponent<CollisionTracker>();
                if (tracker == null)
                {
                    tracker = currentContext.Owner.AddComponent<CollisionTracker>();
                }
                tracker.Initialize(this);
            }

            return isColliding;
        }

        public void SetColliding(GameObject other)
        {
            isColliding = Type switch
            {
                CheckType.Tag => other.CompareTag(Tag),
                CheckType.Layer => ((1 << other.layer) & Layer) != 0,
                CheckType.Both => other.CompareTag(Tag) && ((1 << other.layer) & Layer) != 0,
                _ => false
            };
        }

        public override void Reset()
        {
            isColliding = false;
        }

        // Helper component for tracking collisions
        public class CollisionTracker : MonoBehaviour
        {
            private IsCollidingCondition condition;

            public void Initialize(IsCollidingCondition cond)
            {
                condition = cond;
            }

            private void OnCollisionEnter(Collision collision)
            {
                condition?.SetColliding(collision.gameObject);
            }

            private void OnCollisionExit(Collision collision)
            {
                condition?.SetColliding(null);
            }

            private void OnCollisionEnter2D(Collision2D collision)
            {
                condition?.SetColliding(collision.gameObject);
            }

            private void OnCollisionExit2D(Collision2D collision)
            {
                condition?.SetColliding(null);
            }
        }
    }
}