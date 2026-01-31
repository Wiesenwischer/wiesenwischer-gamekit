# 🧰 GameKit – Tools, Pakete & Setup-Empfehlungen für MMO-Entwicklung

Diese Datei enthält zusätzliche Empfehlungen, Tools und Setup-Tipps für die Entwicklung eines modularen, MMO-fähigen GameKit in Unity.

---

## ✅ Editor- und Entwicklungs-Tools

| Zweck | Tool / Paket | Beschreibung |
|-------|--------------|--------------|
| Editor UI | **Odin Inspector** | Erweiterte Editor-GUIs, Validation, Foldouts, etc. |
| Laufzeit-Inspektion | **Runtime Inspector** | Unity-ähnlicher Inspector zur Laufzeit |
| Visual Debugging | **Shapes** | Gizmos, Runtime-Debug-Lines und Overlay-Render |
| DevTools | Eigenes `gamekit.devtools` | Debug-Overlay, Logs, Event-Tracing, Authority-Check |
| Animation | Motion Matching (MMU) | Fortgeschrittene Animationstechnologie |
| Behavior Trees | NodeCanvas / XNode | Visuale Entscheidungsbäume für KI oder Steuerung |
| Save/Load | Easy Save 3 | Plug-and-play Speichersystem |

---

## 🧪 Multiplayer & Netzwerk

| Thema | Tool / Paket | Nutzen |
|-------|--------------|--------|
| Netzwerk | **FishNet** | Modularer, performanter Netcode mit HostMode |
| Netzwerk-Profiler | FishNet Profiler | Debug Netzwerkverkehr, Paketgrößen, Latenz |
| Lag-Simulation | eigene Module | Teste CSP, Snapbacks, visuelles Verhalten |
| Offline-Simulation | Fake NetworkContext | CSP testen ohne echtes Netzwerk |

---

## 🔧 Input, UI & Struktur

| Thema | Empfehlung |
|-------|------------|
| Input-System | Neues Unity InputSystem, entkoppelt via Interfaces |
| Input-Netzwerkadapter | `gamekit.input.fishnet` |
| UI ↔ Gameplay | Nur Intent übergeben, nie direkt steuern |
| Package-Struktur | `Runtime/`, `Editor/`, `Tests/`, `Samples/`, `package.json` |
| Tests | Unity TestRunner + NSubstitute für Interface-Tests |

---

## 🎮 Projektstruktur (Empfehlung)

```
/repos
  gamekit.charactercontroller/
  gamekit.charactercontroller.network/
  gamekit.input/
  gamekit.input.fishnet/
  gamekit.skills/
  gamekit.skills.network/
  gamekit.building/
  gamekit.building.network/
  gamekit.devtools/
```

---

## 📈 MMO-Skalierung (später)

| Thema | Empfehlung |
|-------|------------|
| Persistenz | LiteDB lokal, später DB per API |
| Authentifizierung | JWT, z. B. über eigenen Login-Server oder PlayFab |
| Instanzen / Zonen | Per Szene oder per FishNet Shard/Region |
| Matchmaking | FishNet Relay, Docker Service oder AWS GameLift |

---

## 💡 Fazit

Diese Tools, Pakete und Strukturen helfen dir, modular und zukunftssicher zu entwickeln – sowohl für lokale Tests als auch für MMO-Produktionsbetrieb.

