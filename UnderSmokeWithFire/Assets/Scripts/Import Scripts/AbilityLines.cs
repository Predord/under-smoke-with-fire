using UnityEngine;

public class AbilityLines : MonoBehaviour
{
    public Material material;

    [Range(4, 32)]
    public int quadsPerLine;

    public int segmentsPerLine;    
    public float outerRadius;
    public Material lineMaterial;

    [HideInInspector]
    public float currentSegmentsPerLine;
    [HideInInspector]
    public Vector3 destination;

    private delegate Vector3 DrawFunction(Vector3 a, Vector3 b, Vector3 c, float t);
    private DrawFunction drawFunction;

    private void OnPostRender()
    {
        RenderLinesQuads();
    }

    private void RenderLinesQuads()
    {
        if (Player.Instance && Player.Instance.playerInput.AttackMode && Player.Instance.playerInput.IsValidAttackPoint && drawFunction != null)
        {
            Vector3 origin = Player.Instance.transform.position;
            origin.y += Player.Instance.Height / 2f;
            Vector3 middle = Vector3.Lerp(origin, destination, 0.5f);
            middle.y = Mathf.Max(destination.y, origin.y) + Player.Instance.playerInput.currentAbilityCurveMiddlePoint;
            Vector3 angle = new Vector3(0f, 0f, 180f - ((quadsPerLine - 2f) / quadsPerLine * 180f));

            lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.LoadProjectionMatrix(CameraMain.Instance._camera.projectionMatrix);
            GL.Begin(GL.QUADS);
            GL.Color(Color.white);

            float segmentFragment = 1f / segmentsPerLine;
            Vector3 currentOrigin = origin;
            Vector3 currentDestination = drawFunction.Invoke(origin, middle, destination, segmentFragment);

            for (int j = 0; j < currentSegmentsPerLine; j++)
            {
                Vector3 localForward = currentDestination - currentOrigin;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, localForward);

                Vector3 currentPosition1 = Vector3.up * outerRadius;
                currentPosition1 = Quaternion.Euler(angle / 2f) * currentPosition1;
                currentPosition1 = rotation * currentPosition1;
                currentPosition1 += currentOrigin;
                Vector3 currentPosition2 = currentPosition1 + localForward;

                for (int i = 0; i < quadsPerLine; i++)
                {
                    GL.Vertex(currentPosition2);
                    GL.Vertex(currentPosition1);                   

                    currentPosition1 = Vector3.up * outerRadius;
                    currentPosition1 = Quaternion.Euler((1.5f + i) * angle) * currentPosition1;
                    currentPosition1 = rotation * currentPosition1;
                    currentPosition1 += currentOrigin;
                    currentPosition2 = currentPosition1 + localForward;

                    GL.Vertex(currentPosition1);
                    GL.Vertex(currentPosition2);                    
                }

                currentOrigin = currentDestination;
                currentDestination = drawFunction.Invoke(origin, middle, destination, segmentFragment * (j + 2));
            }

            GL.End();
            GL.PopMatrix();
        }
    }

    public void SetDrawFunction(TrajectoryTypes type)
    {
        if(type == TrajectoryTypes.None)
        {
            drawFunction = null;
        }
        else if(type == TrajectoryTypes.Linear)
        {
            drawFunction = LinearDraw;
        }
        else if(type == TrajectoryTypes.Curve)
        {
            drawFunction = Bezier.GetPoint;
        }
    }

    private Vector3 LinearDraw(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return Vector3.Lerp(a, c, t);
    }
}
