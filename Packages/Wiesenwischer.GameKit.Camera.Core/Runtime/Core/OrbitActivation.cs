namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Konfiguriert wann Orbit-Input gelesen wird.
    /// </summary>
    public enum OrbitActivation
    {
        /// <summary>Maus steuert immer die Kamera. Cursor immer gelockt. (Action Combat)</summary>
        AlwaysOn,

        /// <summary>Orbit nur bei gedrücktem LMB/RMB. Cursor sonst frei. (Classic MMO)</summary>
        ButtonActivated
    }
}
