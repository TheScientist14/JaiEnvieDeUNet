using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.FPS.AI;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class PVEGameMode : CommonGameMode
{
	[SerializeField] private List<Wave> _waves;
	[SerializeField] private List<DoorBehaviour> doors;

	private int _roomNumber = 0;
	
	public static PVEGameMode Instance()
	{
		return instance as PVEGameMode;
	}

	public void CheckEnemies()
	{
		foreach (var enemy  in _waves[_roomNumber]._enemiesToActivate)
		{
			EnemyController enemyController = enemy.GetComponent<EnemyController>();
			if (enemyController && enemy.GetComponent<HealthComponent>().GetHealth() > 0)
			{
				return;
			}
		}
		
		OpenDoorAndActivateEnemies();
		
	}

	protected override void OnAllPlayersConnected()
	{
		base.OnAllPlayersConnected();

		foreach (var door in doors)
		{
			door.ToggleDoor();
		}
		
		ActivateAllWaveBots(0);
		
	}

	public void OpenDoorAndActivateEnemies()
	{
		doors[_roomNumber].ToggleDoor();
		ActivateAllWaveBots(_roomNumber);
		
	}

	private void ActivateAllWaveBots(int waveNumber)
	{
		if (waveNumber <= _waves.Count)
		{
			foreach (var waveEnemy in _waves[waveNumber]._enemiesToActivate)
            {
            	waveEnemy.SetActive(true);
            }
			_roomNumber++;
		}
	}
	
}

[Serializable]
public struct Wave
{
	public List<GameObject> _enemiesToActivate;
}
