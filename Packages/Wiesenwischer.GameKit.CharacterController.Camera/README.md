# GameKit Character Controller - Camera Package

Third-Person Kamera-System für den GameKit Character Controller.

## Features

- **Orbit-Kontrolle**: Drehe die Kamera um den Spieler
- **Kollisionserkennung**: Kamera durchdringt keine Wände
- **Smooth Follow**: Sanftes Folgen des Ziels
- **Zoom**: Mausrad oder Gamepad-Trigger
- **Konfigurierbar**: Alle Parameter über ScriptableObject

## Installation

Das Package erfordert:
- `wiesenwischer.gamekit.charactercontroller.core` (0.1.0+)
- `com.unity.cinemachine` (2.9.7+)
- `com.unity.inputsystem` (1.7.0+)

## Quick Start

### Automatisches Setup

1. Erstelle eine Szene mit Player (siehe Core Package)
2. Gehe zu **Wiesenwischer > GameKit > Camera > Setup Third Person Camera**
3. Die Kamera wird automatisch konfiguriert und dem Player zugewiesen

### Manuelles Setup

1. Wähle die Main Camera
2. Füge `ThirdPersonCamera` Component hinzu
3. Füge `CameraInputHandler` Component hinzu
4. Erstelle eine CameraConfig: **Wiesenwischer > GameKit > Camera > Create Default Camera Config**
5. Weise die Config und das Target zu

## Steuerung

| Input | Aktion |
|-------|--------|
| Maus Bewegung | Kamera drehen |
| Mausrad | Zoom In/Out |
| Rechter Stick (Gamepad) | Kamera drehen |

## Components

### ThirdPersonCamera

Hauptkomponente für die Kamera-Steuerung.

**Properties:**
- `Config`: CameraConfig ScriptableObject
- `Target`: Transform des Ziels

**Methoden:**
- `SetRotationInput(Vector2)`: Setzt Rotations-Input
- `SetZoomInput(float)`: Setzt Zoom-Input
- `SnapBehindTarget()`: Positioniert Kamera hinter Ziel
- `TeleportToTarget()`: Sofortige Positionierung ohne Smoothing

### CameraInputHandler

Verbindet das Input System mit der Kamera.

**Properties:**
- `LookAction`: InputActionReference für Look
- `ZoomAction`: InputActionReference für Zoom

### CameraConfig

ScriptableObject mit allen Kamera-Einstellungen.

#### Distance
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Default Distance | 5 | Standard-Abstand |
| Min Distance | 2 | Minimaler Zoom |
| Max Distance | 15 | Maximaler Zoom |

#### Sensitivity
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Horizontal Sensitivity | 200 | Horizontale Drehgeschwindigkeit |
| Vertical Sensitivity | 150 | Vertikale Drehgeschwindigkeit |
| Zoom Sensitivity | 2 | Zoom-Geschwindigkeit |

#### Limits
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Min Vertical Angle | -40 | Minimaler Blickwinkel (nach unten) |
| Max Vertical Angle | 70 | Maximaler Blickwinkel (nach oben) |

#### Smoothing
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Follow Damping | 0.1 | Glättung beim Folgen |
| Rotation Damping | 0.05 | Glättung bei Rotation |
| Zoom Damping | 0.1 | Glättung beim Zoomen |

#### Collision
| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| Collision Layers | Everything | Layer für Kollision |
| Collision Radius | 0.3 | Radius der Kollisions-Sphere |
| Collision Snap Speed | 10 | Geschwindigkeit bei Kollision |
| Collision Recovery Speed | 2 | Rückkehr nach Kollision |

## Cinemachine Integration

Das Package ist vorbereitet für Cinemachine-Integration. Für erweiterte Kamera-Features wie:
- Mehrere virtuelle Kameras
- Camera Blending
- Dolly-Tracks
- Screen Shake

Nutze die Cinemachine FreeLook oder Virtual Camera und deaktiviere die ThirdPersonCamera.

## Troubleshooting

### Kamera dreht sich nicht
- Prüfe ob CameraInputHandler vorhanden ist
- Prüfe ob Input Actions korrekt zugewiesen sind
- Prüfe ob Cursor Lock aktiv ist (Config > Lock Cursor)

### Kamera zittert
- Erhöhe Follow Damping und Rotation Damping
- Stelle sicher dass die Kamera in LateUpdate läuft

### Kamera durchdringt Wände
- Prüfe Collision Layers in der Config
- Erhöhe Collision Radius
- Stelle sicher dass Wände Collider haben
