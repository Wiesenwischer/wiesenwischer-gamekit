using System;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Kompakter Snapshot der Animation-Parameter für Netzwerk-Übertragung.
    /// Speed: 0-2.0 → quantisiert als byte (0-255, Auflösung ~0.008)
    /// VerticalVelocity: -50..+20 → quantisiert als short (-500..+200, Auflösung 0.1)
    /// </summary>
    [Serializable]
    public struct AnimationSnapshot : IEquatable<AnimationSnapshot>
    {
        public byte SpeedQuantized;
        public short VerticalVelocityQuantized;

        private const float SpeedMax = 2f;
        private const float VelocityMin = -50f;
        private const float VelocityMax = 20f;
        private const float VelocityScale = 10f;

        public float Speed
        {
            get => SpeedQuantized / 255f * SpeedMax;
            set => SpeedQuantized = (byte)(Mathf.Clamp01(value / SpeedMax) * 255);
        }

        public float VerticalVelocity
        {
            get => VerticalVelocityQuantized / VelocityScale;
            set => VerticalVelocityQuantized = (short)(
                Mathf.Clamp(value, VelocityMin, VelocityMax) * VelocityScale);
        }

        public static AnimationSnapshot Create(float speed, float verticalVelocity)
        {
            var snapshot = new AnimationSnapshot();
            snapshot.Speed = speed;
            snapshot.VerticalVelocity = verticalVelocity;
            return snapshot;
        }

        public bool Equals(AnimationSnapshot other)
        {
            return SpeedQuantized == other.SpeedQuantized
                && VerticalVelocityQuantized == other.VerticalVelocityQuantized;
        }

        public override bool Equals(object obj) => obj is AnimationSnapshot other && Equals(other);
        public override int GetHashCode() => SpeedQuantized << 16 | (ushort)VerticalVelocityQuantized;
    }
}
