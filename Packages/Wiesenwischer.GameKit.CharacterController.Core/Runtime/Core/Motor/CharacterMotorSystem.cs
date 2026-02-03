using System.Collections.Generic;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Motor
{
    /// <summary>
    /// The system that manages the simulation of CharacterMotor
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CharacterMotorSystem : MonoBehaviour
    {
        private static CharacterMotorSystem _instance;

        public static List<CharacterMotor> CharacterMotors = new List<CharacterMotor>();

        private static float _lastCustomInterpolationStartTime = -1f;
        private static float _lastCustomInterpolationDeltaTime = -1f;

        public static CharacterMotorSettings Settings;

        /// <summary>
        /// Creates a CharacterMotorSystem instance if there isn't already one
        /// </summary>
        public static void EnsureCreation()
        {
            if (_instance == null)
            {
                GameObject systemGameObject = new GameObject("CharacterMotorSystem");
                _instance = systemGameObject.AddComponent<CharacterMotorSystem>();

                systemGameObject.hideFlags = HideFlags.NotEditable;
                _instance.hideFlags = HideFlags.NotEditable;

                Settings = ScriptableObject.CreateInstance<CharacterMotorSettings>();

                GameObject.DontDestroyOnLoad(systemGameObject);
            }
        }

        /// <summary>
        /// Gets the CharacterMotorSystem instance if any
        /// </summary>
        /// <returns></returns>
        public static CharacterMotorSystem GetInstance()
        {
            return _instance;
        }

        /// <summary>
        /// Sets the maximum capacity of the character motors list, to prevent allocations when adding characters
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetCharacterMotorsCapacity(int capacity)
        {
            if (capacity < CharacterMotors.Count)
            {
                capacity = CharacterMotors.Count;
            }
            CharacterMotors.Capacity = capacity;
        }

        /// <summary>
        /// Registers a CharacterMotor into the system
        /// </summary>
        public static void RegisterCharacterMotor(CharacterMotor motor)
        {
            CharacterMotors.Add(motor);
        }

        /// <summary>
        /// Unregisters a CharacterMotor from the system
        /// </summary>
        public static void UnregisterCharacterMotor(CharacterMotor motor)
        {
            CharacterMotors.Remove(motor);
        }

        // This is to prevent duplicating the singleton gameobject on script recompiles
        private void OnDisable()
        {
            Destroy(this.gameObject);
        }

        private void Awake()
        {
            _instance = this;
        }

        private void FixedUpdate()
        {
            if (Settings.AutoSimulation)
            {
                float deltaTime = Time.deltaTime;

                if (Settings.Interpolate)
                {
                    PreSimulationInterpolationUpdate(deltaTime);
                }

                Simulate(deltaTime, CharacterMotors);

                if (Settings.Interpolate)
                {
                    PostSimulationInterpolationUpdate(deltaTime);
                }
            }
        }

        private void LateUpdate()
        {
            if (Settings.Interpolate)
            {
                CustomInterpolationUpdate();
            }
        }

        /// <summary>
        /// Remembers the point to interpolate from for CharacterMotors
        /// </summary>
        public static void PreSimulationInterpolationUpdate(float deltaTime)
        {
            // Save pre-simulation poses and place transform at transient pose
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                CharacterMotor motor = CharacterMotors[i];

                motor.InitialTickPosition = motor.TransientPosition;
                motor.InitialTickRotation = motor.TransientRotation;

                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }
        }

        /// <summary>
        /// Ticks characters
        /// </summary>
        public static void Simulate(float deltaTime, List<CharacterMotor> motors)
        {
            int characterMotorsCount = motors.Count;

#pragma warning disable 0162
            // Character controller update phase 1
            for (int i = 0; i < characterMotorsCount; i++)
            {
                motors[i].UpdatePhase1(deltaTime);
            }

            // Character controller update phase 2 and move
            for (int i = 0; i < characterMotorsCount; i++)
            {
                CharacterMotor motor = motors[i];

                motor.UpdatePhase2(deltaTime);

                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }
#pragma warning restore 0162
        }

        /// <summary>
        /// Initiates the interpolation for CharacterMotors
        /// </summary>
        public static void PostSimulationInterpolationUpdate(float deltaTime)
        {
            _lastCustomInterpolationStartTime = Time.time;
            _lastCustomInterpolationDeltaTime = deltaTime;

            // Return interpolated roots to their initial poses
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                CharacterMotor motor = CharacterMotors[i];

                motor.Transform.SetPositionAndRotation(motor.InitialTickPosition, motor.InitialTickRotation);
            }
        }

        /// <summary>
        /// Handles per-frame interpolation
        /// </summary>
        private static void CustomInterpolationUpdate()
        {
            float interpolationFactor = Mathf.Clamp01((Time.time - _lastCustomInterpolationStartTime) / _lastCustomInterpolationDeltaTime);

            // Handle characters interpolation
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                CharacterMotor motor = CharacterMotors[i];

                motor.Transform.SetPositionAndRotation(
                    Vector3.Lerp(motor.InitialTickPosition, motor.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(motor.InitialTickRotation, motor.TransientRotation, interpolationFactor));
            }
        }
    }

    /// <summary>
    /// Settings for the CharacterMotorSystem
    /// </summary>
    public class CharacterMotorSettings : ScriptableObject
    {
        /// <summary>
        /// If true, the system will automatically simulate motors in FixedUpdate
        /// </summary>
        public bool AutoSimulation = true;

        /// <summary>
        /// If true, the system will interpolate motor positions in LateUpdate
        /// </summary>
        public bool Interpolate = true;
    }
}
