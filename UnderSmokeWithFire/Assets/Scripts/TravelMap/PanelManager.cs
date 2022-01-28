using UnityEngine;
using System.Collections;

public class PanelManager : MonoBehaviour
{
	public Animator initiallyOpen;

	private int m_OpenParameterId;
	private Animator m_Open;

	private const string k_OpenTransitionName = "Open";
	private const string k_ClosedStateName = "Closed";

	public void OnEnable()
	{
		m_OpenParameterId = Animator.StringToHash(k_OpenTransitionName);

		if (initiallyOpen == null)
			return;

		OpenPanel(initiallyOpen);
	}

	public void OpenPanel(Animator anim)
	{
		if (m_Open == anim)
			return;

		anim.gameObject.SetActive(true);

		CloseCurrent();

		m_Open = anim;
		m_Open.SetBool(m_OpenParameterId, true);
	}

	public void CloseCurrent()
	{
		if (m_Open == null)
			return;

		m_Open.SetBool(m_OpenParameterId, false);
		StartCoroutine(DisablePanelDeleyed(m_Open));
		m_Open = null;
	}

	private IEnumerator DisablePanelDeleyed(Animator anim)
	{
		bool closedStateReached = false;
		bool wantToClose = true;
		while (!closedStateReached && wantToClose)
		{
			if (!anim.IsInTransition(0))
				closedStateReached = anim.GetCurrentAnimatorStateInfo(0).IsName(k_ClosedStateName);

			wantToClose = !anim.GetBool(m_OpenParameterId);

			yield return new WaitForEndOfFrame();
		}

		if (wantToClose)
			anim.gameObject.SetActive(false);
	}
}
