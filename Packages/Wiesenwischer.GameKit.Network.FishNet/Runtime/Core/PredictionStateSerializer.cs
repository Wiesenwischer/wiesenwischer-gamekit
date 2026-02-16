using FishNet.Serializing;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Custom FishNet Serializer für PredictionState.
    /// </summary>
    public static class PredictionStateSerializer
    {
        public static void WritePredictionState(this Writer writer, PredictionState value)
        {
            writer.WriteInt32(value.Tick);
            writer.WriteVector3(value.Position);
            writer.WriteSingle(value.Rotation);
            writer.WriteVector3(value.Velocity);
            writer.WriteString(value.StateName);
            writer.WriteBoolean(value.IsGrounded);
            writer.WriteSingle(value.Timestamp);
        }

        public static PredictionState ReadPredictionState(this Reader reader)
        {
            int tick = reader.ReadInt32();
            var position = reader.ReadVector3();
            float rotation = reader.ReadSingle();
            var velocity = reader.ReadVector3();
            string stateName = reader.ReadString();
            bool isGrounded = reader.ReadBoolean();
            float timestamp = reader.ReadSingle();

            return new PredictionState
            {
                Tick = tick,
                Position = position,
                Rotation = rotation,
                Velocity = velocity,
                StateName = stateName,
                IsGrounded = isGrounded,
                Timestamp = timestamp
            };
        }
    }
}
