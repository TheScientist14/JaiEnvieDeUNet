using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
	[SerializeField] private HealthComponent m_HealthComponent;

	[SerializeField] private Slider m_HealthSlider;
	[SerializeField] private TextMeshProUGUI m_HealthText;
	[SerializeField] private TextMeshProUGUI m_MaxHealthText;

	// Start is called before the first frame update
	void Start()
	{
		m_MaxHealthText.text = "/" + m_HealthComponent.MaxHealth;
		m_HealthSlider.maxValue = m_HealthComponent.MaxHealth;

		m_HealthComponent.OnValueChanged(_UpdateUI);

		_UpdateUI(0, m_HealthComponent.GetHealth());
	}

	private void _UpdateUI(sbyte _, sbyte iVal)
	{
		int val = Mathf.Clamp(iVal, 0, m_HealthComponent.MaxHealth);

		m_HealthSlider.value = val;
		m_HealthText.text = val.ToString();
	}
}
