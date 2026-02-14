# Phase 24: FootIK Terrain-Adaptive Verbesserungen

> **Epic:** Natürliche Bewegung — Inverse Kinematics
> **Branch:** `integration/phase-24-footik-improvements`
> **Abhängigkeit:** Phase 8 (IK System) — ✅ Abgeschlossen
> **Specs:**
> - [FootIK Verbesserungen Spezifikation](../../specs/FootIK_Verbesserungen_Spezifikation.md)
> - [GameKit IK Spezifikation](../../specs/GameKit_IK_Spezifikation.md)

---

## Ziel

FootIK verbessern, damit auf flachem Boden keine gebeugten Knie entstehen. Drei gezielte, voneinander unabhängige Fixes adressieren die drei identifizierten Ursachen:

1. **Body-Offset nach oben** — Body kann den IK-adjustierten Füßen nach oben folgen
2. **Terrain-Varianz** — IK-Weight proportional zur tatsächlichen Terrain-Unebenheit
3. **Delta Dead Zone** — Minimale Positionsunterschiede triggern keinen IK-Eingriff

**Betroffene Datei:** `Packages/Wiesenwischer.GameKit.CharacterController.IK/Runtime/Modules/FootIK.cs`

**Keine Interface-Änderungen:** `IIKModule` und `IKManager` bleiben unverändert. Alle Änderungen sind intern in `FootIK`.

---

## Voraussetzungen

- [x] Phase 8: IK System (IKManager, FootIK, LookAtIK) — Abgeschlossen
- [x] Phase 20: Visual Grounding Smoother — Abgeschlossen

---

## Schritte

| Schritt | Beschreibung | Branch | Commit-Message |
|---------|-------------|--------|----------------|
| [24.1](24.1-body-offset-up.md) | Body-Offset darf leicht nach oben | `fix/footik-body-offset-up` | `fix(phase-24): 24.1 Body-Offset nach oben erlauben` |
| [24.2](24.2-terrain-variance.md) | Terrain-Varianz-basierter IK-Weight | `fix/footik-terrain-variance` | `fix(phase-24): 24.2 Terrain-Varianz IK-Weight` |
| [24.3](24.3-dead-zone.md) | Delta-basierte Dead Zone pro Fuß | `fix/footik-dead-zone` | `fix(phase-24): 24.3 Delta Dead Zone pro Fuß` |
| [24.4](24.4-unit-tests.md) | Unit Tests für alle drei Fixes | `test/footik-terrain-adaptive` | `test(phase-24): 24.4 FootIK Terrain-Adaptive Tests` |
| [24.5](24.5-play-mode-verifikation.md) | Play Mode Verifikation | — (kein eigener Branch) | — |

**Branch-Zuordnung:**
- `fix/footik-body-offset-up` → Schritt 24.1
- `fix/footik-terrain-variance` → Schritt 24.2
- `fix/footik-dead-zone` → Schritt 24.3
- `test/footik-terrain-adaptive` → Schritt 24.4

---

## Neue Parameter (Zusammenfassung)

| Parameter | Default | Header | Beschreibung |
|-----------|---------|--------|-------------|
| `_maxBodyUpOffset` | 0.05 | Adjustments | Max. Aufwärts-Versatz des Body in Metern |
| `_terrainVarianceThreshold` | 0.03 | Terrain Adaptation | Höhendifferenz (m) ab der IK voll eingreift |
| `_footDeadZone` | 0.02 | Terrain Adaptation | Minimaler Fuß-Versatz (m) für IK-Eingriff |

---

## Neue Felder

```csharp
// Terrain-Varianz (Schritt 24.2)
private float _terrainWeight = 1f;
private Vector3 _leftFootNormal;
private Vector3 _rightFootNormal;
```

---

## Erwartetes Ergebnis

Nach Phase 24:
- Flacher Boden, Idle → Gerade Beine (keine gebeugten Knie mehr)
- Treppen/Slopes → Füße passen sich weiterhin korrekt an
- Übergang flach→uneben → Sanfter IK-Übergang via SmoothDamp
- Walking → Kein visueller Unterschied (IK bereits über Locomotion Blend ausgeblendet)
- Crouching auf flachem Boden → Keine gebeugten Knie

---

## Abgrenzung

| In Phase 24 | NICHT in Phase 24 |
|------------|-------------------|
| Body-Offset Aufwärts-Korrektur | Neues IK-Backend |
| Terrain-Varianz-Erkennung | Änderungen an IKManager oder LookAtIK |
| Per-Fuß Dead Zone | Neue Config-ScriptableObjects |
| Unit Tests für neue Logik | IIKModule Interface-Änderungen |

---

## Nächste Phase im Epic

→ (Kein direkter Nachfolger geplant. Hand IK wird nach Phase 9: Combat Abilities ergänzt.)
