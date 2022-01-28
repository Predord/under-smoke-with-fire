using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveItem : MonoBehaviour
{
    public Toggle objectiveToggle;
    public TMP_Text objectiveText;

    public QuadCell targetCell;

    public void SetObjective(bool isObjectiveDone, string description)
    {
        objectiveToggle.isOn = isObjectiveDone;
        objectiveText.text = description;
    }

    public void FocusCameraOnTarget()
    {
        CameraMain.Instance.FocusCameraOnCell(targetCell);
    }
}
