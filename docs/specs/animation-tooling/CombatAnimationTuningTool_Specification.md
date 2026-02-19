# Combat Animation Tuning Tool — Konsolidierte Spezifikation

> **Konsolidiert aus:**
> 1. Unity Combat Animation Tuning Tool Spec (Grundlagen)
> 2. Unity Combat Animation Tuning Tool FULL SPEC (Erweiterte Architektur)
> 3. Unity Combat Visual Tuning Tool SPEC (Visual Sandbox & Timeline Editor)
> 4. Unity Combat AAA Timeline Architecture SPEC (AAA Timeline UI)

---

## 1. Zielsetzung

Ein Runtime-Tool zur visuellen Bearbeitung von Combat Animation Timing innerhalb eines gebauten Unity Spiels (kein Unity Editor notwendig).

**Ziele:**

- Stabilität der Combat Architektur hat höchste Priorität
- Gameplay Timing ist vollständig data-driven
- Keine Unity Animation Events für Gameplay Timing
- Runtime Tool ermöglicht visuelles Tuning durch Tester und Designer
- Animation bleibt reiner Visual Layer
- Tooling darf Gameplay-Code nicht destabilisieren
- Visuelles Feedback statt Zahlen-basiertes Editing
- Combat Preview gegen einen Gegner
- Frame-basierte Bearbeitung
- Attacken schnell iterieren ohne Gameplay-Code zu verändern

---

## 2. Architekturentscheidungen

### 2.1 KEINE Animation Events für Gameplay Timing

**Entscheidung:** Animation Events werden NICHT für Gameplay Timing verwendet.

**Gründe:**

- Animation Events sind Editor-only Daten
- Runtime Editing schwierig
- Merge Konflikte in Clips / Versionierungsprobleme
- Gameplay-Logik wird im Animator versteckt
- Tooling wird unnötig komplex

**Stattdessen:** Gameplay Timing wird im Code berechnet (data-driven).

---

### 2.2 Animation = Visual Layer

Animator steuert:

- Animation Playback
- Blending / Blendtrees
- State Transitions
- Visualisierung

Animator steuert **NICHT:**

- Hit Timing
- Damage Logic
- Gameplay State
- Gameplay Events

---

### 2.3 Gameplay Anchor = Attack Start im Code

Anchor Zeitpunkt:

```
attackStartTime = Time.time
```

Dieser Zeitpunkt ist:

- deterministisch
- netzwerkfreundlich
- tool-unabhängig
- unabhängig vom Animator

---

### 2.4 Timing in Frames statt Sekunden

**Warum Frames:**

- Combat Design denkt/arbeitet in Frames
- Stabil bei Animation Speed Änderungen
- Intuitiv visualisierbar
- Deterministischer als Sekunden

---

## 3. Projektstruktur

Empfohlene Struktur:

```
Assets/
│
├── Combat/
│   ├── Runtime/
│   │   ├── CombatController.cs
│   │   ├── AttackRuntime.cs
│   │
│   ├── Data/
│   │   ├── AttackDefinition.cs
│   │   ├── AttackDatabase.cs
│   │   ├── AttackMapping.cs
│   │
│   └── Tool/
│       ├── RuntimeEditor/
│       │   ├── AttackEditorUI.cs
│       │   ├── TimelineView.cs
│       │   ├── AnimationPreviewController.cs
```

---

## 4. Datenmodell

### 4.1 AttackDefinition

```csharp
[Serializable]
public class AttackDefinition
{
    public string attackId;

    public int totalFrames;

    public int hitStartFrame;
    public int hitEndFrame;

    public float damage;
}
```

**Warum `totalFrames` speichern:**

- Tool unabhängig vom AnimationClip
- Stabil auch wenn Clip geändert wird

---

### 4.2 Attack Mapping Layer

**Problem:** Animation direkt an AttackDefinition koppeln erzeugt später Chaos.

**Lösung:** Separate Mapping Layer.

```csharp
[Serializable]
public class AttackMapping
{
    public string animationStateName;
    public string attackId;
}
```

**Warum Mapping Layer:**

- Animation kann geändert werden ohne Gameplay Daten zu zerstören
- Mehrere Animationen können gleiche AttackDefinition nutzen
- Cleaner Separation of Concerns

---

## 5. Runtime Combat System

### 5.1 Attack starten

