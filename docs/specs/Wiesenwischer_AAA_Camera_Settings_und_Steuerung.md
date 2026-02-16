
# Wiesenwischer GameKit – AAA Camera Settings & Improved Camera Control
## Zusammenfassung der Einstellungen und erweiterten Camera-Steuerung

Dieses Dokument fasst die zuletzt besprochenen Verbesserungen für die Kamera-Steuerung zusammen.
Ziel ist eine modulare, AAA-taugliche Kamera-Architektur mit konfigurierbaren Settings
und hochwertigem Input-Handling.

---

# ⭐ Ziel der Architektur

- Konsistentes Camera Feeling (AAA Level)
- Runtime konfigurierbare Sensitivity
- Erweiterbar für Combat, Zoom, Mount, Glide etc.
- Saubere Trennung zwischen Input, Kamera-Logik und Rendering (Cinemachine)

Pipeline:

Raw Input
   ↓
Camera Input Pipeline
   ↓
Velocity-Based Camera Rotation
   ↓
Camera Brain
   ↓
Cinemachine Driver

---

# 1️⃣ Runtime konfigurierbare Einstellungen

## Warum wichtig?

- Unterschiedliche Maus-DPI
- Spielerpräferenzen
- Accessibility
- MMO Standard Feature

---

## Empfohlene Settings

class CameraInputSettings
{
    float mouseSensitivityX;
    float mouseSensitivityY;

    float scrollSensitivity;

    bool invertY;

    float acceleration; // smoothing strength

    float baseFov; // Referenz FOV für scaling
}

---

# 2️⃣ AAA Camera Input – Velocity Based Rotation

## Problem klassischer Implementationen

Viele Systeme machen:

rotation += input * sensitivity;

Nachteile:

- jitter
- inkonsistentes Feeling
- kein echtes Gewicht
- abruptes Stoppen

---

## AAA Lösung

Nicht Rotation direkt ändern.

👉 Angular Velocity berechnen.

Pipeline:

Raw Input
   ↓
Sensitivity
   ↓
Target Angular Velocity
   ↓
Smoothed Current Velocity
   ↓
Rotation Update

---

## Beispiel

### Target Velocity

Vector2 targetVelocity =
    rawInput * sensitivity;

### Smooth Velocity (exponential smoothing empfohlen)

float t = 1f - Mathf.Exp(-acceleration * deltaTime);
currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, t);

### Rotation anwenden

yaw   += currentVelocity.x * deltaTime;
pitch += currentVelocity.y * deltaTime;

---

# 3️⃣ FOV-basierte Sensitivity (AAA Standard)

## Problem

Beim Zoomen verändert sich das Kamera-Gefühl stark.

## Lösung

Sensitivity abhängig vom aktuellen FOV skalieren.

---

## Tan-based Scaling (empfohlen)

float fovScale =
    Mathf.Tan(currentFov * 0.5f * Mathf.Deg2Rad) /
    Mathf.Tan(baseFov * 0.5f * Mathf.Deg2Rad);

targetVelocity =
    rawInput * sensitivity * fovScale;

---

## Vorteile

- Muscle Memory bleibt erhalten
- Aim fühlt sich konsistent an
- Zoom wirkt natürlich

---

# 4️⃣ Erweiterbare Sensitivity Modifier (Optional)

AAA Systeme nutzen oft mehrere Multiplikatoren:

finalSensitivity =
    baseSensitivity *
    deviceModifier *
    modeModifier *
    fovScale;

Beispiele:

- Combat Mode
- Aim Mode
- Mount Mode

---

# 5️⃣ Integration in bestehende Architektur

Sensitivity gehört in:

👉 CameraInputPipeline

Nicht in:

- CameraBrain
- Animator
- Movement Controller

---

# 6️⃣ Empfohlene Erweiterungen (später)

- Separate Horizontal/Vertical Acceleration
- Adaptive smoothing (weniger smoothing bei schnellen Flicks)
- Zoom speed curves
- Controller vs Mouse Scaling

---

# ✅ Fazit

Die Kombination aus:

- Runtime Settings
- Velocity-based Rotation
- FOV-scaled Sensitivity

bildet die Grundlage für eine hochwertige AAA Camera Steuerung,
ohne bestehende Architektur umzubauen.
