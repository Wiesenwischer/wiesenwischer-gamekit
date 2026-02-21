# Phase 38: Adaptive Correction Rate (RTT-basiert)

> **Status:** Geplant
> **Abhängigkeit:** Phase 31 (Adaptive Reconciliation & Smooth Correction)
> **Erstellt:** 2026-02-21

---

## Motivation

Der `ReconcileSmoother` aus Phase 31 verwendet statische Werte für `_correctionRate` (Offset-Decay-Geschwindigkeit) und `_snapThreshold` (ab welchem Error sofort korrigiert wird). Diese Werte sind ein Kompromiss:

- **Zu schnelle Correction Rate:** Bei hoher Latenz (viele/große Reconcile-Korrekturen) ist der Decay zu aggressiv → sichtbare Ruckler, weil der Offset zu schnell abgebaut wird bevor der nächste Reconcile kommt.
- **Zu langsame Correction Rate:** Bei niedriger Latenz reagiert das Visual träge → "Gummiband-Effekt", der Character fühlt sich unresponsiv an.

Die Lösung: **Correction Rate dynamisch an die Netzwerk-Latenz anpassen.**

---

## Vorbild: FishNet UniversalTickSmoother

FishNets eigener `UniversalTickSmoother` implementiert ein ähnliches Konzept:

```csharp
// FishNet: Adaptive Interpolation basierend auf RTT
private void UpdateRealtimeInterpolation()
{
    float rtt = _networkManager.TimeManager.RoundTripTime;
    float tickDelta = (float)_networkManager.TimeManager.TickDelta;

    // RTT → Tick-Count umrechnen
    uint interpolation = TimeManager.TicksToTime(TickRateType.Variable, rtt, tickDelta);

    // Interpolation-Buffer skaliert mit Latenz
    _goalData.Interpolation = interpolation;
}
```

**Kernidee:** Je höher die Latenz, desto mehr Buffer/Smoothing-Zeit wird dem visuellen System gegeben. Die Interpolation passt sich automatisch an, statt mit statischen Werten zu arbeiten.

Wir nutzen FishNets `UniversalTickSmoother` nicht direkt (inkompatibel mit KCC-Motor), aber das **Konzept der RTT-adaptiven Smoothing-Parameter** übernehmen wir.

---

## Design

### RTT-Quelle

```csharp
// FishNet stellt RTT direkt bereit:
float rtt = NetworkManager.TimeManager.RoundTripTime; // in Sekunden
```

### RTT → CorrectionRate Mapping

```csharp
// RTT in Ticks umrechnen
float rttTicks = rtt / tickDelta;

// Mapping: wenige Ticks RTT → schneller Decay, viele Ticks → langsamer Decay
// Beispiel-Werte (müssen getuned werden):
float correctionRate = Mathf.Lerp(
    0.6f,   // Niedrige Latenz (1 Tick RTT): schneller Decay → snappy
    0.15f,  // Hohe Latenz (8+ Ticks RTT): langsamer Decay → mehr Smoothing-Zeit
    Mathf.InverseLerp(1f, 8f, rttTicks)
);
```

**Erklärung der Werte:**
- `rttTicks = 1` (≈33ms bei 30Hz): Lokales Netzwerk → fast sofortige Korrektur
- `rttTicks = 4` (≈133ms bei 30Hz): Typische Internet-Verbindung → mittlere Decay-Rate
- `rttTicks = 8+` (≈266ms+ bei 30Hz): Schlechte Verbindung → langsamer Decay, mehr visuelles Smoothing

### RTT-Smoothing (EMA)

Rohe RTT-Werte schwanken stark (Jitter, Spikes). Ein Exponential Moving Average glättet:

```csharp
private float _smoothedRtt;
private const float RttSmoothFactor = 0.1f; // 10% neuer Wert, 90% alter Wert

private void UpdateSmoothedRtt(float currentRtt)
{
    _smoothedRtt = Mathf.Lerp(_smoothedRtt, currentRtt, RttSmoothFactor);
}
```

### Optionale Erweiterung: Adaptiver SnapThreshold

```csharp
// Bei hoher Latenz: größere Korrekturen tolerieren (nicht sofort snappen)
float snapThreshold = Mathf.Lerp(
    0.5f,   // Niedrige Latenz: kleine Korrekturen sind OK
    2.0f,   // Hohe Latenz: erst bei >2m sofort snappen
    Mathf.InverseLerp(1f, 8f, rttTicks)
);
```

