using Poker;
using UnityEngine;

public class PokerTester : MonoBehaviour
{
	[Header("Player Information")]
	[SerializeField] private string userName = "Stringer";
	[SerializeField] private string avatarId = "avatar-1";
	[SerializeField] private string email = "Stringer@gmail.com";
	[SerializeField] private int amount = 1000;


    public void Join()
	{
		if (Server.Instance.IsConnected)
		{
			PokerGameController.instance.JoinRoom(userName, email, amount,0,0);
		}
	}
}