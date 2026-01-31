# 🧩 MMO-Grundlagen für GameKit – Architektur & Setup

**Stand:** 2026-01-30  
**Ziel:** Diese Datei dokumentiert essentielle Prinzipien, Strukturen und Empfehlungen für ein MMO-fähiges GameKit mit Netzwerkunterstützung (FishNet), modularem Aufbau und klarer Trennung von Zuständigkeiten.

---

## ✅ Grundprinzipien für ein MMO-fähiges Framework

### 1. Server vs. Client klar trennen
- **Server = autoritative Quelle** für alle Spiellogik und Zustände
- **Client = Darstellung + lokale Vorhersage (CSP)**  
- Visuals dürfen nicht die Quelle von Logik sein

---

### 2. Alle Subsysteme netzwerkfähig *denken*
- Auch „lokale“ Systeme wie Crafting, Building, Skills etc.
- Schon jetzt:
  - klare Verantwortlichkeiten
  - Netzwerkadapter
  - synchronisierbare Datenmodelle

---

### 3. Struktur für Shared/Client/Server Code

```
/GameKit.Shared
/GameKit.Client
/GameKit.Server
```

> Ergänzt durch Assembly Definitions und Unity Packages

---

### 4. Keine direkten `GetComponent<>()`-Verkettungen
- Stattdessen:
  - Dependency Injection (DI)
  - Services & Registries
  - ScriptableObject-Factories für Zustände und Fähigkeiten

---

### 5. Zustands- und Bewegungssync minimal halten
- Nur **was relevant ist**, synchronisieren:
  - Beispiel: `MovementIntent`, nicht Position/Rotation ständig
  - Zustände wie `IsCasting`, `IsJumping`, `Health`

---

### 6. Client-Side Prediction (CSP)
- Muss von Anfang an konzeptionell eingeplant werden
- Bewegung, Kampf, Build-Aktion, Interaktion
- Komponenten:
  - Lokale Vorhersage
  - Eingabepufferung
  - Korrektur durch Server

---

### 7. Input-System und UI vollständig entkoppeln
- Input via Interface: `IMovementInput`, `ICombatInput`, ...
- UI löst „Absichten“ aus, keine direkten Spielaktionen

---

### 8. Autoritätsprüfung auf dem Server
- Jeder Input, jede Aktion wird **serverseitig validiert**
- z. B. `CanCastSpell()`, `IsInRange()`, `HasMana()`

---

### 9. Identität, Session & Ownership
- Eindeutige Player IDs
- Besitz von Objekten (Pferd, Haus, Charakter)
- Session Context: Wer steuert was, was darf er?

---

### 10. Debug- & Replay-Unterstützung
- Eingabepufferung (lokal/server)
- Wiederholbarkeit von Zuständen
- Snapback-Systeme bei Korrekturen
- Testbare Spielabläufe (z. B. AI statt echter Clients)

---

## 🧱 Projektstruktur (Beispiel)

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
```

---

## 🗺️ Roadmap – From Zero to MMO-Ready

| Phase | Ziel | Inhalt |
|-------|------|--------|
| 1️⃣ | 🧱 **Core Setup** | Input, Controller, StateMachine, Fähigkeiten |
| 2️⃣ | 🌐 **Netzwerkfähig** | FishNet, Authority, Sync, Prediction |
| 3️⃣ | ⚙️ **Systeme** | BuildSystem, Crafting, Combat |
| 4️⃣ | 🎮 **MMO Loop** | Session Mgmt, Persistenz, Multiplayer |
| 5️⃣ | 🧪 **Tools** | Replay, Snapback, Cheat-Test, DevUI |

---

## 🧭 Empfehlung

Baue **jedes neue Modul** von Anfang an:
- mit Trennung `Client/Server`
- mit abstrahiertem Input
- mit optionaler Netzwerk-Bridge (z. B. FishNet)

> So bleibst du flexibel – Singleplayer, Koop, MMO – ohne Umbau.

