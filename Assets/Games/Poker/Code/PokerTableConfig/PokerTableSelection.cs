
using Poker;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PokerTableSelection : MonoBehaviour
{
	public PokerTableConfig SelectedTable { get; set; }

	public Image parentImage;
	public GameObject unselected;
	public Image smallCardBG;


	[Header("DEMO Player Information")]
	[SerializeField] private string userName = "Stringer";
	[SerializeField] private string avatarId = "avatar-1";
	[SerializeField] private string email = "Stringer@gmail.com";
	[SerializeField] private int amount = 1000;

	public void PlayButton()
	{
		JoinSelectedTable(userName, email, amount);
	}

	void JoinSelectedTable(string userName, string email, int amount)
	{
		if (SelectedTable == null)
		{
			Debug.LogError("[POKER] Please select a table first.");
			return;
		}
		if (Server.Instance.IsConnected)
		{
			PokerGameController.instance.JoinRoom(
				userName,
				email,
				amount,
				SelectedTable.smallBlind,
				SelectedTable.bigBlind
			);
		}
	}
}