```csharp
attackStartTime = Time.time;
animator.Play(mapping.animationStateName);
```

---

### 5.2 Frame Berechnung

```csharp
float elapsed = Time.time - attackStartTime;

float animLength = currentClip.length;

float normalizedTime = elapsed / animLength;

int currentFrame =
    Mathf.FloorToInt(normalizedTime * attack.totalFrames);
```

Alternative Berechnung über Animator:

```csharp
AnimatorStateInfo state =
    animator.GetCurrentAnimatorStateInfo(0);

float normalizedTime = state.normalizedTime % 1f;

int totalFrames =
    Mathf.RoundToInt(clip.length * clip.frameRate);

int currentFrame =
    Mathf.FloorToInt(normalizedTime * totalFrames);
```

---

### 5.3 Hit Detection

```csharp
if(currentFrame >= attack.hitStartFrame &&
   currentFrame <= attack.hitEndFrame)
{
    EnableHitbox();
}
```

---

## 6. Combat Preview Scene (Visual Sandbox)

Das Tool ist **keine reine Timeline-Anzeige**, sondern eine kleine Combat Preview Scene.

**Warum Sandbox statt reines Editor-UI:**

- Timing wird erst im Kontext sichtbar
- Reichweite und Reaktion sind entscheidend
- Designer brauchen visuelles Feedback

### 6.1 Scene-Struktur

```
CombatPreviewRoot
    PlayerPreview
    EnemyDummy
    PreviewCamera
```

### 6.2 PlayerPreview

- Nutzt echtes CombatController System
- Gleiche Animationen wie im Game
- Keine Sonderlogik

### 6.3 Enemy Dummy

**Warum notwendig:**

- Timing ohne Gegner wirkt oft korrekt aber fühlt sich falsch an
- Hit Feedback sichtbar

**Features:**

- Hit Reaction Animation
- Damage Numbers
- Debug Hitbox Anzeige

### 6.4 Kamera Setup

Feste Kamera verwenden.

**Warum:**

- Designer verliert sonst Timing-Gefühl
- Konsistente Bewertung der Attacke

---

## 7. Runtime Editor Tool

Tool läuft im Game als Debug UI.

### 7.1 Features

- Animation Preview
- Timeline Anzeige
- Frame Cursor / Counter
- Hit Window Visualisierung / Overlay
- Live Testing
- Frame Marker

### 7.2 Animation Scrubbing (Scrubbing Mode)

Animation direkt an Frame positionieren:

```csharp
animator.Play("Attack", 0, normalizedTime);
animator.Update(0);
```

### 7.3 Live Play Mode

Normales Combat Verhalten:

```csharp
combatController.StartAttack(attackDefinition);
```

### 7.4 Frame Step Controls

Sehr wichtig für präzises Tuning.

**Buttons:**

- Step Forward (+1 Frame)
- Step Back (-1 Frame)

**Implementation:**

```
currentFrame++;
normalized = currentFrame / totalFrames;
PreviewAnimation(normalized);
```

### 7.5 Live Preview

Nach jeder Änderung:

- Attack neu triggern
- Hit Window sofort sichtbar

---

## 8. AAA Timeline Architecture

### 8.1 Grundprinzipien

Die Timeline darf **KEINE** Gameplay-Logik enthalten.

Sie:

- visualisiert Daten
- manipuliert Daten
- sendet Änderungen an Data Layer

Gameplay bleibt im Combat Runtime System.

Timeline basiert vollständig auf Frames.

### 8.2 UI Hierarchie

```
TimelineRoot
    BackgroundBar
    FrameMarkers
    CurrentFrameCursor
    HitWindowContainer
        HitWindowRect
            LeftHandle
            RightHandle
```

### 8.3 Coordinate Mapping

Wichtigster Bestandteil der Timeline.

**Frame → Position:**

```
xPosition = (frame / totalFrames) * timelineWidth
```

**Position → Frame (Reverse):**

```
frame = (xPosition / timelineWidth) * totalFrames
```

### 8.4 Current Frame Cursor

Vertikale Linie, bewegt sich live während Animation läuft.

**Update Loop:**

- Aktuelle Animation Frame berechnen
- Cursor Position setzen

```
cursorX = frameToPosition(currentFrame)
cursorRect.anchoredPosition = new Vector2(cursorX, 0)
```

### 8.5 Hit Window Darstellung

