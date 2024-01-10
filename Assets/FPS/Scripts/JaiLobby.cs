public class JaiLobby
{
	private string m_Id;
	private string m_Name;

	public JaiLobby(string iId)
	{
		m_Id = iId;
		m_Name = iId;
	}

	public string GetId()
	{
		return m_Id;
	}

	public string GetName()
	{
		return m_Name;
	}

	public int GetLoggedPlayerCount()
	{
		return 2;
	}

	public int GetPlayerCountCapacity()
	{
		return 4;
	}
}
