# 🧙 GameKit – D&D-inspiriertes Skill-System (Tab-Targeting & Action-Combat-Ready)

Diese überarbeitete Version basiert weiterhin auf den mechanischen Grundlagen von D&D, ist jedoch **angepasst auf moderne Action/MMORPG-Systeme** mit Tab-Targeting oder semi-action-basierten Kämpfen. Zudem können **Charakterwerte, Waffen und Buffs** die Fertigkeiten direkt beeinflussen.

---

## ⚔️ Ziel: Action-fähiges, regelbasiertes Skill-System

### Unterstützt:
- Tab-Targeting & Action-Combat
- Dynamische Trefferermittlung basierend auf Charakterwerten
- Schaden, Effekte & Status abhängig von Ausrüstung, Buffs, Klassenwerten
- Erweiterbare Eingabe- und Netzwerkarchitektur

---

## 🎯 Zielauswahl & Trefferlogik

**Zielsystem**
- Skill entscheidet über Targeting: Single Target, Cone, AOE, Self, Ground
- Erlaubt manuelles Zielen oder Tab-Zielwechsel

**Trefferermittlung**
```csharp
HitChance = BaseChance
           + Attacker.Attributes.Accuracy
           - Defender.Attributes.Evasion
           + Skill.HitModifier
```

**Rettungswurf (SaveRolls)** optional, z. B. bei:
- AOE-Spells (z. B. DEX Save halbiert Schaden)
- Debuffs (z. B. gegen Betäubung, Verlangsamung, Kontrolleffekte)

---

## 🔢 Einfluss von Attributen

Beispielhafte Modifikationen:

| Wert | Einfluss |
|------|----------|
| Strength | Nahkampfschaden, Durchschlagskraft |
| Dexterity | Trefferchance, Ausweichen, Geschwindigkeit |
| Intelligence | Zauberschaden, Mana-Effizienz |
| Wisdom | Resistenz, Buff-Stärke |
| Constitution | Lebenspunkte, Ausdauerregeneration |

---

## 🛠 SkillData-Erweiterung

```csharp
public class SkillDefinition : ScriptableObject {
    public DamageFormula Damage; // Base + Scaling + WeaponMultiplier
    public SkillTargeting Targeting;
    public SkillExecutionType Execution; // Instant, Cast, Channel
    public bool AllowMovementDuringCast;
    public StatusEffect[] Effects;
    public AttributeRequirement[] Requirements;
}
```

---

## 🧮 Beispielhafte Schadensformel

```csharp
public float CalculateDamage(ICharacterStats stats, IWeapon weapon) {
    float baseDamage = weapon.BaseDamage;
    float scaling = stats.GetModifier(StatType.Intelligence) * scalingFactor;
    float buffs = stats.GetBuffModifier(DamageType.Fire);
    return (baseDamage + scaling) * (1 + buffs);
}
```

---

## 🎛️ Modularisierung

### Eingabe
- Kein direkter Tastendruck im Skill – stattdessen über `SkillSlot` → `SkillExecutionRequest`

### Netzwerk
- Client sendet `RequestSkillCast` mit TargetInfo + Position
- Server prüft, simuliert Hit, löst Effekte aus
- Optionale Prediction möglich

### Ausführung
- SkillState: `Ready → Preparing → Executing → Recovery`
- Castbar, ChannelTime, HitWindow möglich

---

## 🧱 Architektur

| Modul | Zweck |
|-------|-------|
| GameKit.Skills.Core | Basisinterfaces, Skill Execution |
| GameKit.Skills.Attributes | Attribute-Modifikatoren, Requirements |
| GameKit.Skills.ActionCombat | Trefferzonen, Zielauswahl, Reichweite |
| GameKit.Skills.Network | RPC, CastRequest, CastSync |
| GameKit.Skills.UI | CastBar, SkillSlots, Cooldowns |

---

## ✅ Vorteile

- Kombinierbar mit Echtzeit-Steuerung und RPG-Werten
- Modular für MMO & SP
- Unterstützt flexible Erweiterungen (AOE, CC, Buffs, Charges)

---

Sag Bescheid, wenn du eine CSV-Skilltabelle, Zielsystem-Logik oder Unity-ready Paket möchtest.
