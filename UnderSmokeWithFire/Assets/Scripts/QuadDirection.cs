
public enum QuadDirection
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
}

public static class QuadDirectionExtension
{
    public static QuadDirection Opposite(this QuadDirection direction)
    {
        return (int)direction < 4 ? (direction + 4) : (direction - 4);
    }

    public static QuadDirection Previous(this QuadDirection direction)
    {
        return direction == QuadDirection.North ? QuadDirection.NorthWest : (direction - 1);
    }

    public static QuadDirection Next(this QuadDirection direction)
    {
        return direction == QuadDirection.NorthWest ? QuadDirection.North : (direction + 1);
    }

    public static QuadDirection Previous2(this QuadDirection direction)
    {
        return direction >= QuadDirection.East ? (direction - 2) : (direction + 6);
    }

    public static QuadDirection Next2(this QuadDirection direction)
    {
        return direction <= QuadDirection.SouthWest ? (direction + 2) : (direction - 6);
    }

    public static float CoverDirectionToRotation(this QuadDirection direction)
    {
        if(direction == QuadDirection.North)
        {
            return 180f;
        }
        else if(direction == QuadDirection.East)
        {
            return 270f;
        }
        else if(direction == QuadDirection.South)
        {
            return 0f;
        }
        else
        {
            return 90f;
        }
    }
}
