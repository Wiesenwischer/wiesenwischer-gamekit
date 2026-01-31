# 🎨 Alternative Visualisierungen für Bauzustände

Dieses Dokument ergänzt das ArcheAge-inspirierte Bausystem um drei verschiedene Möglichkeiten zur Darstellung von Platzierung und Baufortschritt – **ohne neue 3D-Modelle** bauen zu müssen:

---

## 🔹 1. Shaderbasierte Bauzustände

Shader können visuelle Effekte erzeugen, die Bauprozesse oder Vorschauen simulieren, ohne dass andere Modelle benötigt werden.

### 🧙‍♂️ Mögliche Shader-Effekte

| Effekt | Beschreibung |
|--------|--------------|
| **Ghost Shader** | Halbtransparentes Objekt (z. B. Cyan), mit Additiven Linien |
| **Blueprint Shader** | Simuliert eine Blaupause – Gitterlinien, Leuchten, leuchtender Boden |
| **Dissolve Shader** | Objekt „entsteht“ mit steigender Bau-Fortschritt (z. B. von unten) |
| **Wireframe FX** | Kombiniert Outline mit durchsichtigen Flächen |
| **Fresnel/Glühen** | Glühende Kanten für Bau-Vorschau oder Aktivierung |

> Mit Shader Graph (URP/HDRP) können Shader mit Parametern wie `_Progress` gebaut werden.

```csharp
material.SetFloat("_Progress", constructionProgress);
```

---

## 🔸 2. ProBuilder-basierte Platzhalterobjekte

### 🛠 Was ist ProBuilder?

Unitys integriertes Tool zum schnellen Erstellen einfacher 3D-Modelle direkt im Editor.

### 🧱 Verwendungsideen

| Objekt | Zweck |
|--------|-------|
| Cube (Bodenplatte) | Fundament des Bauplatzes |
| Plane + Blueprint-Textur | Visualisierung eines geplanten Gebäudes |
| Lowpoly-Wände oder -Gerüst | Temporäre Form des Hauses |
| Transparente Blöcke | Zeigen Volumen an |

---

### 🧰 Beispielstruktur

```
ConstructionSite.prefab
├── ProBuilder_Cube (Fundament)
├── Blueprint_Plane (Plane mit Textur)
├── Canvas (UI: 0–100 %)
```

---

## 🔹 3. Bauphasen durch Aktivierung einzelner Meshes

> Ideal für Assets, die in mehrere Teile untergliedert sind (z. B. Wände, Dach, Details)

### 🧱 Prinzip

- Objekt besteht aus mehreren Child-Objekten mit eigenen `MeshRenderer`s
- Je nach Fortschritt werden diese **schrittweise sichtbar**
- Kann mit UI, Shader oder Partikeln kombiniert werden

### 📂 Strukturbeispiel

```
Construction_House.prefab
├── BuildStages
│   ├── Stage01_Base
│   ├── Stage02_Walls
│   ├── Stage03_Roof
│   └── Stage04_Details
```

### 🧩 Script: `ConstructionVisualizer.cs`

```csharp
public class ConstructionVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject[] stages;

    public void ShowStage(int index)
    {
        for (int i = 0; i < stages.Length; i++)
            stages[i].SetActive(i <= index);
    }
}
```

### 🧠 Vorteile

- Ideal bei fertigen Assets mit mehreren Meshteilen
- Kein Shader nötig
- Kombinierbar mit Partikeleffekten und Sound
- Lässt sich serverseitig gut synchronisieren (StageIndex)

---

## 🔁 Kombinierte Lösung (empfohlen)

| Phase | Visualisierung |
|-------|----------------|
| 🟡 Platzierung | Ghost- oder Blueprint-Shader |
| 🟠 Baustelle gestartet | ProBuilder-Objekte oder Shader mit geringem Fortschritt |
| 🟢 Baufortschritt | Shader-Parameter **oder** aktivierte Mesh-Stufen |
| ✅ Fertig | Ersetzung durch echtes Modell mit Standardmaterial |

---

## ✅ Vorteile

- Keine neuen 3D-Modelle nötig
- Voll dynamisch auch zur Laufzeit
- Einfach mit Unity Bordmitteln umsetzbar
- Unterstützt visuelles Feedback im Multiplayer

---

## 📦 Integration in BuildSystem

Empfohlene Unterstruktur für modulare Unterstützung:

```
/Packages
├── Module.BuildSystem
│   └── ConstructionVisuals
│       ├── ConstructionVisualizer.cs
│       ├── ShaderController.cs
│       ├── ProBuilderPlaceholders.prefab
│       └── BlueprintShader.mat
```