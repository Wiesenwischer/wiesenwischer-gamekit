# Basic Movement Sample

Dieses Sample demonstriert die Grundfunktionen des Character Controller Core Packages.

## Voraussetzungen

- Unity 2022.3 LTS oder neuer
- Unity Input System Package (wird automatisch als Dependency installiert)

## Inhalt des Samples

```
BasicMovement/
├── InputActions/
│   └── CharacterControllerActions.inputactions  # Vorkonfigurierte Input Actions
└── README.md
```

## Quick Start

### 1. Automatisches Setup (Empfohlen)

Nach Installation des Packages und Import des Samples:

1. Öffne Unity
2. Gehe zu **Wiesenwischer > GameKit > Create Default MovementConfig**
3. Gehe zu **Wiesenwischer > GameKit > Create Demo Scene**
4. Gehe zu **Wiesenwischer > GameKit > Create Core Prefabs** (optional)
5. Weise im Player-Objekt die MovementConfig zu
6. Drücke Play!

### 2. Input Actions konfigurieren

Die mitgelieferten Input Actions (`InputActions/CharacterControllerActions.inputactions`) sind bereits konfiguriert für:
- Keyboard & Mouse
- Gamepad
- Touch
- Joystick

Um sie zu verwenden:
1. Kopiere die `.inputactions` Datei in dein Assets-Verzeichnis
2. Oder nutze die bereits in Unity existierenden Default Input Actions

### 3. Manuelles Setup

Falls du die Szene manuell erstellen möchtest:

1. Erstelle eine neue Szene
2. Erstelle ein leeres GameObject und nenne es "Player"
3. Füge folgende Components hinzu:
   - `CharacterController` (Unity Built-in)
   - `PlayerController` (aus diesem Package)
   - `PlayerInputProvider` (aus diesem Package)
4. Erstelle ein `MovementConfig` Asset:
   - Rechtsklick im Project Window
   - Create > Wiesenwischer > GameKit > Movement Config
5. Weise die Config dem PlayerController zu
6. Erstelle einen Ground Plane (3D Object > Plane)

## Steuerung

| Taste | Aktion |
|-------|--------|
| WASD / Pfeiltasten | Bewegung |
| Leertaste | Springen |
| Shift (Links) | Sprinten |
| Maus | Blickrichtung |

### Gamepad

| Button | Aktion |
|--------|--------|
| Left Stick | Bewegung |
| Right Stick | Blickrichtung |
| A / Cross | Springen |
| Left Stick Press | Sprinten |

## Demo-Szene Inhalt

Die generierte Demo-Szene enthält:

- **Ground**: Große Bodenfläche (100x100 Units)
- **Obstacles**: Verschiedene Hindernisse zum Überspringen
- **Slopes**: Schrägen in 30°, 45°, und 60°
- **Stairs**: Treppe mit 5 Stufen
- **Player**: Vorkonfigurierter Spieler-Character

## Features demonstriert

- **Walking/Running**: Verschiedene Geschwindigkeiten mit Shift
- **Jumping**: Variable Sprunghöhe basierend auf Tastendruck
- **Coyote Time**: Kurze Sprungmöglichkeit nach Verlassen des Bodens
- **Jump Buffer**: Sprung-Input wird kurz vor Landung gebuffert
- **Slope Handling**: Bewegung auf Schrägen bis zum konfigurierten Maximalwinkel
- **Step Detection**: Automatisches Erklimmen kleiner Stufen
- **State Machine**: Visuelles Feedback des aktuellen Movement States

## Konfiguration (MovementConfig)

Die `MovementConfig` ist ein ScriptableObject mit allen anpassbaren Parametern:

### Ground Movement
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Walk Speed | 5 | Normale Bewegungsgeschwindigkeit |
| Run Speed | 10 | Sprint-Geschwindigkeit |
| Acceleration | 50 | Beschleunigung |
| Deceleration | 50 | Verzögerung beim Stoppen |

### Air Movement
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Air Control | 0.5 | Kontrolle in der Luft (0-1) |
| Gravity | -20 | Schwerkraft |
| Max Fall Speed | -30 | Maximale Fallgeschwindigkeit |

### Jumping
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Jump Height | 2 | Maximale Sprunghöhe in Units |
| Coyote Time | 0.15 | Zeit nach Ground-Verlust für Sprung |
| Jump Buffer Time | 0.1 | Buffer für Sprung-Input vor Landung |

### Ground Detection
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Ground Check Distance | 0.2 | Reichweite der Bodenerkennung |
| Ground Layers | Default | Welche Layer als Boden zählen |
| Max Slope Angle | 45 | Maximaler begehbarer Winkel |

## Debug-Informationen

### Inspector
Im Play Mode zeigt der Custom Inspector:
- Aktueller State (Grounded, Airborne, Jumping, Falling)
- Horizontal Velocity
- Vertical Velocity
- Ground Status & Slope Info
- State History

### Gizmos
Aktiviere "Draw Gizmos" im PlayerController für:
- Ground Check Sphere (grün wenn geerdet, rot wenn in der Luft)
- Velocity Vector
- Ground Normal

## Menü-Befehle

| Menü | Aktion |
|------|--------|
| Wiesenwischer > GameKit > Create Demo Scene | Erstellt komplette Demo-Szene |
| Wiesenwischer > GameKit > Create Core Prefabs | Erstellt alle Prefabs |
| Wiesenwischer > GameKit > Create Default MovementConfig | Erstellt MovementConfig Asset |
| Wiesenwischer > GameKit > Show Compile Log | Zeigt Kompilierungs-Log |
| Wiesenwischer > GameKit > Force Recompile | Erzwingt Neukompilierung |

## Troubleshooting

### Player bewegt sich nicht
- Prüfe ob `MovementConfig` im PlayerController zugewiesen ist
- Prüfe ob Ground Layer in der Config korrekt gesetzt ist
- Prüfe ob Input System aktiviert ist (Edit > Project Settings > Player > Active Input Handling)

### Player fällt durch den Boden
- Stelle sicher dass der Boden einen Collider hat
- Prüfe die Ground Check Distance in der Config
- Prüfe ob der Ground Layer in "Ground Layers" der Config enthalten ist

### Sprung funktioniert nicht
- Prüfe ob Jump-Taste korrekt gemapped ist
- Prüfe ob Coyote Time > 0 ist
- Im Debug Inspector schauen ob "IsGrounded" true ist vor dem Sprung
