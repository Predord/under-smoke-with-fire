using UnityEngine;

public class Connection : MonoBehaviour
{
    public TravelPath path;

    private RectTransform[] steps;

    public void SetConnection(float stepLength, Transform startLocation, Transform finalLocation)
    {
        float gapLength = Vector3.Distance(startLocation.position, finalLocation.position) / stepLength;
        Vector3 gapPositionCorrection = (finalLocation.position - startLocation.position).normalized * (gapLength - Mathf.Floor(gapLength)) / 2f;

        Vector3 start = startLocation.position + gapPositionCorrection;
        Vector3 finish = finalLocation.position - gapPositionCorrection;
        gapLength = Mathf.Floor(gapLength);
        steps = new RectTransform[(int)gapLength - 1];

        for (int i = 1; i < gapLength; i++)
        {
            RectTransform step = steps[i - 1] = Instantiate(path.stepPrefab);
            step.SetParent(transform);
            step.position = Vector3.Lerp(start, finish, i / gapLength) + (Vector3.right + Vector3.up) * Random.Range(-stepLength / 5f, stepLength / 5f);
            step.localRotation = Quaternion.LookRotation(startLocation.forward, gapPositionCorrection) * Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));
        }
    }
}
