using UnityEngine;

public abstract class Command : MonoBehaviour
{
    public abstract void Execute();
    public abstract void Stop();
}
