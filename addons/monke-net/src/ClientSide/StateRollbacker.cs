namespace MonkeNet.Client;

/// <summary>
/// Handles going back in time to a certain tick and resimulating the world state forward again.
/// </summary>
public partial class StateRollbacker : ClientNetworkNode
{
    protected override void OnProcessTick(int currentTick, int currentRemoteTick)
    {

    }
}
