using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EntityFOV))]
public class FieldOfViewEditor : Editor
{
    private void OnSceneGUI()
    {
        EntityFOV fov = (EntityFOV)target;
        float range = fov.enemy.Location ? fov.enemy.VisionRange : 15f;

        Handles.color = Color.white;
        Handles.DrawWireArc(fov.transform.position + Vector3.up * fov.enemy.Height, Vector3.up, Vector3.forward, 360, range);

        Vector3 viewAngle01 = DirectionFromAngle(fov.transform.eulerAngles.y, -fov.GetAngle() / 2);
        Vector3 viewAngle02 = DirectionFromAngle(fov.transform.eulerAngles.y, fov.GetAngle() / 2);

        Handles.color = Color.yellow;
        Handles.DrawLine(
            fov.transform.position + Vector3.up * fov.enemy.Height, 
            fov.transform.position + Vector3.up * fov.enemy.Height + viewAngle01 * range
        );
        Handles.DrawLine(
            fov.transform.position + Vector3.up * fov.enemy.Height, 
            fov.transform.position + Vector3.up * fov.enemy.Height + viewAngle02 * range
        );

        if (fov.CanSeePlayer)
        {
            Handles.color = Color.green;
            Handles.DrawLine(
                fov.transform.position + Vector3.up * fov.enemy.Height, 
                Player.Instance.transform.position + Vector3.up * Player.Instance.Height
            );
        }
    }

    private Vector3 DirectionFromAngle(float eulerY, float angleInDegrees)
    {
        angleInDegrees += eulerY;

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