---

## Integration in ReconcileSmoother

### Neue Felder

```csharp
[Header("Adaptive Correction (RTT-basiert)")]
[SerializeField] private bool _adaptiveCorrectionEnabled = true;
[SerializeField] private float _minCorrectionRate = 0.15f;  // Hohe Latenz
[SerializeField] private float _maxCorrectionRate = 0.6f;   // Niedrige Latenz
[SerializeField] private float _minRttTicks = 1f;
[SerializeField] private float _maxRttTicks = 8f;
[SerializeField] private float _rttSmoothFactor = 0.1f;

private float _smoothedRtt;
private float _adaptiveCorrectionRate;
```

### RTT-Provider Interface

Um das Netzwerk-Package nicht direkt zu referenzieren (ReconcileSmoother liegt im selben Package, aber für Testbarkeit):

```csharp
public interface IRttProvider
{
    float RoundTripTime { get; }
}
```

FishNet-Implementierung:
```csharp
public class FishNetRttProvider : IRttProvider
{
    private readonly NetworkManager _networkManager;

    public float RoundTripTime => (float)_networkManager.TimeManager.RoundTripTime;
}
```

### LateUpdate-Änderung

```csharp
private void LateUpdate()
{
    // ... bestehende Interpolation ...

    // Adaptive Rate berechnen (einmal pro Frame)
    float rate = _adaptiveCorrectionEnabled
        ? _adaptiveCorrectionRate
        : _correctionRate;

    // Offset-Decay mit adaptiver Rate
    float dt = Time.deltaTime * 60f;
    _positionOffset *= Mathf.Pow(1f - rate, dt);
    _rotationOffset *= Mathf.Pow(1f - _rotationCorrectionRate, dt);

    // ...
}
```

---

## Debug-Visualisierung

In der bestehenden `NetworkDebugUI` oder als eigene Erweiterung:

```
RTT: 85ms (2.6 ticks)
Smoothed RTT: 82ms (2.5 ticks)
Correction Rate: 0.42 (adaptive)
Snap Threshold: 0.8m (adaptive)
Current Offset: 0.023m
```

---

## Schritte

### 38.1 RTT-Provider Interface + FishNet-Implementierung
- `IRttProvider` Interface im Network-Package
- `FishNetRttProvider` Implementierung
- EMA-Smoothing für RTT

### 38.2 Adaptive CorrectionRate im ReconcileSmoother
- Neue SerializeField-Parameter für Min/Max Rate und RTT-Range
- RTT → CorrectionRate Mapping in LateUpdate
- Fallback auf statischen Wert wenn `_adaptiveCorrectionEnabled = false`

### 38.3 Adaptive SnapThreshold (optional)
- Gleiche RTT-basierte Skalierung für SnapThreshold
- Evaluieren ob das tatsächlich hilft oder ob ein statischer Threshold reicht

### 38.4 Debug UI — RTT + aktuelle CorrectionRate anzeigen
- Bestehende NetworkDebugUI erweitern
- RTT (raw + smoothed), Tick-Count, aktuelle Rate, Threshold anzeigen

### 38.5 Tests + Verifikation unter simulierter Latenz
- Unit Tests: RTT → Rate Mapping korrekt
- Unit Tests: EMA-Smoothing filtert Spikes
- Integration: ParrelSync mit simulierter Latenz (FishNet Latency Simulator)
- Verifikation: Bei konstanter Latenz stabile Rate, bei Latenz-Änderung smooth Anpassung

---

## Risiken & Offene Fragen

1. **Tuning-Aufwand:** Die Min/Max-Werte für CorrectionRate und RTT-Range müssen empirisch getuned werden. Evtl. ScriptableObject für einfaches Tuning im Editor.
2. **Server-Hosted (kein RTT):** Auf dem Server/Host ist RTT = 0. Adaptive Rate muss diesen Fall handhaben (Fallback auf maximale Rate oder Smoothing komplett deaktivieren).
3. **RTT-Spikes:** Ein einzelner RTT-Spike sollte nicht sofort die Rate ändern → EMA-Smoothing ist kritisch.
4. **Rotation Rate:** Aktuell ist `_rotationCorrectionRate` separat. Soll die auch RTT-adaptiv werden? Wahrscheinlich ja, aber niedrigere Priorität.
