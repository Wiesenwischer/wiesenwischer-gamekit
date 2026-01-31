# 🦴 IK-Unterstützung im Wiesenwischer.GameKit

**Datum:** 2026-01-30

Diese Spezifikation beschreibt, wie Inverse Kinematics (IK) systematisch in das modulare `GameKit` integriert werden kann – inklusive Client/Server-Aspekten, Modularität und Integration mit dem Animation- und Controller-System.

---

## 🎯 Ziele

- IK modular aktivierbar für verschiedene Controller (z. B. Reiten, Fahrzeug, Normal)
- Integration in CSP-kompatible Architektur
- IK-Zielpunkte netzwerkfähig (Prediction möglich)
- Nutzung vorhandener Unity-IK-Systeme oder externer Lösungen (z. B. Final IK, Animation Rigging)

---

## 🔧 Komponentenübersicht

```
PlayerController
├── IKManager
│   ├── LookAtIK (Kopf)
│   ├── HandIK (Links, Rechts)
│   └── FootIK (optional)
├── IKInputProvider (ermittelt Zielpunkte aus Input)
└── NetworkSyncBridge
```

---

## 📦 Module und Erweiterbarkeit

| Paket                                 | Beschreibung |
|--------------------------------------|--------------|
| `wiesenwischer.gamekit.animation`    | Abstraktion über Animator + Layering |
| `wiesenwischer.gamekit.ik`           | IKManager + Ziele + Aktivierung |
| `wiesenwischer.gamekit.ik.fishnet`   | Netzwerk-Zielsync via FishNet |
| `wiesenwischer.gamekit.controller.*` | Aktiviert spezifische IK-Konfigurationen je nach Mount/Vehikel/State |

---

## 🧠 IK-Zieldefinition

```csharp
public interface IIKTargetProvider
{
    Vector3 GetLookTarget();    // z. B. Kamera-Zentrum, Gegner
    Vector3 GetHandTarget();    // z. B. Zauber-Ziel, Waffenposition
}
```

Ziele kommen aus:
- Kamera / Maus / Spielziel (lokal)
- Netzwerk (z. B. LookTarget von Remote Player)
- Fähigkeiten (e.g. Spell auf Position X)

---

## 🌐 Netzwerkaspekte

Nur die folgenden Daten müssen synchronisiert werden:
- Aktives IK-Modul (z. B. „MountedIK“, „VehicleIK“)
- Zielpunkte (z. B. `Vector3 lookTarget`, `handTarget`)

**Empfehlung:** Sync via `Networked<T>` oder `ObserverRpc`, aber nur wenn Spieler sichtbar ist.

---

## 🧩 Integration mit Animation

IK greift in Animator Layer ein, z. B.:
- `LookAtWeight`, `BodyWeight`, `HeadWeight`
- Blending mit Animationen (z. B. für Zauber-Casting)
- `AvatarMask` für IK/Animation-Splitting (z. B. nur Oberkörper)

---

## 🧪 Beispielverwendung im Controller

```csharp
void UpdateIKTargets()
{
    if (currentController is RidingController) {
        ikManager.Activate("MountedIK");
    }
    else if (currentController is NormalController) {
        ikManager.Activate("StandardIK");
    }

    var lookTarget = ikInputProvider.GetLookTarget();
    ikManager.SetLookTarget(lookTarget);
}
```

---

## 🧭 Nächste Schritte

- `IKManager` als MonoBehaviour entwickeln (einschaltbar pro Controller)
- Abstraktion für Zielpunkte (lokal/netzwerkfähig)
- Synchronisierungskomponenten vorbereiten (FishNet optional)
- IK-Setup-Support im AnimationPackage ermöglichen (z. B. AvatarMasks, Weights)
