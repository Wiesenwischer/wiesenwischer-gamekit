using UnityEngine;

namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// ScriptableObject-basierte Konfiguration für eine Ability.
    /// Definiert statische Eigenschaften wie Cooldown, Priorität, Ressourcenkosten.
    /// Wird von konkreten IAbility-Implementierungen referenziert.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewAbility",
        menuName = "Wiesenwischer/Abilities/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Eindeutige ID der Ability (wird auch als IAbility.Id verwendet)")]
        public string abilityId;

        [Tooltip("Anzeigename für UI")]
        public string displayName;

        [Tooltip("Icon für AbilityBar UI")]
        public Sprite icon;

        [TextArea(2, 4)]
        [Tooltip("Beschreibung für Tooltips")]
        public string description;

        [Header("Timing")]
        [Tooltip("Cooldown in Sekunden nach Deaktivierung")]
        [Min(0f)]
        public float cooldown;

        [Tooltip("Maximale Dauer der Ability in Sekunden (0 = unbegrenzt)")]
        [Min(0f)]
        public float duration;

        [Header("Priority")]
        [Tooltip("Priorität für Interruption-Logik (höher = kann niedrigere unterbrechen)")]
        public int priority = AbilityPriority.Attack;

        [Header("Conditions")]
        [Tooltip("Ability nur aktivierbar wenn Character am Boden ist")]
        public bool requiresGrounded;

        [Tooltip("Ability blockiert Bewegung während aktiv")]
        public bool blocksMovement;

        [Tooltip("Ability kann durch andere Abilities unterbrochen werden")]
        public bool interruptible = true;

        [Header("Animation")]
        [Tooltip("Name des Animator States für diese Ability (Layer 1)")]
        public string animationStateName;

        [Tooltip("CrossFade-Dauer beim Wechsel zur Ability-Animation")]
        public float animationTransitionDuration = 0.1f;
    }
}
