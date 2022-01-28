using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputManager : MonoBehaviour
{
    public void OnMenuButton(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (GameUI.Instance.menuPanel.gameObject.activeSelf)
            {
                GameUI.Instance.CloseMenu();
            }
            else if (GameUI.Instance.abilityPanel.gameObject.activeSelf || GameUI.Instance.statsPanel.gameObject.activeSelf)
            {
                GameUI.Instance.CloseAll();
            }
            else if (GameManager.Instance.IsActionMap && Player.Instance && Player.Instance.playerInput.AttackMode && !Player.Instance.playerInput.Casting)
            {
                Player.Instance.playerInput.ExitAttackMode();
                GameManager.Instance.grid.FindPlayerDistance();
            }
            else
            {
                GameUI.Instance.OpenMenu();
            }
        }
    }

    public void OnOpenActionListButton(InputAction.CallbackContext context)
    {
        if (context.performed && !GameManager.paused)
        {
            if (Player.Instance && Player.Instance.playerInput.AttackMode && !Player.Instance.playerInput.Casting)
            {
                Player.Instance.playerInput.ExitAttackMode();
            }

            GameUI.Instance.OpenPlayerActionList();
        }
    }

    public void OnOpenPlayerStatsButton(InputAction.CallbackContext context)
    {
        if (context.performed && !GameManager.paused)
        {
            if (Player.Instance && Player.Instance.playerInput.AttackMode && !Player.Instance.playerInput.Casting)
            {
                Player.Instance.playerInput.ExitAttackMode();
            }

            GameUI.Instance.OpenPlayerStats();
        }
    }
}
