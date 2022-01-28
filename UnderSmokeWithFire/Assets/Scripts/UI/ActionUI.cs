using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionUI : Singleton<ActionUI>
{
    public RectTransform leaveMapWindow;
    public RectTransform gameOverWindow;
    public Button lookAroundCornerLeft;
    public Button lookAroundCornerRight;
    public Button waitTurn;
    public Button pickUpIntelObjective;
    public Button burnIntelObjective;
    public TMP_Text actionPointsPopup;
    public ActionTooltip tooltip;
    public UIBuffDebuffAMList actionMapBuffsDebuffs;

#pragma warning disable 0649
    [SerializeField] private HealthPanel playerHealthPanel;
    [SerializeField] private FatiguePanel playerFatiguePane;
    [SerializeField] private HealthPanel entityHealthPanel;
    [SerializeField] private UIBuffDebuffAMList actionMapEntityBuffsDebuffs;
#pragma warning restore 0649

    private bool binded;
    private float currentActionPointsValue;

    private void Awake()
    {
        if (!RegisterMe())
            return;
    }

    private void Start()
    {
        BindPlayer();
    }

    private void OnEnable()
    {
        Entity.OnSelectedEntityChange += HandleSelectedEntityChange;
    }

    private void OnDisable()
    {
        Entity.OnSelectedEntityChange -= HandleSelectedEntityChange;

        currentActionPointsValue = -1f;
        lookAroundCornerLeft.onClick.RemoveAllListeners();
        lookAroundCornerRight.onClick.RemoveAllListeners();
        waitTurn.onClick.RemoveAllListeners();
        pickUpIntelObjective.onClick.RemoveAllListeners();
        burnIntelObjective.onClick.RemoveAllListeners();
    }

    public void SetActionPointsPopup(float value, Vector3 worldPostion)
    {
        actionPointsPopup.rectTransform.position = CameraMain.Instance._camera.WorldToScreenPoint(worldPostion);
        if (currentActionPointsValue != value)
        {
            actionPointsPopup.SetText("{0} AP", value);
            currentActionPointsValue = value;
        }
    }

    public void HandleSelectedEntityChange(Entity entity)
    {
        if (entityHealthPanel)
            entityHealthPanel.Bind(entity);

        if (actionMapEntityBuffsDebuffs)
        {
            if (entity)
            {
                actionMapEntityBuffsDebuffs.BindList(entity);
            }
            else
            {
                actionMapEntityBuffsDebuffs.UnbindList();
            }
        }          
    }

    public void BindPlayer()
    {
        if (Player.Instance && !binded)
        {
            if (actionMapBuffsDebuffs)
                actionMapBuffsDebuffs.BindList(Player.Instance);

            if (playerHealthPanel)
                playerHealthPanel.Bind(Player.Instance);

            if (playerFatiguePane)
                playerFatiguePane.Bind();

            binded = true;
        }
    }

    public void UnbindPlayer()
    {
        if (actionMapBuffsDebuffs)
            actionMapBuffsDebuffs.UnbindList();

        if (playerHealthPanel)
            playerHealthPanel.Bind(null);

        if (playerFatiguePane)
            playerFatiguePane.Unbind();

        currentActionPointsValue = -1f;
        lookAroundCornerLeft.onClick.RemoveAllListeners();
        lookAroundCornerRight.onClick.RemoveAllListeners();
        waitTurn.onClick.RemoveAllListeners();
        pickUpIntelObjective.onClick.RemoveAllListeners();
        burnIntelObjective.onClick.RemoveAllListeners();

        binded = false;
    }

    public void LeaveMap()
    {
        PlayerInfo.characterBuffsDebuffs.RemoveAll(buffDebuff => buffDebuff.id > 4);
        SceneLoader.Instance.LoadTravelMapScene();
    }

    public void CloseLeaveWindow()
    {
        PlayerInfo.OnExitActionMap();
        leaveMapWindow.gameObject.SetActive(false);
    }

    public void OpenGameOverWindow()
    {
        gameOverWindow.gameObject.SetActive(true);
    }
}
