using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Custom Inspector für PlayerController.
    /// Zeigt Debug-Informationen und Runtime-Status an.
    /// </summary>
    [CustomEditor(typeof(PlayerController))]
    public class PlayerControllerEditor : UnityEditor.Editor
    {
        private PlayerController _controller;

        // Foldout States
        private bool _debugFoldout = true;
        private bool _configFoldout = true;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _stateStyle;
        private GUIStyle _valueStyle;

        private void OnEnable()
        {
            _controller = target as PlayerController;
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();

            // Standard Inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Runtime Debug Info
            if (Application.isPlaying && _controller != null)
            {
                DrawRuntimeDebugInfo();
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime-Debug-Informationen sind nur im Play-Mode verfügbar.", MessageType.Info);
            }
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
            }

            if (_stateStyle == null)
            {
                _stateStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.2f, 0.7f, 0.2f) }
                };
            }

            if (_valueStyle == null)
            {
                _valueStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight
                };
            }
        }

        private void DrawRuntimeDebugInfo()
        {
            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "Runtime Debug", true);
            if (!_debugFoldout) return;

            EditorGUI.indentLevel++;

            // State Info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current State:", GUILayout.Width(100));
            EditorGUILayout.LabelField(_controller.CurrentStateName, _stateStyle);
            EditorGUILayout.EndHorizontal();

            // Grounded Status
            var groundedColor = _controller.IsGrounded ? Color.green : Color.red;
            GUI.color = groundedColor;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Grounded:", GUILayout.Width(100));
            EditorGUILayout.LabelField(_controller.IsGrounded.ToString());
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            // Sliding Status
            var slidingColor = _controller.IsSliding ? Color.yellow : Color.white;
            GUI.color = slidingColor;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sliding:", GUILayout.Width(100));
            EditorGUILayout.LabelField(_controller.IsSliding.ToString());
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            // Velocities
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Velocities", _headerStyle);

            var reusableData = _controller.ReusableData;
            if (reusableData != null)
            {
                DrawVelocityBar("Horizontal", reusableData.HorizontalVelocity.magnitude, _controller.LocomotionConfig?.RunSpeed ?? 10f);
                DrawVelocityBar("Vertical", reusableData.VerticalVelocity, _controller.LocomotionConfig?.MaxFallSpeed ?? 20f, true);
                DrawVector3Field("Total Velocity", reusableData.Velocity);
            }

            // Tick System
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Tick System", _headerStyle);
            EditorGUILayout.LabelField("Current Tick:", _controller.CurrentTick.ToString());

            if (_controller.TickSystem != null)
            {
                EditorGUILayout.LabelField("Tick Rate:", $"{_controller.TickSystem.TickRate} Hz");
                EditorGUILayout.LabelField("Tick Delta:", $"{_controller.TickSystem.TickDelta * 1000:F2} ms");
            }

            // Ground Info (direkt vom Motor)
            var gi = _controller.GroundInfo;
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Ground Info", _headerStyle);
            EditorGUILayout.LabelField("Slope Angle:", $"{gi.SlopeAngle:F1}°");

            var walkableColor = gi.IsWalkable ? Color.green : Color.yellow;
            GUI.color = walkableColor;
            EditorGUILayout.LabelField("Walkable:", gi.IsWalkable.ToString());
            GUI.color = Color.white;

            EditorGUILayout.LabelField("Distance:", $"{gi.Distance:F3}");

            EditorGUI.indentLevel--;

            // Repaint während Runtime
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawVelocityBar(string label, float value, float maxValue, bool allowNegative = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label + ":", GUILayout.Width(100));

            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            float normalizedValue = allowNegative
                ? (value + maxValue) / (2f * maxValue)
                : Mathf.Clamp01(value / maxValue);

            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            Color barColor = allowNegative
                ? (value >= 0 ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.6f, 0.2f, 0.2f))
                : new Color(0.2f, 0.6f, 0.2f);

            var fillRect = new Rect(rect.x, rect.y, rect.width * normalizedValue, rect.height);
            EditorGUI.DrawRect(fillRect, barColor);

            EditorGUI.LabelField(rect, $"{value:F2}", _valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVector3Field(string label, Vector3 value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label + ":", GUILayout.Width(100));
            EditorGUILayout.LabelField($"({value.x:F2}, {value.y:F2}, {value.z:F2})");
            EditorGUILayout.EndHorizontal();
        }

        // Scene View Gizmos
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmos(PlayerController controller, GizmoType gizmoType)
        {
            if (controller == null) return;

            var reusableData = controller.ReusableData;
            if (reusableData == null) return;

            // Movement Direction
            if (Application.isPlaying && reusableData.HorizontalVelocity.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                var start = controller.transform.position + Vector3.up * 0.5f;
                var end = start + reusableData.HorizontalVelocity.normalized * 2f;
                Gizmos.DrawLine(start, end);
                DrawArrowHead(end, reusableData.HorizontalVelocity.normalized, 0.3f);
            }

            // Forward Direction
            Gizmos.color = Color.cyan;
            var forward = controller.transform.position + Vector3.up * 0.5f + controller.transform.forward * 1f;
            Gizmos.DrawLine(controller.transform.position + Vector3.up * 0.5f, forward);
        }

        private static void DrawArrowHead(Vector3 position, Vector3 direction, float size)
        {
            var right = Vector3.Cross(direction, Vector3.up).normalized * size;
            var back = -direction * size;

            Gizmos.DrawLine(position, position + back + right * 0.5f);
            Gizmos.DrawLine(position, position + back - right * 0.5f);
        }
    }
}
