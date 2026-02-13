namespace Wiesenwischer.GameKit.CharacterController.Core.Animation
{
    /// <summary>
    /// Animation-States die von der State Machine gesteuert werden.
    /// Jeder State entspricht einem Animator-State im AnimatorController.
    /// </summary>
    public enum CharacterAnimationState
    {
        Locomotion,
        Jump,
        Fall,
        SoftLand,
        HardLand,
        LightStop,
        MediumStop,
        HardStop,
        Slide
    }

    /// <summary>
    /// Interface für Animation-Steuerung.
    /// Liegt im Core-Package, damit PlayerController und States darauf zugreifen können.
    /// Implementierung liegt im Animation-Package (AnimatorParameterBridge).
    ///
    /// Architektur: Die State Machine ist die einzige Autorität für Animation-States.
    /// Jeder State ruft in OnEnter PlayState() auf, wodurch der Animator via CrossFade
    /// direkt zum passenden State wechselt. Kein parameterbasiertes Transition-System nötig.
    /// </summary>
    public interface IAnimationController
    {
        /// <summary>
        /// Wechselt den Animator zum angegebenen State via CrossFade.
        /// Transition-Dauer wird aus der AnimationTransitionConfig gelesen.
        /// Wird von jedem State in OnEnter aufgerufen.
        /// Ignoriert redundante Aufrufe (wenn bereits im Ziel-State).
        /// Setzt CanExitAnimation auf false.
        /// </summary>
        void PlayState(CharacterAnimationState state);

        /// <summary>
        /// Wechselt den Animator zum angegebenen State via CrossFade mit expliziter Dauer.
        /// Überschreibt die konfigurierte Transition-Dauer für Sonderfälle.
        /// </summary>
        void PlayState(CharacterAnimationState state, float transitionDuration);

        /// <summary>
        /// Normalisierte Zeit der aktuellen Animation (0.0 = Anfang, 1.0 = Ende).
        /// Werte > 1.0 bei Loop-Animationen.
        /// Gibt 0 zurück wenn keine gültige Animation aktiv ist.
        /// </summary>
        float GetAnimationNormalizedTime() => 0f;

        /// <summary>
        /// Prüft ob die aktuelle Animation mindestens einmal vollständig abgespielt wurde.
        /// Gibt false zurück während einer CrossFade-Transition.
        /// Nur sinnvoll für One-Shot Animationen (nicht für Loops wie Locomotion).
        /// </summary>
        bool IsAnimationComplete() => false;

        /// <summary>
        /// Wird true wenn ein "AllowExit" Animation Event auf dem aktuellen Clip feuert.
        /// Wird automatisch auf false zurückgesetzt wenn PlayState() aufgerufen wird.
        /// Ermöglicht Designern, den frühestmöglichen Exit-Punkt einer Animation zu markieren.
        /// </summary>
        bool CanExitAnimation => false;

        /// <summary>
        /// Setzt die Bewegungsgeschwindigkeit (0 = Idle, 1 = Run, 1.5 = Sprint).
        /// Wird für die Locomotion Blend Tree benötigt.
        /// </summary>
        void SetSpeed(float speed);

        /// <summary>
        /// Setzt die vertikale Velocity für Jump/Fall Blending.
        /// </summary>
        void SetVerticalVelocity(float velocity);

        /// <summary>
        /// Setzt das Gewicht des Ability-Layers (0-1).
        /// </summary>
        void SetAbilityLayerWeight(float weight);
    }
}
