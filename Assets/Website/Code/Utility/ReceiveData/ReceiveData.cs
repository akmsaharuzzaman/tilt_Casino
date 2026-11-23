using System;
using TMPro;
using UnityEngine;
using static ReceiveData;

public class ReceiveData : MonoBehaviour
{
	[System.Serializable]
	public enum ReceiveDataType { amount, userName }

	public ReceiveDataType receiveDataType;

	private void UserDataUpdated()
	{
		if (GetComponent<TextMeshProUGUI>())
		{
			switch (receiveDataType)
			{
				case ReceiveDataType.amount:
					GetComponent<TextMeshProUGUI>().text = $"${Authentication.Instance.CurrentUserAmount}";
					break;
				case ReceiveDataType.userName:
					GetComponent<TextMeshProUGUI>().text = $"${Authentication.Instance.CurrentUserName}";
					break;
			}
			
		}
	}

	void Start()
	{
		if (Authentication.Instance)
		{
			Authentication.Instance.userUpdated += UserDataUpdated;
		}
	}

	private void OnDestroy()
	{
		if (Authentication.Instance)
		{
			Authentication.Instance.userUpdated += UserDataUpdated;
		}
	}
}
