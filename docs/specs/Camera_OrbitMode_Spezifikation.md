# Camera Orbit Mode Spezifikation

## Problem

Das aktuelle Kamerasystem behandelt allen Look-Input gleich: Mausbewegung wird immer als Kamerarotation angewendet, der Cursor ist immer gelockt. Das entspricht dem Action-Combat-Paradigma, bildet aber **nicht** den fundamentalen Unterschied zwischen Action-Combat- und Classic-MMO-Kameras ab.

### Action Combat (Preset: ActionCombat)
- Cursor **immer** gelockt
- Maus steuert **immer** die Kamera
- Character dreht sich **unabhängig** von der Kamera (Movement-Richtung oder Combat-Facing)
- Kein Button nötig für Kamerarotation
- Beispiele: Black Desert Online, TERA, Guild Wars 2

### Classic MMO (Preset: ClassicMMO)
- Cursor **standardmäßig frei** (sichtbar, unlocked)
- **LMB gehalten + Drag** = Free Orbit (nur Kamera dreht, Character bleibt)
- **RMB gehalten + Drag** = Steer Orbit (Kamera + Character drehen zusammen)
- **Beide Buttons** = Character läuft vorwärts in Kamerarichtung
- Beispiele: World of Warcraft, ArcheAge, Final Fantasy XIV

## Lösung

### CameraOrbitMode (Enum)

Beschreibt den aktuellen Orbit-Zustand pro Frame:

| Wert | Bedeutung | Cursor |
|------|-----------|--------|
| `None` | Kein Orbit-Input aktiv | Frei |
| `FreeOrbit` | Kamera rotiert, Character nicht | Gelockt |
| `SteerOrbit` | Kamera rotiert, Character dreht mit | Gelockt |

### OrbitActivation (Enum)

Konfiguriert **wann** Orbit-Input gelesen wird:

| Wert | Verhalten | Typisch für |
|------|-----------|-------------|
| `AlwaysOn` | Maus steuert immer Kamera, Cursor immer gelockt | Action Combat (BDO, TERA, GW2) |
| `ButtonActivated` | Orbit nur bei gedrücktem LMB/RMB | Classic MMO (WoW, ArcheAge, FFXIV) |

### Datenfluss

```
InputPipeline (OrbitActivation + Button-Erkennung)
    ↓
CameraInputState.OrbitMode (None / FreeOrbit / SteerOrbit)
    ↓
OrbitBehaviour (ignoriert Input wenn OrbitMode == None)
    ↓
CameraContext.IsSteerMode (für Character Controller)
    ↓
CameraBrain.IsSteerMode (public API)
    ↓
Character Controller liest IsSteerMode → alignt sich zu CameraBrain.Forward
```

## Betroffene Komponenten

### CameraInputState (erweitern)
- Neues Feld: `CameraOrbitMode OrbitMode`

### CameraInputPipeline (erweitern)
- Neues Feld: `OrbitActivation _orbitActivation`
- Neue optionale Input Actions: `_freeLookButtonName` (LMB), `_steerButtonName` (RMB)
- Cursor-Management: Lock/Unlock basierend auf OrbitActivation + Button-State
- OrbitMode-Bestimmung pro Frame

### CameraContext (erweitern)
- Neues Feld: `bool IsSteerMode`

### OrbitBehaviour (anpassen)
- Input nur anwenden wenn `ctx.Input.OrbitMode != CameraOrbitMode.None`

### CameraBrain (erweitern)
- `public bool IsSteerMode` Getter
- In `SetPreset()`: OrbitActivation an InputPipeline weiterreichen

### CameraPreset (erweitern)
- Neue Felder: `OrbitActivation`, `FreeLookButton`, `SteerButton`

### RecenterBehaviour
- Keine Änderung nötig: checkt bereits `hasLookInput`, SteerOrbit hat Input → Timer reset

## Preset-Zuordnung

| Preset | OrbitActivation | FreeOrbit | SteerOrbit |
|--------|----------------|-----------|------------|
| ActionCombat | `AlwaysOn` | Immer aktiv | N/A |
| ClassicMMO | `ButtonActivated` | LMB | RMB |

## Abhängigkeit: Character Controller

Das Kamerasystem **dreht den Character nicht selbst** (Modularitätsprinzip). Stattdessen:
1. `CameraBrain.IsSteerMode` exponiert den Zustand
2. `CameraBrain.Forward` gibt die Kamerarichtung (Y=0, normalisiert)
3. Der Character Controller liest diese Werte und entscheidet selbst über Rotation

Dies wird in einer späteren Phase implementiert, wenn der Character Controller das Camera-System konsumiert.
