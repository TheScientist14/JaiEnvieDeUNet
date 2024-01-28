using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
	[SerializeField] private GameObject m_ScorePanel;
	[SerializeField] private List<TextMeshProUGUI> m_ScoreTxts;

	private CaptureTheFlagGamemode m_Gamemode;

	private void Start()
	{
		m_Gamemode = CaptureTheFlagGamemode.Instance();
		m_ScorePanel.SetActive(false);
	}

	void Update()
	{
		if(m_ScorePanel.activeInHierarchy)
		{
			UpdateUI();

			if(InputManager.instance.PlayerHideScore())
				m_ScorePanel.SetActive(false);
		}
		else
		{
			if(InputManager.instance.PlayerShowScore())
				m_ScorePanel.SetActive(true);
		}
	}

	public void UpdateUI()
	{
		int teamIdx = 0;
		foreach(TextMeshProUGUI scoreTxt in m_ScoreTxts)
		{
			scoreTxt.text = m_Gamemode.GetTeamPoints(teamIdx).ToString();
			teamIdx++;
		}
	}
}
