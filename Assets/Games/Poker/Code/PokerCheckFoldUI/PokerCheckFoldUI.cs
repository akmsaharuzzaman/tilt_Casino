
using Newtonsoft.Json.Linq;
using SGDSOFT.JsonNode;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Poker;

public class PokerCheckFoldUI : MonoBehaviour
{
	private const string SET_CHECK_FOLD = "SGDSOFT@SET_CHECK_FOLD";
	private const string CHECK_FOLD_RESULT = "SGDSOFT@CHECK_FOLD_RESULT";
	private const string PLAYER_ACTION = "SGDSOFT@PLAYER_ACTION";
	private const string HAND_STARTED = "SGDSOFT@HAND_STARTED";

	private const string PLAYER_TURN = "SGDSOFT@PLAYER_TURN";
	private const string PLAYER_ACTION_RESULT = "SGDSOFT@POKER_ACTION_RESULT";

	[Header("UI References")]
	[SerializeField] private Button checkFoldButton;
	[SerializeField] private GameObject checkFoldActiveMark;
	[SerializeField] private TMP_Text buttonText;

	[Header("Button Labels")]
	[SerializeField] private string enabledText = "CHECK/FOLD ON";
	[SerializeField] private string disabledText = "CHECK/FOLD";

	public bool IsEnabled { get; private set; }
	public bool IsSending { get; private set; }

	public event Action<bool> OnCheckFoldChanged;

	private bool IsMyTurn;

	private void Start()
	{
		if (Server.Instance == null)
		{
			Debug.LogError("[CHECK-FOLD] Server instance not found.");
			return;
		}

		Server.Instance.RegisterEvent(
			CHECK_FOLD_RESULT,
			HandleCheckFoldResult
		);

		Server.Instance.RegisterEvent(
			PLAYER_ACTION,
			HandlePlayerAction
		);

		Server.Instance.RegisterEvent(
			HAND_STARTED,
			HandleHandStarted
		);

		Server.Instance.RegisterEvent(PLAYER_TURN, HandlePlayerTurn);
		Server.Instance.RegisterEvent(PLAYER_ACTION_RESULT, HandlePlayerActionResult);

		if (checkFoldButton != null)
		{
			checkFoldButton.onClick.AddListener(ToggleCheckFold);
		}

		UpdateUI();
	}

	private void OnDestroy()
	{
		if (checkFoldButton != null)
		{
			checkFoldButton.onClick.RemoveListener(ToggleCheckFold);
		}

		if (Server.Instance != null)
		{
			Server.Instance.UnregisterEvent(
				CHECK_FOLD_RESULT,
				HandleCheckFoldResult
			);

			Server.Instance.UnregisterEvent(
				PLAYER_ACTION,
				HandlePlayerAction
			);

			Server.Instance.UnregisterEvent(
				HAND_STARTED,
				HandleHandStarted
			);

			Server.Instance.UnregisterEvent(PLAYER_TURN, HandlePlayerTurn);
			Server.Instance.UnregisterEvent(PLAYER_ACTION_RESULT, HandlePlayerActionResult);
		}
	}

	private void HandlePlayerTurn(JObject data)
	{
		string playerId = data["playerId"]?.ToString();

		IsMyTurn = !string.IsNullOrEmpty(playerId) &&
				   playerId == Server.Instance.PlayerId;

		UpdateUI();
	}

	private void HandlePlayerActionResult(JObject data)
	{
		IsMyTurn = false;
		UpdateUI();
	}

	public void ToggleCheckFold()
	{
		SetCheckFold(!IsEnabled);
		PokerUIController.Instance.TurnOffAutoCheckAndFoldAction();
	}

	public async void SetCheckFold(bool enabled)
	{
		if (IsSending)
			return;

		if (Server.Instance == null || !Server.Instance.IsConnected)
		{
			Debug.LogWarning("[CHECK-FOLD] Server not connected.");
			return;
		}

		if (string.IsNullOrEmpty(Server.Instance.RoomId))
		{
			Debug.LogWarning("[CHECK-FOLD] Not inside a Poker room.");
			return;
		}

		JSONObject data = new JSONObject();
		data.AddField("enabled", enabled);

		IsSending = true;

		UpdateUI();

		try
		{
			await Server.Instance.SGDSOFT_On(
				SET_CHECK_FOLD,
				data
			);
		}
		catch (Exception e)
		{
			IsSending = false;
			UpdateUI();

			Debug.LogError("[CHECK-FOLD] Send failed: " + e.Message);
		}
	}

	private void HandleCheckFoldResult(JObject data)
	{
		IsSending = false;

		bool success = data["success"]?.Value<bool>() ?? false;

		if (!success)
		{
			Debug.LogWarning(
				"[CHECK-FOLD] " +
				(data["message"]?.ToString() ?? "Request rejected.")
			);

			IsEnabled = false;
			UpdateUI();
			return;
		}

		bool enabled = data["enabled"]?.Value<bool>() ?? false;
		bool executed = data["executed"]?.Value<bool>() ?? false;

		IsEnabled = enabled && !executed;

		UpdateUI();

		Debug.Log(
			$"[CHECK-FOLD] Enabled: {IsEnabled} | Executed: {executed}"
		);

		OnCheckFoldChanged?.Invoke(IsEnabled);
	}

	private void HandlePlayerAction(JObject data)
	{
		string playerId = data["playerId"]?.ToString();

		if (string.IsNullOrEmpty(playerId))
			return;

		if (playerId != Server.Instance.PlayerId)
			return;

		if (!IsEnabled)
			return;

		IsEnabled = false;
		IsSending = false;

		UpdateUI();

		OnCheckFoldChanged?.Invoke(false);

		Debug.Log("[CHECK-FOLD] Automatically or manually executed.");
	}

	private void HandleHandStarted(JObject data)
	{
		IsEnabled = false;
		IsSending = false;
		IsMyTurn = false;

		UpdateUI();

		OnCheckFoldChanged?.Invoke(false);
	}

	private void UpdateUI()
	{
		if (checkFoldButton != null)
		{
			checkFoldButton.interactable = !IsSending;
		}

		if (buttonText != null)
		{
			buttonText.text = IsEnabled
				? enabledText
				: disabledText;
		}

		if (checkFoldButton != null)
			checkFoldButton.interactable = !IsMyTurn && !IsSending;

		if (buttonText != null)
			buttonText.text = IsEnabled ? enabledText : disabledText;


		checkFoldActiveMark.SetActive(IsEnabled);
		
	}
}
