using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Custom Editor für LocomotionConfig ScriptableObject.
    /// Zeigt Parameter übersichtlich gruppiert an.
    /// </summary>
    [CustomEditor(typeof(LocomotionConfig))]
    public class LocomotionConfigEditor : UnityEditor.Editor
    {
        // Foldout States
        private bool _groundMovementFoldout = true;
        private bool _airMovementFoldout = true;
        private bool _jumpingFoldout = true;
        private bool _groundDetectionFoldout = true;
        private bool _rotationFoldout = true;
        private bool _stepDetectionFoldout = false;
        private bool _slopeSpeedFoldout = true;
        private bool _slopeSlidingFoldout = false;
        private bool _landingFoldout = true;
        private bool _landingRollFoldout = false;
        private bool _crouchingFoldout = false;

        // Serialized Properties
        private SerializedProperty _walkSpeed;
        private SerializedProperty _runSpeed;
        private SerializedProperty _acceleration;
        private SerializedProperty _deceleration;

        private SerializedProperty _airControl;
        private SerializedProperty _gravity;
        private SerializedProperty _maxFallSpeed;

        private SerializedProperty _jumpHeight;
        private SerializedProperty _jumpDuration;
        private SerializedProperty _coyoteTime;
        private SerializedProperty _jumpBufferTime;

        private SerializedProperty _groundCheckDistance;
        private SerializedProperty _groundCheckRadius;
        private SerializedProperty _groundLayers;
        private SerializedProperty _maxSlopeAngle;
        private SerializedProperty _groundDetection;
        private SerializedProperty _fallDetection;
        private SerializedProperty _groundToFallRayDistance;

        private SerializedProperty _rotationSpeed;
        private SerializedProperty _rotateTowardsMovement;

        private SerializedProperty _maxStepHeight;
        private SerializedProperty _minStepDepth;
        private SerializedProperty _stairSpeedReductionEnabled;
        private SerializedProperty _stairSpeedReduction;

        private SerializedProperty _uphillSpeedPenalty;
        private SerializedProperty _downhillSpeedBonus;

        private SerializedProperty _slopeSlideSpeed;
        private SerializedProperty _useSlopeDependentSlideSpeed;

        private SerializedProperty _softLandingThreshold;
        private SerializedProperty _hardLandingThreshold;
        private SerializedProperty _softLandingDuration;
        private SerializedProperty _hardLandingDuration;

        private SerializedProperty _rollEnabled;
        private SerializedProperty _rollTriggerMode;
        private SerializedProperty _rollSpeedModifier;

        private SerializedProperty _crouchHeight;
        private SerializedProperty _standingHeight;
        private SerializedProperty _crouchSpeed;
        private SerializedProperty _crouchAcceleration;
        private SerializedProperty _crouchDeceleration;
        private SerializedProperty _crouchTransitionDuration;
        private SerializedProperty _crouchHeadClearanceMargin;
        private SerializedProperty _canJumpFromCrouch;
        private SerializedProperty _canSprintFromCrouch;
        private SerializedProperty _crouchStepHeight;

        private void OnEnable()
        {
            // Ground Movement
            _walkSpeed = serializedObject.FindProperty("_walkSpeed");
            _runSpeed = serializedObject.FindProperty("_runSpeed");
            _acceleration = serializedObject.FindProperty("_acceleration");
            _deceleration = serializedObject.FindProperty("_deceleration");

            // Air Movement
            _airControl = serializedObject.FindProperty("_airControl");
            _gravity = serializedObject.FindProperty("_gravity");
            _maxFallSpeed = serializedObject.FindProperty("_maxFallSpeed");

            // Jumping
            _jumpHeight = serializedObject.FindProperty("_jumpHeight");
            _jumpDuration = serializedObject.FindProperty("_jumpDuration");
            _coyoteTime = serializedObject.FindProperty("_coyoteTime");
            _jumpBufferTime = serializedObject.FindProperty("_jumpBufferTime");

            // Ground Detection
            _groundCheckDistance = serializedObject.FindProperty("_groundCheckDistance");
            _groundCheckRadius = serializedObject.FindProperty("_groundCheckRadius");
            _groundLayers = serializedObject.FindProperty("_groundLayers");
            _maxSlopeAngle = serializedObject.FindProperty("_maxSlopeAngle");
            _groundDetection = serializedObject.FindProperty("_groundDetection");
            _fallDetection = serializedObject.FindProperty("_fallDetection");
            _groundToFallRayDistance = serializedObject.FindProperty("_groundToFallRayDistance");

            // Rotation
            _rotationSpeed = serializedObject.FindProperty("_rotationSpeed");
            _rotateTowardsMovement = serializedObject.FindProperty("_rotateTowardsMovement");

            // Step Detection
            _maxStepHeight = serializedObject.FindProperty("_maxStepHeight");
            _minStepDepth = serializedObject.FindProperty("_minStepDepth");
            _stairSpeedReductionEnabled = serializedObject.FindProperty("_stairSpeedReductionEnabled");
            _stairSpeedReduction = serializedObject.FindProperty("_stairSpeedReduction");

            // Slope Speed
            _uphillSpeedPenalty = serializedObject.FindProperty("_uphillSpeedPenalty");
            _downhillSpeedBonus = serializedObject.FindProperty("_downhillSpeedBonus");

            // Slope Sliding
            _slopeSlideSpeed = serializedObject.FindProperty("_slopeSlideSpeed");
            _useSlopeDependentSlideSpeed = serializedObject.FindProperty("_useSlopeDependentSlideSpeed");

            // Landing
            _softLandingThreshold = serializedObject.FindProperty("_softLandingThreshold");
            _hardLandingThreshold = serializedObject.FindProperty("_hardLandingThreshold");
            _softLandingDuration = serializedObject.FindProperty("_softLandingDuration");
            _hardLandingDuration = serializedObject.FindProperty("_hardLandingDuration");

            // Landing Roll
            _rollEnabled = serializedObject.FindProperty("_rollEnabled");
            _rollTriggerMode = serializedObject.FindProperty("_rollTriggerMode");
            _rollSpeedModifier = serializedObject.FindProperty("_rollSpeedModifier");

            // Crouching
            _crouchHeight = serializedObject.FindProperty("_crouchHeight");
            _standingHeight = serializedObject.FindProperty("_standingHeight");
            _crouchSpeed = serializedObject.FindProperty("_crouchSpeed");
            _crouchAcceleration = serializedObject.FindProperty("_crouchAcceleration");
            _crouchDeceleration = serializedObject.FindProperty("_crouchDeceleration");
            _crouchTransitionDuration = serializedObject.FindProperty("_crouchTransitionDuration");
            _crouchHeadClearanceMargin = serializedObject.FindProperty("_crouchHeadClearanceMargin");
            _canJumpFromCrouch = serializedObject.FindProperty("_canJumpFromCrouch");
            _canSprintFromCrouch = serializedObject.FindProperty("_canSprintFromCrouch");
            _crouchStepHeight = serializedObject.FindProperty("_crouchStepHeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();

            EditorGUILayout.Space(5);

            // Ground Movement Section
            _groundMovementFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_groundMovementFoldout, "Ground Movement");
            if (_groundMovementFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_walkSpeed, "Walking Speed", "Movement speed when walking (m/s)");
                DrawPropertyWithTooltip(_runSpeed, "Running Speed", "Movement speed when sprinting (m/s)");
                DrawPropertyWithTooltip(_acceleration, "Acceleration", "How fast the character reaches target speed");
                DrawPropertyWithTooltip(_deceleration, "Deceleration", "How fast the character stops");

                // Preview
                DrawSpeedPreview();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Air Movement Section
            _airMovementFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_airMovementFoldout, "Air Movement");
            if (_airMovementFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_airControl, "Air Control", "Movement control while airborne (0-1)");
                DrawPropertyWithTooltip(_gravity, "Gravity", "Downward acceleration (m/s²)");
                DrawPropertyWithTooltip(_maxFallSpeed, "Max Fall Speed", "Terminal velocity (m/s)");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Jumping Section
            _jumpingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_jumpingFoldout, "Jumping");
            if (_jumpingFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_jumpHeight, "Jump Height", "Maximum jump height (m)");
                DrawPropertyWithTooltip(_jumpDuration, "Jump Duration", "Time to reach peak (s)");
                DrawPropertyWithTooltip(_coyoteTime, "Coyote Time", "Grace period after leaving ground (s)");
                DrawPropertyWithTooltip(_jumpBufferTime, "Jump Buffer", "Pre-land jump input buffer (s)");

                // Jump Physics Preview
                DrawJumpPreview();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Ground Detection Section
            _groundDetectionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_groundDetectionFoldout, "Ground & Fall Detection");
            if (_groundDetectionFoldout)
            {
                EditorGUI.indentLevel++;

                // Strategy Selection
                EditorGUILayout.LabelField("Strategy", EditorStyles.boldLabel);
                DrawPropertyWithTooltip(_groundDetection, "Ground Detection", "Motor = KCC-Standard (IsStableOnGround), Collider = SphereCast (Genshin-Style)");
                DrawPropertyWithTooltip(_fallDetection, "Fall Detection", "Motor = SnappingPrevented + IsStable, Collider = Raycast von Capsule-Unterseite (Genshin-Style)");

                EditorGUILayout.Space(5);

                // Collider Parameters
                EditorGUILayout.LabelField("Collider Parameters", EditorStyles.boldLabel);
                DrawPropertyWithTooltip(_groundCheckDistance, "Check Distance", "SphereCast distance for ground detection (m). Nur bei Ground Detection = Collider.");
                DrawPropertyWithTooltip(_groundCheckRadius, "Check Radius", "SphereCast radius for ground detection (m). Nur bei Ground Detection = Collider.");
                DrawPropertyWithTooltip(_groundToFallRayDistance, "Fall Ray Distance", "Raycast distance from capsule bottom for fall detection (m). Nur bei Fall Detection = Collider.");

                EditorGUILayout.Space(5);

                // General
                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_groundLayers, new GUIContent("Ground Layers"));
                DrawPropertyWithTooltip(_maxSlopeAngle, "Max Slope Angle", "Maximum walkable slope (degrees)");

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Rotation Section
            _rotationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_rotationFoldout, "Rotation");
            if (_rotationFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_rotationSpeed, "Rotation Speed", "How fast character rotates (deg/s)");
                EditorGUILayout.PropertyField(_rotateTowardsMovement, new GUIContent("Auto-Rotate"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Step Detection Section
            _stepDetectionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_stepDetectionFoldout, "Step Detection");
            if (_stepDetectionFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_maxStepHeight, "Max Step Height", "Maximum climbable step (m)");
                DrawPropertyWithTooltip(_minStepDepth, "Min Step Depth", "Minimum step surface depth (m)");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Stair Speed", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_stairSpeedReductionEnabled, new GUIContent("Enable Stair Slowdown", "Reduce speed when climbing stairs (multiple steps detected)"));
                using (new EditorGUI.DisabledScope(!(_stairSpeedReductionEnabled?.boolValue ?? true)))
                {
                    DrawPropertyWithTooltip(_stairSpeedReduction, "Speed Reduction", "Speed reduction on stairs (0=none, 1=full stop)");
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Slope Speed Section
            _slopeSpeedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_slopeSpeedFoldout, "Slope Speed");
            if (_slopeSpeedFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_uphillSpeedPenalty, "Uphill Penalty", "Max speed reduction going uphill at steepest walkable angle (0=none, 1=full stop)");
                DrawPropertyWithTooltip(_downhillSpeedBonus, "Downhill Bonus", "Speed bonus going downhill at steepest walkable angle (positive=faster, 0=none)");

                // Preview
                DrawSlopeSpeedPreview();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Slope Sliding Section
            _slopeSlidingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_slopeSlidingFoldout, "Slope Sliding");
            if (_slopeSlidingFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_slopeSlideSpeed, "Slide Speed", "Base speed when sliding on steep slopes (m/s)");
                EditorGUILayout.PropertyField(_useSlopeDependentSlideSpeed, new GUIContent("Dynamic Speed", "Scale speed with slope steepness"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Landing Section
            _landingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_landingFoldout, "Landing");
            if (_landingFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_softLandingThreshold, "Soft Landing Threshold", "Landing speed below this = no recovery pause (m/s)");
                DrawPropertyWithTooltip(_hardLandingThreshold, "Hard Landing Threshold", "Landing speed above this = hard landing or roll (m/s)");
                DrawPropertyWithTooltip(_softLandingDuration, "Soft Landing Duration", "Recovery time for soft landing (s)");
                DrawPropertyWithTooltip(_hardLandingDuration, "Hard Landing Duration", "Recovery time for hard landing (s)");

                // Preview
                DrawLandingPreview();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Landing Roll Section
            _landingRollFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_landingRollFoldout, "Landing Roll");
            if (_landingRollFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_rollEnabled, new GUIContent("Enabled", "Enable/disable landing roll (false = always HardLanding)"));
                using (new EditorGUI.DisabledScope(!(_rollEnabled?.boolValue ?? true)))
                {
                    DrawPropertyWithTooltip(_rollTriggerMode, "Trigger Mode", "MovementInput = auto roll with stick input, ButtonPress = requires dodge button");
                    DrawPropertyWithTooltip(_rollSpeedModifier, "Speed Modifier", "Roll speed relative to RunSpeed (0.5-2.0)");
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Crouching Section
            _crouchingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_crouchingFoldout, "Crouching");
            if (_crouchingFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Capsule", EditorStyles.boldLabel);
                DrawPropertyWithTooltip(_crouchHeight, "Crouch Height", "Capsule height when crouching (m)");
                DrawPropertyWithTooltip(_standingHeight, "Standing Height", "Capsule height when standing (m) — Motor default");
                DrawPropertyWithTooltip(_crouchTransitionDuration, "Transition Duration", "Duration of capsule height transition (s)");
                DrawPropertyWithTooltip(_crouchHeadClearanceMargin, "Head Clearance", "Safety margin for stand-up ceiling check (m)");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
                DrawPropertyWithTooltip(_crouchSpeed, "Crouch Speed", "Movement speed when crouching (m/s)");
                DrawPropertyWithTooltip(_crouchAcceleration, "Acceleration", "Acceleration when crouching (m/s²)");
                DrawPropertyWithTooltip(_crouchDeceleration, "Deceleration", "Deceleration when crouching (m/s²)");
                DrawPropertyWithTooltip(_crouchStepHeight, "Step Height", "Reduced step height when crouching (m), -1 = Motor default");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_canJumpFromCrouch, new GUIContent("Can Jump", "Allow jumping from crouch (stands up + jumps)"));
                EditorGUILayout.PropertyField(_canSprintFromCrouch, new GUIContent("Can Sprint", "Sprint input exits crouch automatically"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private new void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Locomotion Configuration", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Reset Configuration",
                    "Reset all values to defaults?", "Reset", "Cancel"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPropertyWithTooltip(SerializedProperty property, string label, string tooltip)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            }
        }

        private void DrawSpeedPreview()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Speed Preview", EditorStyles.miniLabel);

            float walkSpeed = _walkSpeed?.floatValue ?? 5f;
            float runSpeed = _runSpeed?.floatValue ?? 10f;

            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            float maxSpeed = Mathf.Max(runSpeed, 15f);

            // Walk bar
            float walkWidth = (walkSpeed / maxSpeed) * rect.width;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, walkWidth, rect.height / 2 - 1), new Color(0.2f, 0.6f, 0.2f));
            EditorGUI.LabelField(new Rect(rect.x + 2, rect.y, 100, rect.height / 2), $"Walk: {walkSpeed:F1} m/s", EditorStyles.miniLabel);

            // Run bar
            float runWidth = (runSpeed / maxSpeed) * rect.width;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height / 2 + 1, runWidth, rect.height / 2 - 1), new Color(0.6f, 0.4f, 0.2f));
            EditorGUI.LabelField(new Rect(rect.x + 2, rect.y + rect.height / 2 + 1, 100, rect.height / 2), $"Run: {runSpeed:F1} m/s", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawJumpPreview()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Jump Physics", EditorStyles.miniLabel);

            float height = _jumpHeight?.floatValue ?? 2f;
            float gravity = _gravity?.floatValue ?? 20f;

            // Berechne Jump Velocity (v = sqrt(2 * g * h))
            float jumpVelocity = Mathf.Sqrt(2f * gravity * height);
            float airTime = 2f * jumpVelocity / gravity;

            EditorGUILayout.LabelField($"Initial Velocity: {jumpVelocity:F2} m/s");
            EditorGUILayout.LabelField($"Total Air Time: {airTime:F2} s");
            EditorGUILayout.LabelField($"Peak Time: {airTime / 2:F2} s");

            EditorGUILayout.EndVertical();
        }

        private void DrawSlopeSpeedPreview()
        {
            float uphill = _uphillSpeedPenalty?.floatValue ?? 0.3f;
            float downhill = _downhillSpeedBonus?.floatValue ?? 0.1f;
            float maxAngle = _maxSlopeAngle?.floatValue ?? 45f;
            float runSpeed = _runSpeed?.floatValue ?? 6f;

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Slope Speed Preview", EditorStyles.miniLabel);

            float[] angles = { 15f, 30f, 45f };
            foreach (float angle in angles)
            {
                if (angle > maxAngle) break;
                float factor = maxAngle > 0f ? angle / maxAngle : 0f;
                float uphillMul = Mathf.Clamp(1f - uphill * factor, 0.1f, 2f);
                float downhillMul = Mathf.Clamp(1f + downhill * factor, 0.1f, 2f);
                EditorGUILayout.LabelField(
                    $"{angle:F0}°:  ↑ {runSpeed * uphillMul:F1} m/s ({uphillMul:P0})  |  ↓ {runSpeed * downhillMul:F1} m/s ({downhillMul:P0})");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLandingPreview()
        {
            float gravity = _gravity?.floatValue ?? 20f;
            float softThreshold = _softLandingThreshold?.floatValue ?? 5f;
            float hardThreshold = _hardLandingThreshold?.floatValue ?? 15f;

            // Benötigte Fallhöhe: h = v² / (2g)
            float softHeight = softThreshold * softThreshold / (2f * gravity);
            float hardHeight = hardThreshold * hardThreshold / (2f * gravity);

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Landing Preview", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Soft Landing ab: {softHeight:F1}m Fallhöhe ({softThreshold:F0} m/s)");
            EditorGUILayout.LabelField($"Hard Landing ab: {hardHeight:F1}m Fallhöhe ({hardThreshold:F0} m/s)");
            EditorGUILayout.EndVertical();
        }

        private void ResetToDefaults()
        {
            // Ground Movement
            _walkSpeed.floatValue = 3f;
            _runSpeed.floatValue = 6f;
            _acceleration.floatValue = 10f;
            _deceleration.floatValue = 15f;

            // Air Movement
            _airControl.floatValue = 0.3f;
            _gravity.floatValue = 20f;
            _maxFallSpeed.floatValue = 50f;

            // Jumping
            _jumpHeight.floatValue = 2f;
            _jumpDuration.floatValue = 0.4f;
            _coyoteTime.floatValue = 0.15f;
            _jumpBufferTime.floatValue = 0.1f;

            // Ground Detection
            _groundCheckDistance.floatValue = 0.2f;
            _groundCheckRadius.floatValue = 0.3f;
            _maxSlopeAngle.floatValue = 45f;

            // Rotation
            _rotationSpeed.floatValue = 720f;
            _rotateTowardsMovement.boolValue = true;

            // Step Detection
            _maxStepHeight.floatValue = 0.3f;
            _minStepDepth.floatValue = 0.1f;
            _stairSpeedReductionEnabled.boolValue = true;
            _stairSpeedReduction.floatValue = 0.3f;

            // Slope Speed
            _uphillSpeedPenalty.floatValue = 0.3f;
            _downhillSpeedBonus.floatValue = 0.1f;

            // Slope Sliding
            _slopeSlideSpeed.floatValue = 8f;
            _useSlopeDependentSlideSpeed.boolValue = true;

            // Landing
            _softLandingThreshold.floatValue = 5f;
            _hardLandingThreshold.floatValue = 15f;
            _softLandingDuration.floatValue = 0.1f;
            _hardLandingDuration.floatValue = 0.4f;

            // Landing Roll
            _rollEnabled.boolValue = true;
            _rollTriggerMode.enumValueIndex = 0; // MovementInput
            _rollSpeedModifier.floatValue = 1.0f;

            // Crouching
            _crouchHeight.floatValue = 1.2f;
            _standingHeight.floatValue = 2.0f;
            _crouchSpeed.floatValue = 2.5f;
            _crouchAcceleration.floatValue = 8.0f;
            _crouchDeceleration.floatValue = 10.0f;
            _crouchTransitionDuration.floatValue = 0.25f;
            _crouchHeadClearanceMargin.floatValue = 0.1f;
            _canJumpFromCrouch.boolValue = true;
            _canSprintFromCrouch.boolValue = true;
            _crouchStepHeight.floatValue = 0.2f;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
