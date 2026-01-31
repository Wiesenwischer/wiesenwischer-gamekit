# 🎭 Animationsarchitektur für modulare Charakterkontrolle & Fähigkeiten

Dieses Dokument beschreibt ein modulares und erweiterbares Animationskonzept für ein Spiel mit verschiedenen Bewegungsmodi (Gehen, Reiten, Gleiten) und Fähigkeiten (z. B. Feuerball, Nahkampf), inklusive Layer-System, Avatar Masks und UnityPackage-Struktur.

---

## 🎯 Zielsetzung

- Fähigkeit zur Kombination von Bewegung + Fähigkeit
- Modularität: Movement und Combat trennbar
- Wiederverwendbarkeit: Zauber mehrfach nutzbar
- Multiplayer-Sync (z. B. mit FishNet)

---

## 🧱 Layer-basierte Animator-Struktur

### 🔲 Layer 0: **Base Movement**
> Bewegt den Unterkörper und steuert das Locomotion-Verhalten

- Idle, Walk, Run
- Jump, Fall, Land
- Ride_Walk, Ride_Idle
- Glide

**Avatar Mask:** Unterkörper

---

### 🔳 Layer 1: **Abilities (Fähigkeiten)**
> Steuert obere Körperhälfte: Zaubern, Angreifen, Blocken

- Cast_Fireball
- Cast_Heal
- Attack_Melee
- Draw_Bow

**Avatar Mask:** Oberkörper

---

### 🔲 Layer 2 (optional): **Facial / LookAt**

---

## 🧩 Avatar Masks

| Name            | Enthaltene Körperteile     |
|------------------|-----------------------------|
| `Mask_UpperBody` | Spine, Arms, Head           |
| `Mask_LowerBody` | Hips, Legs                  |
| `Mask_ArmsOnly`  | Left/Right Arm              |

> Erstellt über `Assets → Create → Avatar Mask`

---

## 🔁 Ablauf: Kombination von Bewegung & Fähigkeit

### 🔥 Beispiel: Feuerball während Reiten

1. **Active MovementController:** `RidingController`
2. Spielt `Ride_Walk` im Base Layer (Layer 0)
3. `FireballAbility` wird gestartet:
    - Layer 1 wird aktiviert
    - Animation `Cast_Fireball` wird abgespielt
4. Nach Ende des Zaubers → Layer 1 auf 0 setzen

```csharp
animator.SetLayerWeight(1, 1.0f);
animator.SetTrigger("CastFireball");
```

---

## 🧪 Ability-Integration

### Interface

```csharp
public interface IAnimatableAbility
{
    string AnimationName { get; }
    AvatarMask AnimationMask { get; }
}
```

→ Ermöglicht dynamisches Zuordnen von Animation + Ziel-Layer

---

## 📦 UnityPackage-Struktur

| Paket | Inhalt |
|-------|--------|
| `Module.Character.Animations.Core` | Animator Controller + Base Movement |
| `Module.Character.Animations.Masks` | Avatar Masks |
| `Module.Character.Animations.Abilities.Fireball` | Clip + Animator Override für Layer 1 |
| `Module.Character.Abilities.Fireball` | Fireball-Ability (Code + Config) |
| `Module.Character.Animations.Riding` | Riding Movement (Base Layer Override) |
| `Module.Character.Animations.Gliding` | Gliding Movement |
| `Module.Character.Animations.MeleeCombat` | Melee-Angriff Layer |

---

## 🧠 Vorteile

| Punkt | Vorteil |
|-------|---------|
| Wiederverwendung | Zauber funktioniert beim Gehen, Reiten, Gleiten |
| Modularisierung | Trennung von Locomotion und Combat |
| Netzwerkfähig | Nur Parameter synchronisieren |
| Erweiterbar | Neue Animationen als UnityPackage hinzufügen |
| Editorfreundlich | Animationen testbar ohne Wechsel der States |

---

## 🧩 Erweiterungen

- Synced `AnimatorParameterBridge` für Multiplayer
- `AnimationControllerResolver` für dynamische Paketbindung
- Layer 1 als Skill-Vorschau nutzbar (Cast-Vorschau im Build-Modus)

---

## 📝 Fazit

Durch Layer, Masken und modulare Animator-Controller können Fähigkeiten wie Zauber nahtlos mit verschiedenen Bewegungssystemen (auch Reiten, Schwimmen, Bauen) kombiniert werden. Die Trennung von Bewegungs- und Fähigkeitsanimationen fördert Wiederverwendung und Multiplayer-Kompatibilität.