
using System;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Poker
{
	public class PokerTableUI : MonoBehaviour
	{
		[SerializeField] private CommunityCardController communityCardController;
		[SerializeField] private PersonalHandRankingController personalHandRankingController;
		[SerializeField] private RaisePopupUI raisePopupUI;

		[Header("Player Seats")]
		[SerializeField] private PokerSeatView[] seatViews = new PokerSeatView[5];

		private readonly string[] playerIdsByVisualSlot = new string[5];
		public PokerSeatView[] GetSeatViews()
		{
			return seatViews;
		}

		[Header("Table Information")]
		[SerializeField] private TextMeshProUGUI potText;
		[SerializeField] private TextMeshProUGUI streetText;
		[SerializeField] private TextMeshProUGUI handNumberText;

		[Header("Waiting List")]
		[SerializeField] private GameObject waitingPanel;
		[SerializeField] private TextMeshProUGUI waitingListText;

		private const string PLAYER_ACTION = "SGDSOFT@PLAYER_ACTION";
		private const string HAND_STARTED = "SGDSOFT@HAND_STARTED";
		private const string TABLE_STATE = "SGDSOFT@TABLE_STATE";

		private string myPlayerId = "";
		private int mySeatIndex = -1;
		private string currentStreet = "";

		[Header("Start Match and Waiting for Player")]
		public GameObject waitingForOtherPlayerPanel;
		public TextMeshProUGUI waitingForPlayersCountDownText;
		public GameObject startMatchPanel;
		public TextMeshProUGUI startMatchCountDownText;
		private bool firstTimeShowStartMatchCountDown;

		private Coroutine waitingCountdownCoroutine;
		private Coroutine startCountdownCoroutine;

		[Header("Poker Ranking UI")]
		//[SerializeField] private GameObject personalRankingParent;
		//[SerializeField] private GameObject communityRankingParent;

		//[SerializeField] private TMP_Text personalRankingText;
		//[SerializeField] private TMP_Text communityRankingText;

		private const string BETTING_ROUND_UPDATED = "SGDSOFT@BETTING_ROUND_UPDATED";


		private long tablePot;
		private long myStack;
		private long tableCurrentBet;
		private long myCurrentBet;
		private long minRaiseTo;

		// ---------------------------------
		// EVENTS
		// ---------------------------------

		private void Start()
		{
			firstTimeShowStartMatchCountDown = true;
			if (Server.Instance == null)
			{
				Debug.LogError("[POKER UI] Server instance not found.");
				return;
			}

			Server.Instance.RegisterEvent(
				PLAYER_ACTION,
				HandlePlayerAction
			);

			Server.Instance.RegisterEvent(
				HAND_STARTED,
				HandleHandStarted
			);

			Server.Instance.RegisterEvent(
	BETTING_ROUND_UPDATED,
	HandleBettingRoundUpdated
);

			if (raisePopupUI != null)
				raisePopupUI.OnActionConfirmed += HandleRaiseActionConfirmed;

			if (communityCardController != null)
			{
				communityCardController.OnCommunityRankingUpdated += UpdateCommunityRanking;
			}
			if (personalHandRankingController != null)
			{
				personalHandRankingController.OnPersonalRankingUpdated += UpdatePersonalRanking;
			}

			Server.Instance.RegisterEvent(TABLE_STATE, HandleTableState);
		}

		private void OnDestroy()
		{
			if (Server.Instance == null)
				return;

			Server.Instance.UnregisterEvent(
				PLAYER_ACTION,
				HandlePlayerAction
			);

			Server.Instance.UnregisterEvent(
				HAND_STARTED,
				HandleHandStarted
			);

			Server.Instance.UnregisterEvent(
	BETTING_ROUND_UPDATED,
	HandleBettingRoundUpdated
);

			if (raisePopupUI != null)
				raisePopupUI.OnActionConfirmed -= HandleRaiseActionConfirmed;

			if (communityCardController != null)
			{
				communityCardController.OnCommunityRankingUpdated -= UpdateCommunityRanking;
			}
			if (personalHandRankingController != null)
			{
				personalHandRankingController.OnPersonalRankingUpdated -= UpdatePersonalRanking;
			}
			Server.Instance.UnregisterEvent(TABLE_STATE, HandleTableState);
		}

		// ---------------------------------
		// PLAYER ACTION DISPLAY
		// ---------------------------------

		private void HandleTableState(JObject data)
		{
			if (data == null)
				return;

			JObject table = data["table"] as JObject ?? data;

			ApplySnapshot(table);
		}

		private void HandlePlayerAction(JObject data)
		{
			if (data == null)
				return;

			// Always apply the latest server state first.
			JObject table = data["table"] as JObject;

			if (table != null)
			{
				JArray seats = table["seats"] as JArray;

				if (seats != null)
				{
					foreach (JToken token in seats)
					{
						JObject player = token as JObject;

						if (player == null)
							continue;

						Debug.Log(
							$"[SERVER SNAPSHOT] " +
							$"Player: {player["userName"]?.ToString()} | " +
							$"ID: {player["id"]?.ToString()} | " +
							$"CurrentBet: {player["currentBet"]?.ToString()} | " +
							$"Stack: {player["stack"]?.ToString()}"
						);
					}
				}

				ApplySnapshot(table);
			}
			else
			{
				Debug.LogError("[SERVER SNAPSHOT] Table object missing!");
			}

			string playerId = GetString(data, "playerId");
			string action = GetString(data, "action");

			if (string.IsNullOrEmpty(playerId) ||
				string.IsNullOrEmpty(action))
				return;

			long amount = 0;

			long.TryParse(
				GetString(data, "amount", "0"),
				NumberStyles.Integer,
				CultureInfo.InvariantCulture,
				out amount
			);

			for (int i = 0; i < playerIdsByVisualSlot.Length; i++)
			{
				if (playerIdsByVisualSlot[i] != playerId)
					continue;

				PokerSeatView view = seatViews[i];

				if (view != null)
				{
					view.ShowPlayerAction(action, amount);

					Debug.Log(
						$"[POKER UI] {playerId} | {action} | " +
						$"Action Amount: {amount} | Visual Slot: {i}"
					);
				}

				return;
			}

			Debug.Log(
				$"[POKER UI] Snapshot updated. Action player has no local visual seat: {playerId}"
			);
		}
		private void HandlePlayerActionClear()
		{
			if (seatViews == null)
				return;

			foreach (PokerSeatView seat in seatViews)
			{
				if (seat != null)
					seat.HidePlayerAction();
			}
		}

		private void HandleHandStarted(JObject data)
		{
			currentStreet = "";

			HandlePlayerActionClear();

			StopAllSeatTimers();

			Debug.Log("[POKER UI] New hand started. Action UI cleared.");
		}

		// ---------------------------------
		// LOCAL PLAYER
		// ---------------------------------

		public void SetMyPlayer(string playerId, int seatIndex)
		{
			myPlayerId = playerId ?? "";
			mySeatIndex = seatIndex;

			Debug.Log(
				$"[POKER UI] My ID: {myPlayerId}, My Seat: {mySeatIndex}"
			);
		}

		// ---------------------------------
		// APPLY SERVER TABLE SNAPSHOT
		// ---------------------------------

		public void ApplySnapshot(JObject table)
		{
			if (table == null)
			{
				Debug.LogWarning("[POKER UI] Table data is null.");
				return;
			}

			if (seatViews == null || seatViews.Length != 5)
			{
				Debug.LogError(
					"[POKER UI] Assign exactly 5 PokerSeatView references."
				);
				return;
			}

			JArray seats = table["seats"] as JArray;

			if (seats == null || seats.Count != 5)
			{
				Debug.LogError(
					"[POKER UI] Server must provide exactly 5 seats."
				);
				return;
			}

			// Find local player's server seat.
			if (mySeatIndex < 0 && !string.IsNullOrEmpty(myPlayerId))
			{
				for (int i = 0; i < seats.Count; i++)
				{
					JObject player = seats[i] as JObject;

					if (player == null)
						continue;

					if (GetString(player, "id") == myPlayerId)
					{
						mySeatIndex = i;
						break;
					}
				}
			}

			// Clear previous street actions.
			string newStreet = GetString(table, "street", "WAITING");

			if (!string.IsNullOrEmpty(currentStreet) &&
				currentStreet != newStreet)
			{
				HandlePlayerActionClear();

				Debug.Log(
					$"[POKER UI] Street changed: {currentStreet} -> {newStreet}"
				);
			}

			currentStreet = newStreet;

			int dealerSeat = GetInt(table, "dealerSeat", -1);
			int smallBlindSeat = GetInt(table, "smallBlindSeat", -1);
			int bigBlindSeat = GetInt(table, "bigBlindSeat", -1);

			UpdateRaiseData(table, seats);

			RenderSeats(
				seats,
				dealerSeat,
				smallBlindSeat,
				bigBlindSeat
			);

			UpdateTableInfo(table);

			UpdateWaitingList(
				table["waitingPlayers"] as JArray
			);
		}

		// ---------------------------------
		// RENDER SEATS
		// ---------------------------------

		private void RenderSeats(
			JArray seats,
			int dealerSeat,
			int smallBlindSeat,
			int bigBlindSeat)
		{
			for (int serverSeat = 0; serverSeat < 5; serverSeat++)
			{
				// Local player appears at visual slot 0.
				int visualSlot = mySeatIndex >= 0
					? (serverSeat - mySeatIndex + 2 + 5) % 5
					: serverSeat;

				PokerSeatView view = seatViews[visualSlot];

				if (view == null)
				{
					Debug.LogError(
						$"[POKER UI] Seat view missing at slot {visualSlot}"
					);
					continue;
				}

				view.gameObject.SetActive(true);

				JObject player = seats[serverSeat] as JObject;

				if (player == null)
				{
					playerIdsByVisualSlot[visualSlot] = "";

					view.SetEmpty();
					continue;
				}

				string playerId = GetString(player, "id");

				playerIdsByVisualSlot[visualSlot] = playerId;

				string playerName = GetString(
					player,
					"userName",
					"Player"
				);

				string stack = FormatStack(
					GetString(player, "stack", "0")
				);

				bool isMe =
					!string.IsNullOrEmpty(myPlayerId) &&
					playerId == myPlayerId;

				bool connected = GetBool(player, "connected", true);
				bool folded = GetBool(player, "folded", false);
				bool allIn = GetBool(player, "allIn", false);
				bool isPlaying = GetBool(player, "isPlaying", false);

				string status = GetPlayerStatus(
					connected,
					folded,
					allIn,
					isPlaying,
					GetString(player, "lastAction")
				);

				string currentBet = FormatStack(
	GetString(player, "currentBet", "0")
);


				string totalContribution = FormatStack(
	GetString(player, "totalContribution", "0")
);

				Debug.Log(
	$"[UI RENDER] {playerName} | " +
	$"Round Bet: {currentBet} | " +
	$"Total Bet: {totalContribution}"
);

				view.SetPlayer(
					playerId,
					playerName,
					stack,
					currentBet,
					totalContribution,
					isMe,
					serverSeat == dealerSeat,
					serverSeat == smallBlindSeat,
					serverSeat == bigBlindSeat,
					status
				);
			}
		}

		// ---------------------------------
		// HOLE CARD SYSTEM
		// ---------------------------------

		public void ResetAllHoleCards()
		{
			if (seatViews == null)
				return;

			foreach (PokerSeatView seat in seatViews)
			{
				if (seat != null)
					seat.ResetHoleCards();
			}

			Debug.Log("[POKER UI] All hole cards reset.");
		}

		public void RestartPlayCards()
		{
			foreach (PokerSeatView seat in seatViews)
			{
				if (seat != null)
					seat.ResetCards();
			}
		}

		public void ShowMyHoleCards(JArray cards)
		{
			if (string.IsNullOrEmpty(myPlayerId))
			{
				Debug.LogWarning("[POKER UI] My Player ID is missing.");
				return;
			}

			if (cards == null || cards.Count != 2)
			{
				Debug.LogWarning("[POKER UI] Invalid private hole cards.");
				return;
			}

			for (int i = 0; i < 5; i++)
			{
				if (playerIdsByVisualSlot[i] != myPlayerId)
					continue;

				PokerSeatView myView = seatViews[i];

				if (myView != null)
				{
					myView.ShowHoleCards(cards,true);

					Debug.Log("[POKER UI] My hole cards displayed.");
				}

				return;
			}

			Debug.LogWarning("[POKER UI] My seat view not found.");
		}

		public void ShowPlayerHoleCards(string playerId, JArray cards)
		{
			if (string.IsNullOrEmpty(playerId))
				return;

			if (cards == null || cards.Count != 2)
				return;

			for (int i = 0; i < playerIdsByVisualSlot.Length; i++)
			{
				if (playerIdsByVisualSlot[i] != playerId)
					continue;

				PokerSeatView view = seatViews[i];

				if (view != null)
				{
					view.ShowHoleCards(cards,false);

					Debug.Log(
						$"[POKER UI] Revealed cards for player: {playerId}"
					);
				}

				return;
			}

			Debug.LogWarning(
				$"[POKER UI] Player seat not found: {playerId}"
			);
		}

		// ---------------------------------
		// PLAYER STATUS
		// ---------------------------------

		private string GetPlayerStatus(
			bool connected,
			bool folded,
			bool allIn,
			bool isPlaying,
			string lastAction)
		{
			if (!connected)
				return "Disconnected";

			if (folded)
				return "Folded";

			if (allIn)
				return "All-In";

			if (!string.IsNullOrEmpty(lastAction))
				return lastAction.ToUpperInvariant();

			return isPlaying ? "Playing" : "Seated";
		}

		// ---------------------------------
		// TABLE INFORMATION
		// ---------------------------------

		private void UpdateTableInfo(JObject table)
		{
			if (potText != null)
			{
				potText.text = "Pot: " +
					FormatStack(GetString(table, "pot", "0"));
			}

			if (streetText != null)
			{
				streetText.text = GetString(
					table,
					"street",
					"WAITING"
				);
			}

			if (handNumberText != null)
			{
				handNumberText.text = "Hand: " +
					GetString(table, "handNumber", "0");
			}
		}

		// ---------------------------------
		// WAITING PLAYERS
		// ---------------------------------

		private void UpdateWaitingList(JArray waitingPlayers)
		{
			if (waitingPanel == null || waitingListText == null)
				return;

			if (waitingPlayers == null || waitingPlayers.Count == 0)
			{
				waitingPanel.SetActive(false);
				waitingListText.text = "";
				return;
			}

			waitingPanel.SetActive(true);

			waitingListText.text = "WAITING FOR NEXT HAND";

			for (int i = 0; i < waitingPlayers.Count; i++)
			{
				JObject player = waitingPlayers[i] as JObject;

				if (player == null)
					continue;

				string playerName = GetString(
					player,
					"userName",
					"Player"
				);

				waitingListText.text +=
					$"\n{i + 1}. {playerName}";
			}
		}

		// ---------------------------------
		// JSON HELPERS
		// ---------------------------------

		private string GetString(
			JObject data,
			string key,
			string fallback = "")
		{
			return data?[key]?.ToString() ?? fallback;
		}

		private int GetInt(
			JObject data,
			string key,
			int fallback)
		{
			if (data?[key] == null)
				return fallback;

			return int.TryParse(
				GetString(data, key),
				NumberStyles.Integer,
				CultureInfo.InvariantCulture,
				out int value
			) ? value : fallback;
		}

		private bool GetBool(
			JObject data,
			string key,
			bool fallback)
		{
			if (data?[key] == null)
				return fallback;

			return bool.TryParse(
				GetString(data, key),
				out bool value
			) ? value : fallback;
		}

		private string FormatStack(string value)
		{
			if (long.TryParse(
				value,
				NumberStyles.Integer,
				CultureInfo.InvariantCulture,
				out long amount))
			{
				return amount.ToString(
					"N0",
					CultureInfo.InvariantCulture
				);
			}

			return value;
		}

		// ---------------------------------
		// TURN TIMER
		// ---------------------------------

		public void StartSeatTimer(int serverSeatIndex, float duration)
		{
			if (seatViews == null || seatViews.Length != 5)
				return;

			if (serverSeatIndex < 0 || serverSeatIndex >= 5)
				return;

			int visualSlot = mySeatIndex >= 0
				? (serverSeatIndex - mySeatIndex + 2 + 5) % 5
				: serverSeatIndex;

			if (visualSlot < 0 || visualSlot >= seatViews.Length)
				return;

			if (seatViews[visualSlot] != null)
			{
				seatViews[visualSlot].StartTurnTimer(duration);
			}
		}

		public void StopAllSeatTimers()
		{
			if (seatViews == null)
				return;

			foreach (PokerSeatView seat in seatViews)
			{
				if (seat != null)
					seat.StopTurnTimer();
			}
		}

		private void HandleBettingRoundUpdated(JObject data)
		{
			if (data == null)
				return;

			JObject table = data["table"] as JObject;

			if (table != null)
			{
				ApplySnapshot(table);
			}
		}

		#region Ranking
		public void UpdatePersonalRanking(string ranking)
		{
			/*if (personalRankingParent != null)
				personalRankingParent.SetActive(true);

			if (personalRankingText != null)
				personalRankingText.text = ranking;*/
		}
		public void UpdateCommunityRanking(string ranking)
		{
			Debug.Log("[UI] Community Ranking Received: " + ranking);

			/*if (communityRankingParent != null)
				communityRankingParent.SetActive(true);*/

			/*if (communityRankingText != null)
				communityRankingText.text = ranking;*/
		}
		public void PersonalAndCommunityRankParentObjectDeActive()
		{
			/*if (personalRankingParent != null)
				personalRankingParent.SetActive(false);*/

			/*if (communityRankingParent != null)
				communityRankingParent.SetActive(false);*/
		}
		#endregion

		private void UpdateRaiseData(JObject table, JArray seats)
		{
			tablePot = GetLong(table, "pot", 0);

			tableCurrentBet = GetLong(table, "currentBet", 0);

			myStack = 0;
			myCurrentBet = 0;

			foreach (JToken token in seats)
			{
				JObject player = token as JObject;

				if (player == null)
					continue;

				if (GetString(player, "id") != myPlayerId)
					continue;

				myStack = GetLong(player, "stack", 0);
				myCurrentBet = GetLong(player, "currentBet", 0);

				break;
			}

			minRaiseTo = GetLong(
				table,
				"minRaiseTo",
				tableCurrentBet + GetLong(table, "minRaise", 0)
			);
		}

		private long GetLong(JObject data, string key, long fallback = 0)
		{
			if (data?[key] == null)
				return fallback;

			return long.TryParse(
				data[key].ToString(),
				NumberStyles.Integer,
				CultureInfo.InvariantCulture,
				out long value
			) ? value : fallback;
		}

		public void OpenRaisePopup()
		{
			if (raisePopupUI == null)
				return;

			raisePopupUI.Open(
				tablePot,
				myStack,
				tableCurrentBet,
				myCurrentBet,
				minRaiseTo
			);
		}
		private void OnDisable()
		{
			if (raisePopupUI != null)
				raisePopupUI.OnActionConfirmed -= HandleRaiseActionConfirmed;
		}

		private void HandleRaiseActionConfirmed(string action, long amount)
		{
			if (PokerActionManager.Instance == null)
			{
				Debug.LogError("[POKER UI] PokerActionManager not found.");
				return;
			}

			switch (action.ToUpperInvariant())
			{
				case "RAISE":
					PokerActionManager.Instance.RaiseTo(amount);
					break;

				case "ALL_IN":
					PokerActionManager.Instance.AllIn();
					break;

				default:
					Debug.LogWarning("[POKER UI] Unsupported popup action: " + action);
					break;
			}
		}



	

		public void ShowWaitingForOtherPlayer(int seconds)
		{
			if (waitingCountdownCoroutine != null)
				StopCoroutine(waitingCountdownCoroutine);

			waitingForOtherPlayerPanel.SetActive(true);
			startMatchPanel.SetActive(false);

			waitingCountdownCoroutine = StartCoroutine(
				CountdownRoutine(seconds, waitingForPlayersCountDownText)
			);
		}

		public void ShowStartMatchCountdown(int seconds)
		{
			if (firstTimeShowStartMatchCountDown == true)
			{
				firstTimeShowStartMatchCountDown = false;
				if (startCountdownCoroutine != null)
				{
					StopCoroutine(startCountdownCoroutine);
				}
				waitingForOtherPlayerPanel.SetActive(false);
				startMatchPanel.SetActive(true);

				startCountdownCoroutine = StartCoroutine(
					CountdownRoutine(seconds, startMatchCountDownText)
				);
			}
		}

		private IEnumerator CountdownRoutine(
			int seconds,
			TextMeshProUGUI countdownText)
		{
			for (int remaining = seconds; remaining >= 0; remaining--)
			{
				if (countdownText != null)
					countdownText.text = remaining.ToString();

				if (remaining == 0)
					break;

				yield return new WaitForSecondsRealtime(1f);
			}
		}

		public void HideMatchCountdownPanels()
		{
			if (waitingCountdownCoroutine != null)
			{
				StopCoroutine(waitingCountdownCoroutine);
				waitingCountdownCoroutine = null;
			}

			if (startCountdownCoroutine != null)
			{
				StopCoroutine(startCountdownCoroutine);
				startCountdownCoroutine = null;
			}

			waitingForOtherPlayerPanel.SetActive(false);
			startMatchPanel.SetActive(false);
		}


	}
}
