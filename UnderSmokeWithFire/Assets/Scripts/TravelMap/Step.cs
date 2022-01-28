using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Step : MonoBehaviour
{
    public List<Sprite> steps = new List<Sprite>();

    private Image currentImage;

    private void Awake()
    {
        InitializeStep();
    }

    private void InitializeStep()
    {
        currentImage = GetComponent<Image>();
        currentImage.sprite = steps[Random.Range(0, steps.Count)];
    }
}
