using Godot;
using MemoryPack;

namespace NetMessage
{
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(UserCommand))]
    [MemoryPackUnion(1, typeof(GameSnapshot))]
    [MemoryPackUnion(2, typeof(Sync))]
    public partial interface ICommand { }

    // Used to calculate latency
    [MemoryPackable]
    public partial class Sync : ICommand
    {
        public int ClientTime;
        public int ServerTime;
    }

    // Encapsulates user input and other client actions
    [MemoryPackable]
    public partial class UserCommand : ICommand
    {
        public int Tick;    // This is the Tick stamp for the latest generated input (Inputs[Inputs.Length])
                            // all other Ticks are (Tick - index)
        public byte[] Inputs;
    }

    // Game state for a given point in time
    [MemoryPackable]
    public partial class GameSnapshot : ICommand
    {
        public EntityState[] States;
        public int Tick;
    }

    // Encapsulates current state for a player (gets sent with gameSnapshot)
    [MemoryPackable]
    public partial class EntityState
    {
        public int Id; // Entity Id
        public float[] PosArray; // Entity Position
        public float[] VelArray; // Entity velocity

        [MemoryPackIgnore]
        public Vector3 Position
        {
            get { return new Vector3(PosArray[0], PosArray[1], PosArray[2]); }
        }

        [MemoryPackIgnore]
        public Vector3 Velocity
        {
            get { return new Vector3(VelArray[0], VelArray[1], VelArray[2]); }
        }
    }

    public enum InputFlags
    {
        Forward = 0b_0000_0001,
        Backward = 0b_0000_0010,
        Left = 0b_0000_0100,
        Right = 0b_0000_1000,
        Space = 0b_0001_0000,
        Shift = 0b_0010_0000,
    }
}