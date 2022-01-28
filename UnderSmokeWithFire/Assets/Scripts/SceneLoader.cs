using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    public CanvasGroup canvasGroup;

    private void Awake()
    {
        if (!RegisterMe())
        {
            return;
        }
        gameObject.SetActive(false);
    }

    public void LoadTravelMapScene()
    {
        gameObject.SetActive(true);
        StartCoroutine(StartTravelMapLoad());
    }

    public void LoadActionMapScene()
    {
        gameObject.SetActive(true);
        StartCoroutine(StartActionMapLoad());
    }

    public void LoadMenuScene()
    {
        gameObject.SetActive(true);
        StartCoroutine(StartMenuLoad());
    }

    private IEnumerator StartTravelMapLoad()
    {
        yield return StartCoroutine(FadeLoadingScreen(1, 1));

        AsyncOperation operation = SceneManager.LoadSceneAsync("TravelMap");
        while (!operation.isDone)
        {
            yield return null;
        }

        operation = SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);

        while (!operation.isDone)
        {
            yield return null;
        }
     
        GameManager.Instance.IsActionMap = false;

        yield return StartCoroutine(FadeLoadingScreen(0, 1));
        gameObject.SetActive(false);
    }

    private IEnumerator StartActionMapLoad()
    {
        yield return StartCoroutine(FadeLoadingScreen(1, 1));

        AsyncOperation operation = SceneManager.LoadSceneAsync("QuadMap");
        while (!operation.isDone)
        {
            yield return null;
        }

        operation = SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);

        while (!operation.isDone)
        {
            yield return null;
        }

        GameManager.Instance.IsActionMap = true;
        GameManager.Instance.Load(GameManager.Instance.currentMap);
        AudioManager.Instance.PlayAudio(true, "UndetectedTheme");
        CameraMain.Instance.FocusCameraOnCellInstantly(Player.Instance.Location);

        yield return StartCoroutine(FadeLoadingScreen(0, 1));
        gameObject.SetActive(false);
    }

    private IEnumerator StartMenuLoad()
    {
        yield return StartCoroutine(FadeLoadingScreen(1, 1));

        AsyncOperation operation = SceneManager.LoadSceneAsync("Menu");
        while (!operation.isDone)
        {
            yield return null;
        }

        GameManager.Instance.IsActionMap = false;

        yield return StartCoroutine(FadeLoadingScreen(0, 1));
        gameObject.SetActive(false);
    }

    private IEnumerator FadeLoadingScreen(float targetValue, float duration)
    {
        float startValue = canvasGroup.alpha;
        float time = 0;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startValue, targetValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetValue;
    }
}
