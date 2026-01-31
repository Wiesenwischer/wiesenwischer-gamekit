# 🏠 ArcheAge-inspiriertes Bausystem mit Bauplänen

Dieses Dokument beschreibt ein modulares, erweiterbares Bausystem für ein MMO-ähnliches Spiel, bei dem der Spieler mit Hilfe von Bauplänen (Blueprints) Gebäude und Objekte platzieren kann – inspiriert vom System in ArcheAge.

---

## 🎯 Ziel

- Spieler besitzen **Baupläne** im Inventar (z. B. Haus, Zaun, Feld)
- Durch Nutzung eines Bauplans wird ein **Platzierungsmodus** aktiviert
- Der Spieler sieht eine **Ghost-Preview** des Bauobjekts
- Platzierung wird validiert (Snapping, Claims, Kollisionen)
- Optional: Baufortschritt durch **Construction-Site**, Materialien liefern etc.

---

## 🧩 Komponenten

### 1. `BuildBlueprint` (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "Build/Blueprint")]
public class BuildBlueprint : ScriptableObject
{
    public string displayName;
    public GameObject buildPrefab;
    public bool requiresClaim;
    public bool startAsConstructionSite;
}
```

- Wird über das Inventar verwendet
- Leitet Platzierungslogik ein

---

### 2. `BuildSystem.StartPlacement(Blueprint blueprint)`

```csharp
public void StartPlacement(BuildBlueprint blueprint)
{
    activePreview = Instantiate(blueprint.buildPrefab);
    currentBlueprint = blueprint;
    state = BuildState.Placing;
}
```

- Zeigt ein Platzierungs-Preview (Ghost Object)
- Startet Platzierungsprozess

---

### 3. Platzierung & Validierung

- Spieler kann Position und Rotation anpassen
- Validierung:
  - Position frei?
  - Innerhalb eines Claims?
  - Boden vorhanden?
- Bestätigung durch Eingabe

---

### 4. `ConstructionSite` (optional)

```csharp
public class ConstructionSite : MonoBehaviour
{
    public BuildBlueprint blueprint;
    public Dictionary<ResourceType, int> required;

    public void Deliver(ResourceType type, int amount)
    {
        // Ressourcen liefern
    }
}
```

- Spieler (oder andere) liefern Materialien
- Fortschritt visualisiert (UI, Mesh, Partikel etc.)

---

### 5. Eigentum & Claims (optional)

- Jedes Bauobjekt kennt seinen Eigentümer (z. B. PlayerID, GuildID)
- Optional: Integration mit Claim-System oder Landverwaltung
- Zentrale Abfrage: `CanPlaceAt(Vector3 position, Player player)`

---

### 6. Netzwerkfähigkeit

- Bauobjekte als NetworkObjects
- ConstructionSites über RPCs synchronisiert
- Platzierung erfordert Server-Authority

---

## 📦 Paketstruktur

```
/Packages
├── Module.BuildSystem
│   ├── BuildSystem.cs
│   ├── BuildBlueprint.cs
│   ├── BuildValidator.cs
│   ├── BuildPreviewManager.cs
│   └── BuildInputHandler.cs
├── Module.BuildSystem.Construction
│   └── ConstructionSite.cs
├── Module.BuildSystem.Sync
│   └── NetworkBuildHandler.cs
```

---

## 🔁 Integration in bestehende Systeme

| System | Nutzung |
|--------|--------|
| 🎮 `PlayerController` | Übergibt Position, ruft `StartPlacement()` auf |
| 🎒 `Inventory` | Blueprint ist Item (nutzt `Use()` oder `Activate()`) |
| 🧰 `AbilitySystem` | Blueprint kann optional als Ability behandelt werden |
| 🛡 `ClaimSystem` | Regeln für erlaubte Platzierung |
| 🌐 `FishNet` / `Mirror` | Serverautorisierte Platzierung, Sync |

---

## ✅ Vorteile

- Immersives Bau-Gameplay mit Eigentum & Ressourcen
- Modular entwickelbar: Start mit Blueprint-Platzierung, später Ausbau zu Häusern, Feldern etc.
- Geringe Kopplung: Inventar, Player, UI, BuildSystem sind lose verbunden
- Ideal für Multiplayer

---

## 🚀 Erweiterungsideen

- Bau-Animationen, Sound, Partikel
- Gilden- oder Fraktionsgebäude
- Bauzeiten, Baustellen mit Fortschritt
- Upgrades (z. B. Farm → Gewächshaus)
- Bau auf Schiffen oder beweglichen Plattformen