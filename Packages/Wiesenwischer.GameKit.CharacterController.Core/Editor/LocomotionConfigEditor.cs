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
        private bool _slopeSlidingFoldout = false;

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

        private SerializedProperty _rotationSpeed;
        private SerializedProperty _rotateTowardsMovement;

        private SerializedProperty _maxStepHeight;
        private SerializedProperty _minStepDepth;

        private SerializedProperty _slopeSlideSpeed;
        private SerializedProperty _useSlopeDependentSlideSpeed;

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

            // Rotation
            _rotationSpeed = serializedObject.FindProperty("_rotationSpeed");
            _rotateTowardsMovement = serializedObject.FindProperty("_rotateTowardsMovement");

            // Step Detection
            _maxStepHeight = serializedObject.FindProperty("_maxStepHeight");
            _minStepDepth = serializedObject.FindProperty("_minStepDepth");

            // Slope Sliding
            _slopeSlideSpeed = serializedObject.FindProperty("_slopeSlideSpeed");
            _useSlopeDependentSlideSpeed = serializedObject.FindProperty("_useSlopeDependentSlideSpeed");
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
            _groundDetectionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_groundDetectionFoldout, "Ground Detection");
            if (_groundDetectionFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithTooltip(_groundCheckDistance, "Check Distance", "Distance to check for ground (m)");
                DrawPropertyWithTooltip(_groundCheckRadius, "Check Radius", "Radius of ground check sphere (m)");
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

            // Slope Sliding
            _slopeSlideSpeed.floatValue = 8f;
            _useSlopeDependentSlideSpeed.boolValue = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
