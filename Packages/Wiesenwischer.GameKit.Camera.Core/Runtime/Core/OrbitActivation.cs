namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Konfiguriert wann Orbit-Input gelesen wird.
    /// </summary>
    public enum OrbitActivation
    {
        /// <summary>Maus steuert immer die Kamera. Cursor immer gelockt. (BDO/Action Combat)</summary>
        AlwaysOn,

        /// <summary>Orbit nur bei gedrücktem LMB/RMB. Cursor sonst frei. (ArcheAge/WoW/Tab-Target)</summary>
        ButtonActivated
    }
}
