
public enum PoseState
{
    Stand,
    ForcedCrouch,
    CrouchInCover
}

public static class PoseStateExtension
{
    public static bool IsForcedState(this PoseState state)
    {
        return state == PoseState.ForcedCrouch;
    }

    public static bool IsCrouching(this PoseState state)
    {
        return state == PoseState.ForcedCrouch;
    }
}
