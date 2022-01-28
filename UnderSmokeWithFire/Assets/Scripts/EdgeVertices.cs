using UnityEngine;

public struct EdgeVertices 
{
    public Vector3 vertix1, vertix2, vertix3, vertix4, vertix5;

    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        vertix1 = corner1;
        vertix2 = Vector3.Lerp(corner1, corner2, 0.25f);
        vertix3 = Vector3.Lerp(corner1, corner2, 0.5f);
        vertix4 = Vector3.Lerp(corner1, corner2, 0.75f);
        vertix5 = corner2;
    }

    public void AddPosY(float y)
    {
        vertix1.y += y;
        vertix2.y += y;
        vertix3.y += y;
        vertix4.y += y;
        vertix5.y += y;
    }

    public void SetPosY(float y)
    {
        vertix1.y = vertix2.y = vertix3.y = vertix4.y = vertix5.y = y;
    }

    public void SetSideSlopeCoordinates(float elevationCorner1, float elevationCorner2)
    {
        vertix1.y = elevationCorner1;
        vertix2.y = elevationCorner1 + (elevationCorner2 - elevationCorner1) * 0.25f;
        vertix3.y = elevationCorner1 + (elevationCorner2 - elevationCorner1) * 0.5f;
        vertix4.y = elevationCorner1 + (elevationCorner2 - elevationCorner1) * 0.75f;
        vertix5.y = elevationCorner2;
    }

    public EdgeVertices MirrorVerices()
    {
        Vector3 vertix = vertix1;
        vertix1 = vertix5;
        vertix5 = vertix;
        vertix = vertix2;
        vertix2 = vertix4;
        vertix4 = vertix;

        return this;
    }
}


