
using Newtonsoft.Json.Linq;
using SGDSOFT.JsonNode;
using System;
using UnityEngine;

public class PokerActionManager : MonoBehaviour
{
	private const string POKER_ACTION = "SGDSOFT@POKER_ACTION";
	private const string POKER_ACTION_RESULT = "SGDSOFT@POKER_ACTION_RESULT";

	public static PokerActionManager Instance { get; private set; }

	public event Action<JObject> OnActionResult;

	public bool IsSendingAction { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}
	private void Start()
	{
		Debug.Log("[POKER] Registering ACTION RESULT event.");

		if (Server.Instance != null)
		{
			Server.Instance.RegisterEvent(
				POKER_ACTION_RESULT,
				HandleActionResult
			);

			Debug.Log("[POKER] ACTION RESULT registered.");
		}
		else
		{
			Debug.LogError("[POKER] Server instance missing during registration.");
		}
	}


	private void OnDisable()
	{
		if (Server.Instance != null)
		{
			Server.Instance.UnregisterEvent(
				POKER_ACTION_RESULT,
				HandleActionResult
			);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	public void Check() => SendAction("CHECK");
	public void Call() => SendAction("CALL");
	public void Fold() => SendAction("FOLD");
	public void Bet(int amount) => SendAction("BET", amount);
	public void Raise(int amount) => SendAction("RAISE", amount);
	public void AllIn() => SendAction("ALL_IN");

	public async void SendAction(string action, int amount = 0)
	{
		Debug.Log("[ACTION MANAGER] SendAction ENTERED!");

		Debug.Log("[ACTION MANAGER] IsSendingAction: " + IsSendingAction);

		Debug.Log("[ACTION MANAGER] Server Exists: " +
			(Server.Instance != null));

		if (IsSendingAction)
		{
			Debug.LogWarning("[POKER] Previous action is still pending.");
			return;
		}

		if (Server.Instance == null || !Server.Instance.IsConnected)
		{
			Debug.LogWarning("[POKER] Server is not connected.");
			return;
		}

		if (string.IsNullOrEmpty(Server.Instance.RoomId))
		{
			Debug.LogWarning("[POKER] Player is not in a room.");
			return;
		}

		action = action.ToUpperInvariant();

		if (action != "FOLD" &&
			action != "CHECK" &&
			action != "CALL" &&
			action != "BET" &&
			action != "RAISE" &&
			action != "ALL_IN")
		{
			Debug.LogWarning("[POKER] Invalid action: " + action);
			return;
		}

		if ((action == "BET" || action == "RAISE") && amount <= 0)
		{
			Debug.LogWarning("[POKER] Invalid betting amount.");
			return;
		}

		JSONObject data = new JSONObject();
		data.AddField("action", action);

		if (action == "BET" || action == "RAISE")
			data.AddField("amount", amount);

		Debug.Log(
			$"[POKER] Sending Event: {POKER_ACTION} | Action: {action} | Amount: {amount}"
		);

		IsSendingAction = true;

		try
		{
			await Server.Instance.SGDSOFT_On(POKER_ACTION, data);

			Debug.Log("[POKER] Transport send completed.");
		}
		catch (Exception e)
		{
			IsSendingAction = false;
			Debug.LogError("[POKER] Action send failed: " + e);
		}
	}

	private void HandleActionResult(JObject data)
	{
		Debug.Log("[POKER] ACTION RESULT RECEIVED: " + data);

		IsSendingAction = false;

		bool success = data["success"]?.Value<bool>() ?? false;
		string action = data["action"]?.ToString() ?? "";

		if (success)
		{
			Debug.Log("[POKER] Action accepted: " + action);
		}
		else
		{
			string message =
				data["result"]?["message"]?.ToString()
				?? data["message"]?.ToString()
				?? "Action rejected.";

			Debug.LogWarning("[POKER] Action rejected: " + message);
		}

		OnActionResult?.Invoke(data);
	}
	public void RaiseTo(long targetAmount)
	{
		if (targetAmount <= 0 || targetAmount > int.MaxValue)
		{
			Debug.LogWarning("[POKER] Invalid Raise To amount: " + targetAmount);
			return;
		}

		SendAction("RAISE", (int)targetAmount);
	}
}
