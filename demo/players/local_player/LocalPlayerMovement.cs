using Godot;
using ImGuiNET;
using MonkeNet.Client;

namespace GameDemo;

// Local Player movement script
public partial class LocalPlayerMovement : CharacterControllerClient<CharacterInputMessage, EntityStateMessage>
{
    // How much we allow our local prediction to deviate from the servers authoritative state this should always be as close to 0 as possible,
    // but for many reasons we allow for a very small margin
    [Export] private float MaxDeviationAllowedThousands = 0.1f;

    // Controls camera movement
    [Export] private FirstPersonCameraController _firstPersonCameraController;

    // If _autoMoveEnabled is ON the player is always pressing some inputs and moving, useful for testing
    private byte _automoveInput = 0b0000_0010;
    private bool _autoMoveEnabled = false;

    public override void _Process(double delta)
    {
        base._Process(delta);
        DisplayDebugInformation();
    }

    // Called each physics tick, captures all inputs pressed at that moment and packs them into a PlayerInputMessage
    protected override CharacterInputMessage CaptureCurrentInput()
    {
        byte keys;

        if (_autoMoveEnabled)
        {
            SolveAutoMove();
            keys = _automoveInput;
        }
        else
        {
            keys = PlayerMovementCalculator.GetCurrentPressedKeys();
        }

        return new CharacterInputMessage
        {
            Keys = keys,
            CameraYaw = _firstPersonCameraController.GetLateralRotationAngle()
        };
    }

    // Called each physics tick, takes the last captured input and process them locally (prediction)
    // the code you run to move the player here must be the same on both the Client (here) and the server
    protected override Vector3 CalculateVelocity(CharacterBody3D character, CharacterInputMessage input)
    {
        return PlayerMovementCalculator.CalculateVelocity(character, input);
    }

    // Called when we receive an authoritative state from the server, here we have to determine if our simulation was succesful, this is up to you
    protected override bool CalculateDeviation(EntityStateMessage incomingState, Vector3 savedState)
    {
        // If the length between our position and the server authoritative position is greater than a threshold, we consider that deviation
        return (incomingState.Position - savedState).LengthSquared() > MaxDeviationAllowedThousands;
    }

    // Called when deviation was detected, we correct to the authoritative position (reconciliation), in this case snapping the player back
    protected override void HandleReconciliation(CharacterBody3D character, EntityStateMessage incomingState)
    {
        character.Position = incomingState.Position;
        character.Velocity = incomingState.Velocity;
    }

    private void SolveAutoMove()
    {
        if (CharacterBody.Position.Z > 5f && _automoveInput == 0b0000_0010)
        {
            _automoveInput = 0b0000_0001;
        }
        else if (CharacterBody.Position.Z < -5f && _automoveInput == 0b0000_0001)
        {
            _automoveInput = 0b0000_0010;
        }
    }

    private new void DisplayDebugInformation()
    {
        if (ImGui.Begin("Player Data"))
        {
            ImGui.Checkbox("Automove?", ref _autoMoveEnabled);
            ImGui.Text("Use `C` to free your cursor.");
            ImGui.End();
        }
    }
}
