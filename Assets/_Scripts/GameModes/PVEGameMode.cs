using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PVEGameMode : CommonGameMode
{
	[SerializeField] private List<Wave> _waves;
	[SerializeField] private List<DoorBehaviour> doors;

	private int _enemyDeathCounter = 0;
	private int _roomNumber = 0;

	public static PVEGameMode Instance()
	{
		return instance as PVEGameMode;
	}

	public void AddToEnemyDeathCounter()
	{
		_enemyDeathCounter++;
		if(_enemyDeathCounter == _waves[_roomNumber]._enemiesToActivate.Count)
		{
			OpenDoorAndActivateEnemies();
		}
	}

	protected override void OnAllPlayersConnected()
	{
		base.OnAllPlayersConnected();

		foreach(var door in doors)
		{
			door.ToggleDoor();
		}

		// ActivateAllWaveBotsClientRpc(0);
		// ActivateAllWaveBots(0);

	}

	public void OpenDoorAndActivateEnemies()
	{
		doors[_roomNumber].ToggleDoor();
		ActivateAllWaveBotsClientRpc(_roomNumber);
		ActivateAllWaveBots(_roomNumber);

	}

	[ClientRpc]
	private void ActivateAllWaveBotsClientRpc(int waveNumber)
	{
		ActivateAllWaveBots(waveNumber);
	}

	private void ActivateAllWaveBots(int waveNumber)
	{
		if(waveNumber <= _waves.Count)
		{
			foreach(var waveEnemy in _waves[waveNumber]._enemiesToActivate)
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
