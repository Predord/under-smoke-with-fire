using UnityEngine;

public static class Bezier
{
    public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float r = 1f - t;
        return r * r * a + 2f * r * t * b + t * t * c;
    }

    public static Vector3 GetDerivative(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return 2f * ((1f - t) * (b - a) + t * (c - b));
    }

    public static float GetLength(Vector3 a, Vector3 b, Vector3 c, float t)
    {       
        Vector3 previousPoint = GetPoint(a, b, c, 0.005f);

        float length = Vector3.Distance(a, previousPoint);
        float step = 0.01f;

        while (step <= t)
        {
            length += Vector3.Distance(previousPoint, previousPoint = GetPoint(a, b, c, step));
            step += 0.005f;
        }

        return length;
    }
}