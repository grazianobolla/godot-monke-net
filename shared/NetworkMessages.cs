using Godot;
using System.Runtime.InteropServices;

// Encapsulates user input and other states
public struct UserCommand
{
    public int Id;
    public float DirX, DirY;
}

// Encapsulates current game state for a player
public struct UserState
{
    public int Id;
    public float X, Y, Z;
    public double Time;

    public Vector3 Position
    {
        get { return new Vector3(X, Y, Z); }
    }
}

public struct GameState
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public UserState[] States = new UserState[2];
    public GameState() { }
}