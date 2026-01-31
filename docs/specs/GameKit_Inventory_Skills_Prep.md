# 🧩 GameKit – Vorbereitende Spezifikation für Inventory- & Skillsystem

Diese Spezifikation legt die minimale Struktur und Vorbereitung für spätere Inventory- und Skill-Systeme im GameKit fest – ohne sie bereits vollständig zu implementieren.

---

## 🎯 Ziel

Ein skalierbares MMO-GameKit soll vorbereitet werden auf:
- Inventar- und Ausrüstungsmanagement
- Aktive und passive Fähigkeiten
- Integration in UI, Input-System, Netzwerk
- Server Authority und Persistenz (später)

---

## 🗂️ Minimale Startstruktur (Schnittstellen & Platzhalter)

### 📦 `gamekit.inventory`
- `IItem`
- `IInventory`
- `IEquipableItem`
- `IInventorySlot`

🔹 Erste Implementierung:
- `SOItemDefinition` (ScriptableObject)
- `DummyInventory : MonoBehaviour` mit fixer Itemliste

---

### 📦 `gamekit.skills`
- `ISkill`
- `ISkillSlot`
- `ISkillUser`
- `ISkillEffect`

🔹 Erste Implementierung:
- `FireballSkill : MonoBehaviour`
- `DummySkillBar : MonoBehaviour` (z. B. 4 Slots)
- Input-Mapper: Taste → Slot → Skill → Execute()

---

### 🔀 Skills & Inventory verbinden
- `SkillRequirementComponent` → prüft Item-Voraussetzungen (z. B. Zauberstab)
- `InventoryCondition` → Skill ist nur aktivierbar, wenn Item X vorhanden

---

## 🔧 Technische Schnittstellen (Start)

```csharp
public interface IItem {
    string Id { get; }
    Sprite Icon { get; }
    string DisplayName { get; }
}

public interface ISkill {
    string Id { get; }
    string Name { get; }
    Sprite Icon { get; }
    void Execute(ISkillUser user);
}
```

---

## 🔒 Authority & Netzwerk (nur vorbereiten)

| Thema | Vorbereitung |
|-------|--------------|
| Item Ownership | `IItem.OwnerId` (z. B. Guid / PlayerRef) |
| Skill Cast Sync | Skill Cast an `ISkillUserNetwork` melden |
| Server Validation | `CanExecute()` auf Server prüfen lassen |
| Sync | Cooldowns, Skillstates synchronisieren (FishNet später) |

---

## 📋 Erste Aufgaben & Roadmap

| Priorität | Aufgabe |
|----------|---------|
| 🔵 Hoch | Interfaces definieren (`IItem`, `ISkill`, etc.) |
| 🟡 Mittel | Dummy-Implementierung mit ScriptableObjects |
| 🟢 Niedrig | Platzhalter-UI für Skillleiste, Inventar anzeigen |
| 🟢 Niedrig | Testbare Dummies für Unit Tests und Integrationstests |

---

## 🧠 Fazit

Das System ist so vorbereitet, dass du:
- Jetzt keine volle Implementation brauchst
- Später alles modular entwickeln kannst
- Netzwerk und UI direkt andocken kannst

