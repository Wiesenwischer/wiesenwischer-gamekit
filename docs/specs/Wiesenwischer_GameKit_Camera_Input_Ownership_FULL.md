
# Wiesenwischer GameKit – Camera Input + Ownership Architecture (FULL SPEC)

## Ziel

Diese Spezifikation beschreibt die vollständige Input- und Ownership-Architektur
für das Camera System. Ziel ist eine modulare Unterstützung für unterschiedliche
Camera Styles (z.B. BDO und ArcheAge), ohne dass sich diese gegenseitig beeinflussen.

Dieses Dokument ist als Implementierungsgrundlage gedacht (z.B. für Claude).

---

# ⭐ High-Level Pipeline

Unity Input System
        ↓
RawInput
        ↓
ICameraInputBehaviour      (Mode spezifisch)
        ↓
ICameraOwnershipPolicy     (entscheidet Ownership)
        ↓
CameraInputPipeline
        ↓
CameraBrain
        ↓
Cinemachine Driver

---

# ⭐ Core Design Prinzipien

- CameraBrain kennt KEINE Modes (kein if ArcheAge/BDO).
- Unterschiedliche Camera Styles werden über Behaviour + Ownership implementiert.
- Input wird VOR der Kamera interpretiert.

---

# ⭐ Raw Input (Unity Input System)

RawInput enthält nur rohe Daten:

class RawInput
{
    public Vector2 mouseDelta;
    public float scroll;
    public bool lmbHeld;
    public bool rmbHeld;
}

---

# ⭐ Camera Input Behaviour

Interpretation der Input-Semantik.

Interface:

interface ICameraInputBehaviour
{
    CameraInputState Process(RawInput input);
}

CameraInputState:

class CameraInputState
{
    public Vector2 lookDelta;
    public float facingDelta;
}

---

# ⭐ BDO Input Behaviour

Mouse bewegt Kamera immer.

class BdoInputBehaviour : ICameraInputBehaviour
{
    public CameraInputState Process(RawInput input)
    {
        return new CameraInputState
        {
            lookDelta = input.mouseDelta,
            facingDelta = 0
        };
    }
}

---

# ⭐ ArcheAge Input Behaviour

Mouse bewegt Kamera nur wenn RMB gedrückt ist.

class ArcheAgeInputBehaviour : ICameraInputBehaviour
{
    public CameraInputState Process(RawInput input)
    {
        var state = new CameraInputState();

        if(input.rmbHeld)
        {
            state.lookDelta = input.mouseDelta;
        }
        else if(input.lmbHeld)
        {
            state.facingDelta = input.mouseDelta.x;
        }
        else
        {
            state.lookDelta = Vector2.zero;
        }

        return state;
    }
}

---

# ⭐ Ownership Policy

Ownership bestimmt:

- darf Kamera rotieren?
- darf Character rotieren?
- welches FrameSpace Movement nutzt?

Interface:

interface ICameraOwnershipPolicy
{
    CameraOwnershipResult Evaluate(RawInput input);
}

CameraOwnershipResult:

class CameraOwnershipResult
{
    public Vector2 lookDelta;
    public float facingDelta;
    public FrameSpace frameSpace;
}

---

# ⭐ ArcheAge Ownership Verhalten

| LMB | RMB | LookDelta | FacingDelta | FrameSpace |
|-----|-----|-----------|-------------|------------|
| ❌ | ❌ | 0 | 0 | CharacterFrame |
| ❌ | ✅ | mouse | optional | CameraFrame |
| ✅ | ❌ | 0 | mouse.x | CharacterFrame |
| ✅ | ✅ | mouse | optional | CameraFrame |

---

# ⭐ BDO Ownership Verhalten

result.lookDelta = input.mouseDelta;
result.frameSpace = CameraFrame;

---

# ⭐ Integration Regeln

❌ Keine Mode Logik im CameraBrain.

✔ InputBehaviour interpretiert Buttons.

✔ OwnershipPolicy entscheidet Kontrolle.

✔ CameraBrain erhält nur bereinigte Daten.

---

# ⭐ Vorteile

- BDO und ArcheAge beeinflussen sich nicht.
- Neue Camera Modes leicht hinzufügbar.
- Saubere Trennung zwischen Input, Ownership und Camera Logic.