Hit Window ist ein RectTransform.

**Start Position:**

```
startX = frameToPosition(hitStartFrame)
```

**Breite:**

```
width = frameToPosition(hitEndFrame - hitStartFrame)
```

**Alternativ (über Timeline):**

```
startPosition = hitStartFrame / totalFrames
width = (hitEndFrame - hitStartFrame) / totalFrames
```

### 8.6 Drag Behaviour

HitWindowRect ist draggable.

**Beim Drag:**

1. Maus Delta berechnen
2. Neue Position setzen
3. Frame berechnen
4. Data aktualisieren

```
newFrame = positionToFrame(newX)
attack.hitStartFrame = newFrame
```

### 8.7 Resize Behaviour

Handles links/rechts an der HitWindowRect.

**Left Handle:**

- Verändert `hitStartFrame`

**Right Handle:**

- Verändert `hitEndFrame`

### 8.8 Attack Phasen Visualisierung (Optional aber stark empfohlen)

Zusätzlich anzeigen:

```
[ Windup ][ Active ][ Recovery ]
```

Erhöht das Verständnis erheblich.

### 8.9 Visual Feedback (AAA UX)

Empfohlen:

- Hit Window Farbe (z.B. Rot)
- Hover Highlight
- Resize Cursor Icons
- Grid Lines alle 5 oder 10 Frames
- Farbige Balken
- Frame Numbers anzeigen
- Current Frame Cursor

### 8.10 Timeline Editor UX (Video Editor Style)

Ziel: Hit Window visuell verschieben.

**Features:**

- Drag Hit Window
- Resize durch Ziehen der Kanten
- Direktes visuelles Feedback

**Warum:**

- Zahlen werden weniger benötigt
- Schnellere Iteration

---

## 9. Hitbox Debug Visualisierung

- Collider transparent anzeigen
- Aktiv wenn Hit Window aktiv ist
- Option: `OnDrawGizmos` oder Runtime Mesh Renderer

---

## 10. Tool UI Layout

Empfohlenes Gesamtlayout:

```
------------------------------------------------
|             Animation Preview                |
|                                              |
|   Player Character        Enemy Dummy        |
|                                              |
------------------------------------------------

| Timeline ------------------------------------|
| 0      10     20     30     40               |
|        [===== HIT WINDOW =====]              |
|               ^                              |
|         Current Frame Cursor                 |
------------------------------------------------

[ Play ] [ Pause ] [ Step +1 ] [ Step -1 ]

Hit Start Frame: [ slider ]
Hit End Frame:   [ slider ]

[ Set Start ]
[ Set End ]

[ Play Attack ]
[ Save ]
```

### 10.1 Marker Editing (Alternative zu Drag)

UI-Elemente:

- Slider für Current Frame
- Button "Set Hit Start"
- Button "Set Hit End"

---

## 11. Performance Regeln

Sehr wichtig für stabile Timeline:

- **KEIN** Layout Rebuild pro Frame
- Nur Positionsupdate
- Keine `Instantiate`/`Destroy` während Drag

---

## 12. Speicherung

### 12.1 JSON Export (Runtime)

```csharp
string json =
    JsonUtility.ToJson(attackDefinition);

File.WriteAllText(path, json);
```

**Empfohlen:** StreamingAssets oder Remote Config System.

### 12.2 Optionale Erweiterungen

- Upload via API
- Remote Config
- ScriptableObject Export

---

## 13. Erweiterbarkeit

Architektur erlaubt später:

- Multiple Hit Windows (mehrere pro Attack)
- Cancel Frames
- Invulnerability Frames (i-Frames)
- Super Armor Windows / Frames
- Camera Events / Camera Trigger Markers
- Combo Windows / Combo Branch Points
- Combo Chains

---

## 14. Häufige Fehler vermeiden

- `normalizedTime` direkt als Gameplay Timing nutzen/speichern
- Animation Events für Gameplay Timing verwenden
- Sekunden statt Frames speichern
- Animator als Combat State Machine benutzen
- Timeline mit Gameplay-Logik koppeln

---

## 15. Vorteile dieses Systems

- Deterministisch
- Editor-unabhängig (läuft im Build)
- Leicht testbar
- Multiplayerfreundlich
- Visuell verständlich
- Tool-first Workflow
- Erweiterbar ohne Refactoring
- Minimale Abhängigkeit vom Animator
