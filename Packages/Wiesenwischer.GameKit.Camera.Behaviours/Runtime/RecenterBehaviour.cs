using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Auto-Recenter: Kamera dreht sich hinter den Character
    /// wenn keine Look-Eingabe vorliegt und der Character sich bewegt.
    /// </summary>
    public class RecenterBehaviour : MonoBehaviour, ICameraBehaviour
    {
        [Header("Recenter")]
        [Tooltip("Verzögerung bevor Recenter startet (Sekunden nach letztem Look-Input).")]
        [SerializeField] private float _delay = 1.5f;

        [Tooltip("Recenter-Geschwindigkeit (Grad/Sekunde).")]
        [SerializeField] private float _speed = 60f;

        [Tooltip("Minimale Movement-Geschwindigkeit für Recenter-Aktivierung.")]
        [SerializeField] private float _minMoveSpeed = 0.5f;

        private float _timeSinceLastInput;
        private Vector3 _lastTargetPosition;

        public bool IsActive => enabled;

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            // Input-Erkennung: Reset Timer bei Look-Input
            bool hasLookInput = Mathf.Abs(ctx.Input.LookX) > 0.01f
                             || Mathf.Abs(ctx.Input.LookY) > 0.01f;

            if (hasLookInput)
            {
                _timeSinceLastInput = 0f;
                return;
            }

            _timeSinceLastInput += ctx.DeltaTime;

            // Delay abwarten
            if (_timeSinceLastInput < _delay) return;

            // Movement-Richtung ermitteln
            if (ctx.FollowTarget == null) return;

            Vector3 currentPos = ctx.FollowTarget.position;
            Vector3 moveDir = currentPos - _lastTargetPosition;
            _lastTargetPosition = currentPos;

            moveDir.y = 0f;
            if (moveDir.magnitude < _minMoveSpeed * ctx.DeltaTime) return;

            // Ziel-Yaw aus Bewegungsrichtung
            float targetYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

            // Sanftes Drehen zum Ziel-Yaw
            float yawDiff = Mathf.DeltaAngle(state.Yaw, targetYaw);
            float maxStep = _speed * ctx.DeltaTime;
            state.Yaw += Mathf.Clamp(yawDiff, -maxStep, maxStep);
        }
    }
}
