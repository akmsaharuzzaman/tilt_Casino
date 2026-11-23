
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;
using Poker;

public class PokerRematchController : MonoBehaviour
{
	public PokerGameController gameController;
	public PokerTableUI pokerTableUI;
	[Header("UI")]
	[SerializeField] private GameObject rematchPanel;
	[SerializeField] private TMP_Text countdownText;
	[SerializeField] private TMP_Text messageText;
	[SerializeField] private Button acceptButton;
	[SerializeField] private Button declineButton;

	[Header("Settings")]
	[SerializeField] private float defaultCountdown = 5f;

	private Coroutine countdownCoroutine;

	private bool responseSent;

	private const string REMATCH_OFFER =
		"SGDSOFT@REMATCH_OFFER";

	private const string REMATCH_RESPONSE =
		"SGDSOFT@REMATCH_RESPONSE";

	private const string REMATCH_RESULT =
		"SGDSOFT@REMATCH_RESULT";

	private void Awake()
	{
		if (rematchPanel != null)
			rematchPanel.SetActive(false);

		acceptButton.onClick.AddListener(AcceptRematch);
		declineButton.onClick.AddListener(DeclineRematch);
	}

	private void Start()
	{
		if (Server.Instance == null)
			return;

		Server.Instance.RegisterEvent(
			REMATCH_OFFER,
			OnRematchOffer
		);

		Server.Instance.RegisterEvent(
			REMATCH_RESULT,
			OnRematchResult
		);
	}

	private void OnDisable()
	{
		if (Server.Instance != null)
		{
			Server.Instance.UnregisterEvent(
				REMATCH_OFFER,
				OnRematchOffer
			);

			Server.Instance.UnregisterEvent(
				REMATCH_RESULT,
				OnRematchResult
			);
		}

		StopCountdown();
	}

	private void OnRematchOffer(JObject data)
	{
		Debug.Log("[REMATCH] Offer received.");

		responseSent = false;

		float duration =
			data["seconds"]?.Value<float>() ?? defaultCountdown;

		if (rematchPanel != null)
			rematchPanel.SetActive(true);

		acceptButton.interactable = true;
		declineButton.interactable = true;

		if (messageText != null)
		{
			messageText.text =
				data["message"]?.ToString()
				?? "Do you want to play another match?";
		}

		StartCountdown(duration);
	}

	private void StartCountdown(float duration)
	{
		StopCountdown();

		countdownCoroutine =
			StartCoroutine(Countdown(duration));
	}

	private IEnumerator Countdown(float duration)
	{
		float remaining = duration;

		while (remaining > 0f && !responseSent)
		{
			if (countdownText != null)
			{
				countdownText.text =
					$"Next Match: {Mathf.CeilToInt(remaining)}";
			}

			yield return new WaitForSecondsRealtime(1f);

			remaining -= 1f;
		}

		if (!responseSent)
		{
			if (countdownText != null)
				countdownText.text = "Time Up!";

			SendResponse(false);
		}

		countdownCoroutine = null;
	}

	public void AcceptRematch()
	{
		SendResponse(true);
	}

	public void DeclineRematch()
	{
		SendResponse(false);
	}

	private async void SendResponse(bool accept)
	{
		if (responseSent)
			return;

		if (Server.Instance == null ||
			!Server.Instance.IsConnected)
		{
			Debug.LogError("[REMATCH] Server not connected.");
			return;
		}

		responseSent = true;

		StopCountdown();

		acceptButton.interactable = false;
		declineButton.interactable = false;

		if (countdownText != null)
		{
			countdownText.text = accept
				? "Waiting for opponent..."
				: "Leaving table...";
		}

		JSONObject data = new JSONObject();

		data.AddField("roomId", Server.Instance.RoomId);
		data.AddField("accept", accept);

		try
		{
			await Server.Instance.SGDSOFT_On(
				REMATCH_RESPONSE,
				data
			);

			Debug.Log(
				"[REMATCH] Response sent: " + accept
			);

			pokerTableUI.PersonalAndCommunityRankParentObjectDeActive();
		}
		catch (Exception e)
		{
			Debug.LogError(
				"[REMATCH] Send failed: " + e.Message
			);
		}
	}

	private void OnRematchResult(JObject data)
	{
		string status = data["status"]?.ToString();

		Debug.Log("[REMATCH] Status: " + status);

		switch (status)
		{
			case "ACCEPTED":

				if (messageText != null)
					messageText.text = "Everyone accepted!";

				if (countdownText != null)
					countdownText.text = "Starting next match...";

				gameController.ResetBoard();
				break;

			case "REJECTED":

				ClosePanel();

				Debug.Log("[REMATCH] Rematch rejected.");
				break;

			case "STARTED":

				ClosePanel();

				responseSent = false;

				Debug.Log("[REMATCH] Next match started.");
				break;

			case "ENDED":

				ClosePanel();

				Debug.Log("[REMATCH] Match ended.");
				break;
		}
	}

	private void ClosePanel()
	{
		StopCountdown();

		if (rematchPanel != null)
			rematchPanel.SetActive(false);

		if (PokerGameController.instance != null)
		{
			PokerGameController.instance.SetMatchResultPanel(false);
		}
	}

	private void StopCountdown()
	{
		if (countdownCoroutine != null)
		{
			StopCoroutine(countdownCoroutine);
			countdownCoroutine = null;
		}
	}

	private void OnDestroy()
	{
		acceptButton.onClick.RemoveListener(AcceptRematch);
		declineButton.onClick.RemoveListener(DeclineRematch);
	}
}
