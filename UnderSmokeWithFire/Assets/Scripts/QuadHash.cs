using UnityEngine;

public struct QuadHash
{
    public float a, b, c, d;

    public static QuadHash Create()
    {
        QuadHash hash;
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        return hash;
    }
}
