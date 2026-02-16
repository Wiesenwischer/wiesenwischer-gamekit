using FishNet.Serializing;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Custom FishNet Serializer für ControllerInput.
    /// Wird automatisch von FishNet erkannt (statische Extension Methods auf Writer/Reader).
    /// </summary>
    public static class ControllerInputSerializer
    {
        public static void WriteControllerInput(this Writer writer, ControllerInput value)
        {
            writer.WriteInt32(value.Tick);
            writer.WriteVector2(value.MoveDirection);
            writer.WriteVector2(value.LookDirection);
            writer.WriteSingle(value.Rotation);
            writer.WriteUInt16((ushort)value.Buttons);
            writer.WriteSingle(value.Timestamp);
        }

        public static ControllerInput ReadControllerInput(this Reader reader)
        {
            int tick = reader.ReadInt32();
            var move = reader.ReadVector2();
            var look = reader.ReadVector2();
            float rotation = reader.ReadSingle();
            var buttons = (ControllerButtons)reader.ReadUInt16();
            float timestamp = reader.ReadSingle();

            return new ControllerInput
            {
                Tick = tick,
                MoveDirection = move,
                LookDirection = look,
                Rotation = rotation,
                Buttons = buttons,
                Timestamp = timestamp
            };
        }
    }
}
