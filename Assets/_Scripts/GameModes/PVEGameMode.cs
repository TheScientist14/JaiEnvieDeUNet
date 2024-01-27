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

	private int _nextRoomNumber = 0;
	
	public PVEGameMode Instance()
	{
		return instance as PVEGameMode;
	}

	public void CheckEnemies()
	{
		foreach (var VARIABLE  in _waves[_nextRoomNumber-1]._enemiesToActivate)
		{
			var enemy = VARIABLE.GetComponent<EnemyController>();
			if (enemy && enemy.GetComponent<HealthComponent>().GetHealth() > 0)
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
	}

	public void OpenDoorAndActivateEnemies()
	{
		doors[_nextRoomNumber].ToggleDoor();
		ActivateAllWaveBots(_nextRoomNumber);
		_nextRoomNumber++;
	}

	private void ActivateAllWaveBots(int waveNumber)
	{
		foreach (var waveEnemy in _waves[waveNumber]._enemiesToActivate)
		{
			waveEnemy.SetActive(true);
		}
	}
	
}

[Serializable]
public struct Wave
{
	public List<GameObject> _enemiesToActivate;
}
