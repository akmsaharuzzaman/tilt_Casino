
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.InputSystem.InputControlScheme;

namespace Poker
{
	public partial class PokerGameController : MonoBehaviour
	{
		public static PokerGameController instance { get; set; }

		[Header("Player Information")]
		[SerializeField] private string userName = "Stringer";
		[SerializeField] private string avatarId = "avatar-1";
		[SerializeField] private string email = "Stringer@gmail.com";
		[SerializeField] private int amount = 1000;

		[Header("UI")]
		public GameObject bottomButtonActivityPanel;
		public GameObject bottomButtonActivityPanel_NotMyTurn;
		public GameObject selectionPanel;

		[SerializeField] private PokerTableUI pokerTableUI;

		[Header("Disconnect / Timeout UI")]
		[SerializeField] private GameObject disconnectPanel;
		[SerializeField] private TMPro.TMP_Text disconnectTitle;
		[SerializeField] private TMPro.TMP_Text disconnectMessage;

		private bool authenticationRequested;
		private bool matchmakingRequested;

		[Header("Match Result UI")]
		[SerializeField] private GameObject matchResultPanel;
		public void SetMatchResultPanel(bool active) { matchResultPanel.SetActive(active); }
		[SerializeField] private TMPro.TMP_Text matchResultTitle;
		[SerializeField] private TMPro.TMP_Text matchResultTitle2;
		[SerializeField] private TMPro.TMP_Text winnerNameText;
		[SerializeField] private TMPro.TMP_Text payoutText;
		[SerializeField] private TMPro.TMP_Text finalStackText;
		[SerializeField] private TMPro.TMP_Text walletBalanceText;
		[SerializeField] private TMPro.TMP_Text totalPotText;

		[Header("Insufficient Balance Popup")]
		[SerializeField] private GameObject insufficientBalancePanel;
		[SerializeField] private TMPro.TMP_Text insufficientBalanceTitle;
		[SerializeField] private TMPro.TMP_Text requiredAmountText;
		[SerializeField] private TMPro.TMP_Text walletBalanceText2;
		[SerializeField] private TMPro.TMP_Text shortageText;

		[Header("Dealer Take Tips")]
		Coroutine co_dealerTip;
		[SerializeField] private GameObject dealerTip;
		[SerializeField] private TMPro.TMP_Text adminRakeText;
		[SerializeField] private TMPro.TMP_Text netPotText;

		private Coroutine resultCoroutine;

		public CommunityCardController communityCardController;

		private void Awake()
		{
			instance = this;
		}

		private void Start()
		{
			if (Server.Instance == null)
			{
				Debug.LogError("[POKER] Server not found!");
				return;
			}

			Server.Instance.TransportConnected += OnTransportConnected;

			RegisterPokerEvents();

			/*if (Server.Instance.IsConnected)
				OnTransportConnected();*/
		}

		private void RegisterPokerEvents()
		{
			Server.Instance.RegisterEvent(
				"SGDSOFT@Connected",
				OnAuthenticationSuccess
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@ROOM_CREATED",
				OnRoomCreated
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@ROOM_JOINED",
				OnRoomJoined
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@WAITING",
				OnWaiting
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@PLAYER_JOINED",
				OnPlayerJoined
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@PLAYER_LEFT",
				OnPlayerLeft
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@START",
				OnPokerStart
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@TABLE_STATE",
				OnTableState
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@ERROR",
				OnServerError
			);

			Server.Instance.RegisterEvent(
				"SGDSOFT@PLAYER_TIMEOUT",
				OnPlayerTimeout
			);

			Server.Instance.RegisterEvent(
	"SGDSOFT@PLAYER_TURN",
	OnPlayerTurn
);

			Server.Instance.RegisterEvent(
	"SGDSOFT@POKER_ACTION_OPTIONS",
	OnPokerActionOptions
);

			Server.Instance.RegisterEvent(
	"SGDSOFT@MATCH_RESULT",
	OnMatchResult
);

			Server.Instance.RegisterEvent(
				"SGDSOFT@DEAL_HOLE_CARDS",
				OnDealHoleCards
			);

			Server.Instance.RegisterEvent(
	"SGDSOFT@DEBUG_ALL_HOLE_CARDS",
	OnDebugAllHoleCards
);

			Server.Instance.RegisterEvent(
				"SGDSOFT@SHOWDOWN_RESULT",
				OnShowdownResult
			);

			Server.Instance.RegisterEvent(
	"SGDSOFT@BETTING_ROUND_FINISHED",
	OnBettingRoundFinished
);

			Server.Instance.RegisterEvent(
	"SGDSOFT@BETTING_ROUND_UPDATED",
	OnBettingRoundUpdated
);

			Server.Instance.RegisterEvent(
	"SGDSOFT@COUNTDOWN",
	OnCountdown
);

			Server.Instance.RegisterEvent(
	"SGDSOFT@EMPTY_ROOM_COUNTDOWN",
	OnEmptyRoomCountdown
);

		}

		public void ResetBoard()
		{
			if (pokerTableUI != null)
			{
				PokerSeatView[] seats = pokerTableUI.GetSeatViews();

				for (int i = 0; i < seats.Length; i++)
				{
					if (seats[i] == null)
						continue;

					seats[i].ResetHoleCards();
					seats[i].HidePublicRanking();
				}
			}

			if (communityCardController != null)
				communityCardController.ResetCommunityCards();
		}

		private PokerSeatView GetMySeatView()
		{
			foreach (PokerSeatView seat in pokerTableUI.GetSeatViews())
			{
				if (seat == null)
					continue;

				// এখানে নিজের player ID-এর সঙ্গে মিলাতে হবে
				if (seat.PlayerId == Server.Instance.PlayerId)
					return seat;
			}

			return null;
		}

		private PokerSeatView GetSeatViewByPlayerId(string playerId)
		{
			foreach (PokerSeatView seat in pokerTableUI.GetSeatViews())
			{
				if (seat == null)
					continue;

				if (seat.PlayerId == playerId)
					return seat;
			}

			return null;
		}

		public void OnTransportConnected()
		{
			/*if (authenticationRequested)
				return;

			authenticationRequested = true;

			Server.Instance.Authenticate(
				userName,
				avatarId,
				email,
				amount
			);*/
		}
		public void JoinRoom(string _name, string _email, int _amount, int _sb, int _bb)
		{
			if (authenticationRequested)
				return;


			selectionPanel.SetActive(false);
			authenticationRequested = true;

			JSONObject details = new JSONObject();
			details.AddField("smallBlind", _sb);
			details.AddField("bigBlind", _bb);

			Server.Instance.Authenticate(
				_name,
				avatarId,
				_email,
				_amount,
				details
			);
		}

		private void OnAuthenticationSuccess(JObject data)
		{
			if (!Server.Instance.IsAuthenticated)
			{
				Debug.LogError("[POKER] Authentication failed.");
				return;
			}

			Debug.Log("[POKER] Authentication successful.");

			if (matchmakingRequested)
				return;

			matchmakingRequested = true;

			Server.Instance.JoinMatchmaking();
		}

		private void OnRoomCreated(JObject data)
		{
			Debug.Log("[POKER] Room created.");

			ApplyRoomSnapshot(data);
			pokerTableUI.ShowWaitingForOtherPlayer(10);
		}

		private void OnRoomJoined(JObject data)
		{
			Debug.Log("[POKER] Room joined.");

			ApplyRoomSnapshot(data);
		}

		private void OnWaiting(JObject data)
		{
			Debug.Log("[POKER] Added to waiting list.");

			JObject table = data["table"] as JObject;

			if (table != null && pokerTableUI != null)
				pokerTableUI.ApplySnapshot(table);

			// Waiting player has no seat yet.
			// Do not assign a visual seat here.
		}

		private void OnPlayerJoined(JObject data)
		{
			string status = data["status"]?.ToString();

			Debug.Log("[POKER] Player joined: " + status);

			// Promotion event contains a fresh table snapshot.
			JObject table = data["table"] as JObject;

			if (table != null && pokerTableUI != null)
				pokerTableUI.ApplySnapshot(table);
		}

		private void OnPlayerLeft(JObject data)
		{
			Debug.Log("[POKER] Player left.");

			JObject table = data["table"] as JObject;

			if (table != null && pokerTableUI != null)
				pokerTableUI.ApplySnapshot(table);
		}

		private void OnPokerStart(JObject data)
		{
			pokerTableUI.HideMatchCountdownPanels();

			Debug.Log("[POKER] Hand started.");

			JObject table = data["table"] as JObject;

			if (table != null && pokerTableUI != null)
			{
				pokerTableUI.ApplySnapshot(table);
			}
		}

		private void OnTableState(JObject data)
		{
			if (pokerTableUI != null)
				pokerTableUI.ApplySnapshot(data);
		}

		private void OnServerError(JObject data)
		{
			string code = data["code"]?.ToString();
			string message = data["message"]?.ToString();

			Debug.LogError($"[SERVER ERROR] {code}: {message}");

			if (code == "INSUFFICIENT_BALANCE")
			{
				long requiredAmount =
					data["requiredAmount"]?.Value<long>() ?? 0;

				long walletBalance =
					data["walletBalance"]?.Value<long>() ?? 0;

				long shortage =
					data["shortage"]?.Value<long>() ??
					Math.Max(0, requiredAmount - walletBalance);

				ShowInsufficientBalancePopup(
					requiredAmount,
					walletBalance,
					shortage
				);

				return;
			}

			Debug.LogWarning("[SERVER ERROR] " + message);
		}
		private void ShowInsufficientBalancePopup(
	long requiredAmount,
	long walletBalance,
	long shortage)
		{
			if (insufficientBalancePanel == null)
			{
				Debug.LogWarning(
					"[POKER UI] Insufficient Balance Panel is missing."
				);
				return;
			}

			insufficientBalancePanel.SetActive(true);

			if (insufficientBalanceTitle != null)
				insufficientBalanceTitle.text = "INSUFFICIENT BALANCE";

			if (requiredAmountText != null)
				requiredAmountText.text =
					"Required Buy-in: " + requiredAmount.ToString("N0");

			if (walletBalanceText2 != null)
				walletBalanceText2.text =
					"Your Balance: " + walletBalance.ToString("N0");

			if (shortageText != null)
				shortageText.text =
					"Need More: " + shortage.ToString("N0");
		}

		public void CloseInsufficientBalancePopup()
		{
			if (insufficientBalancePanel != null)
				insufficientBalancePanel.SetActive(false);
		}

		private void ApplyRoomSnapshot(JObject data)
		{
			if (pokerTableUI == null)
			{
				Debug.LogError("[POKER] PokerTableUI is not assigned.");
				return;
			}

			JObject table = data["table"] as JObject;

			if (table == null)
			{
				Debug.LogError("[POKER] Table snapshot missing.");
				return;
			}

			int seatIndex = data["seatIndex"]?.Value<int>() ?? -1;

			JArray seats = table["seats"] as JArray;

			if (seatIndex < 0 || seats == null || seatIndex >= seats.Count)
			{
				Debug.LogError("[POKER] Invalid seat index.");
				return;
			}

			JObject myPlayer = seats[seatIndex] as JObject;

			if (myPlayer == null)
			{
				Debug.LogError("[POKER] My player data not found.");
				return;
			}

			string myPlayerId = myPlayer["id"]?.ToString();

			if (string.IsNullOrEmpty(myPlayerId))
			{
				Debug.LogError("[POKER] Player ID missing.");
				return;
			}

			pokerTableUI.SetMyPlayer(myPlayerId, seatIndex);
			pokerTableUI.ApplySnapshot(table);

			Debug.Log(
				$"[POKER] My seat: {seatIndex} | ID: {myPlayerId}"
			);
		}

		private void OnPlayerTimeout(JObject data)
		{
			string playerName =
	   data["player"]?["ownName"]?.ToString() ?? "Unknown";

			string playerId =
				data["player"]?["id"]?.ToString() ?? "";

			string reason =
				data["reason"]?.ToString() ?? "TIMEOUT";

			string message =
				data["message"]?.ToString() ?? "Player timed out.";

			Debug.Log($"[POKER] {playerName} ({playerId}) | {reason}");
			Debug.Log($"[POKER] {message}");
			ShowDisconnectPage(reason, message);
		}

		private void ShowDisconnectPage(string reason, string message)
		{
			if (disconnectPanel == null)
			{
				Debug.LogError("[POKER] Disconnect Panel is not assigned!");
				return;
			}

			disconnectPanel.SetActive(true);

			if (disconnectTitle != null)
			{
				disconnectTitle.text = reason == "TIMEOUT"
					? "TIME OUT"
					: "DISCONNECTED";
			}

			if (disconnectMessage != null)
			{
				disconnectMessage.text = message;
			}
		}

		private void OnPlayerTurn(JObject data)
		{
			int seatIndex = data["seatIndex"]?.Value<int>() ?? -1;
			float duration = data["duration"]?.Value<float>() ?? 0f;

			if (seatIndex < 0 || duration <= 0)
				return;

			pokerTableUI.StopAllSeatTimers();
			pokerTableUI.StartSeatTimer(seatIndex, duration);

			Debug.Log(
				$"[POKER] Seat {seatIndex} turn started: {duration}s"
			);
		}

		private void OnPokerActionOptions(JObject data)
		{
			if (PokerUIController.Instance == null)
				return;

			PokerUIController.Instance.ApplyActionOptions(data);
		}

		private void OnMatchResult(JObject data)
		{
			Debug.Log("[POKER] Match result received.");

			bool isWinner = data["isWinner"]?.Value<bool>() ?? false;

			string message = data["message"]?.ToString()
				?? (isWinner ? "Congratulations! You won the match." : "Better luck next time");

			string message2 = data["message"]?.ToString()
				?? (isWinner ? "WINNER" : "LOSSER");

			long payout = data["payout"]?.Value<long>() ?? 0;
			long finalStack = data["finalStack"]?.Value<long>() ?? 0;
			long chipsChange = data["chipsChange"]?.Value<long>() ?? 0;
			long walletBalance = data["walletBalance"]?.Value<long>() ?? 0;
			long totalPot = data["totalPot"]?.Value<long>() ?? 0;

			long rakeAmount = data["rakeAmount"]?.Value<long>() ?? 0;
			long netPot = data["netPot"]?.Value<long>() ?? 0;

			if (adminRakeText != null)
				adminRakeText.text = "Admin Rake: " + rakeAmount.ToString("N0");

			if (netPotText != null)
				netPotText.text = "Net Pot: " + netPot.ToString("N0");

			Debug.Log($"[Rake And Net Pot]: {rakeAmount.ToString("N0")} , == {netPot.ToString("N0")}");

			if(co_dealerTip != null)
			{
				StopCoroutine(co_dealerTip);
			}
			co_dealerTip = StartCoroutine(DealerTipFade());

			string winnerName = "Unknown";

			JArray winners = data["winners"] as JArray;

			System.Text.StringBuilder winnerNames = new System.Text.StringBuilder();
			System.Text.StringBuilder winnerPayouts = new System.Text.StringBuilder();

			System.Collections.Generic.Dictionary<string, long> payoutByPlayer =
				new System.Collections.Generic.Dictionary<string, long>();

			if (winners != null)
			{
				foreach (JObject winner in winners)
				{
					string playerId = winner["playerId"]?.ToString() ?? "";
					string playerName = winner["userName"]?.ToString() ?? "Unknown";
					long playerPayout = winner["payout"]?.Value<long>() ?? 0;

					if (winnerNames.Length > 0)
					{
						winnerNames.AppendLine();
						winnerPayouts.AppendLine();
					}

					winnerNames.Append(playerName);
					winnerPayouts.Append(playerName + ": " + playerPayout.ToString("N0"));

					if (!string.IsNullOrEmpty(playerId))
						payoutByPlayer[playerId] = playerPayout;
				}
			}

			Debug.Log($"[POKER] Result: {message}");
			Debug.Log($"[POKER] Winner: {winnerName}");
			Debug.Log($"[POKER] Payout: {payout}");
			Debug.Log($"[POKER] Final Stack: {finalStack}");
			Debug.Log($"[POKER] Chips Change: {chipsChange}");
			Debug.Log($"[POKER] Wallet Balance: {walletBalance}");

			if (matchResultPanel == null)
				return;

			//matchResultPanel.SetActive(true);

			if (matchResultTitle != null)
				matchResultTitle.text = message;

			if (matchResultTitle2 != null)
				matchResultTitle2.text = message2;

			if (winnerNameText != null)
				winnerNameText.text = "WINNERS:\n" + winnerNames;

			if (payoutText != null)
				payoutText.text = "PAYOUTS:\n" + winnerPayouts;

			if (finalStackText != null)
				finalStackText.text = "Final Stack: " + finalStack.ToString("N0");

			// Add a separate TMP field for Wallet Balance.
			if (walletBalanceText != null && walletBalance >= 0)
				walletBalanceText.text = "Balance: " + walletBalance.ToString("N0");

			if (totalPotText != null)
				totalPotText.text = "Total Pot: " + totalPot.ToString("N0");



			if(pokerTableUI != null)
			{				
				foreach (PokerSeatView seat in pokerTableUI.GetSeatViews())
				{
					if (seat == null || !seat.gameObject.activeSelf)
						continue;

					bool isWin = payoutByPlayer.TryGetValue(
						seat.PlayerId,
						out long playerPayout
					);

					seat.WinLossEffect(isWin, playerPayout);
				}
			}

			if (resultCoroutine != null)
				StopCoroutine(resultCoroutine);

			resultCoroutine = StartCoroutine(HideResultAfterDelay());
		}

		IEnumerator DealerTipFade()
		{
			dealerTip.SetActive(true);
			yield return new WaitForSeconds(2f);
			dealerTip.SetActive(false);
		}

		private IEnumerator HideResultAfterDelay()
		{
			yield return new WaitForSecondsRealtime(3f);

			pokerTableUI.RestartPlayCards();


			resultCoroutine = null;
		}
		private void OnDebugAllHoleCards(JObject data)
		{
			if (data == null)
				return;

			JArray players = data["players"] as JArray;

			if (players == null)
				return;

			string myPlayerId = Server.Instance.PlayerId;

			foreach (JObject player in players)
			{
				string playerId = player["playerId"]?.ToString();

				if (string.IsNullOrEmpty(playerId))
					continue;

				if (playerId == myPlayerId)
					continue;

				JArray cards = player["cards"] as JArray;

				if (cards == null || cards.Count != 2)
					continue;

				PokerSeatView seat = GetSeatViewByPlayerId(playerId);

				if (seat == null)
					continue;

				Debug.Log($"[DEBUG CARDS] Before ShowHoleCards: {playerId} | Cards: {cards.Count}");

				seat.ShowHoleCards(cards, true);

				Debug.Log($"[DEBUG CARDS] After ShowHoleCards: {playerId}");
			}
		}
		private void OnDealHoleCards(JObject data)
		{
			if (data == null)
				return;

			JArray cards = data["cards"] as JArray;

			if (cards == null || cards.Count != 2)
				return;

			string myPlayerId = Server.Instance.PlayerId;

			if (string.IsNullOrEmpty(myPlayerId))
				return;

			PokerSeatView mySeat = GetSeatViewByPlayerId(myPlayerId);

			if (mySeat == null)
				return;

			mySeat.ShowHoleCards(cards,true);

			foreach (PokerSeatView seat in pokerTableUI.GetSeatViews())
			{
				if (seat == null)
					continue;

				if (seat.PlayerPanel.activeSelf == true && seat.PlayerId != myPlayerId)
				{
					seat.OpponentShowCards();
				}					
			}

			PersonalHandRankingController.Instance?.SetHoleCards(cards);

			// Show pre-flop ranking.

			string ranking =
				PersonalHandRankingController.Instance?.GetCurrentRanking();

			if (!string.IsNullOrEmpty(ranking))
			{
				mySeat.ShowPublicRanking(ranking);

				Debug.Log(
					$"[DEAL HOLE CARDS] {myPlayerId} | Ranking: {ranking}"
				);
			}
		}
		private void OnShowdownResult(JObject data)
		{
			JArray results = data["results"] as JArray;

			if (results == null)
			{
				Debug.LogWarning("[SHOWDOWN] Results not found.");
				return;
			}

			Debug.Log("[SHOWDOWN] Public rankings received.");

			foreach (JObject player in results)
			{
				string playerId = player["playerId"]?.ToString();

				JArray holeCards = player["holeCards"] as JArray;

				string ranking =
					player["hand"]?["label"]?.ToString()
					?? player["hand"]?["name"]?.ToString();

				if (string.IsNullOrEmpty(playerId))
					continue;

				PokerSeatView seat = GetSeatViewByPlayerId(playerId);

				if (seat == null)
				{
					Debug.LogWarning(
						$"[SHOWDOWN] Seat not found: {playerId}"
					);
					continue;
				}

				// Reveal cards only when backend sends them.
				if (holeCards != null && holeCards.Count == 2)
				{
					seat.ShowHoleCards(holeCards,false);
				}

				// Show public hand ranking.
				if (!string.IsNullOrEmpty(ranking))
				{
					seat.ShowPublicRanking(ranking);

					Debug.Log(
						$"[SHOWDOWN] {playerId} | Ranking: {ranking}"
					);
				}
			}
		}

		private void OnBettingRoundFinished(JObject data)
		{
			pokerTableUI.StopAllSeatTimers();
		}

		private void OnBettingRoundUpdated(JObject data)
		{
			if (data == null)
				return;

			JObject table = data["table"] as JObject;

			if (table == null)
				return;

			pokerTableUI.ApplySnapshot(table);
		}

		private void OnCountdown(JObject data)
		{
			int seconds = data["seconds"]?.Value<int>() ?? 0;

			if (pokerTableUI != null)
				pokerTableUI.ShowStartMatchCountdown(seconds);
		}

		private void OnEmptyRoomCountdown(JObject data)
		{
			int seconds = data["seconds"]?.Value<int>() ?? 0;

			Debug.Log($"[EMPTY ROOM COUNTDOWN] {seconds}");

			if (pokerTableUI != null)
				pokerTableUI.ShowWaitingForOtherPlayer(seconds);
		}

		
		private void OnDestroy()
		{
			if (Server.Instance == null)
				return;

			Server.Instance.TransportConnected -= OnTransportConnected;

			Server.Instance.UnregisterEvent(
				"SGDSOFT@Connected", OnAuthenticationSuccess
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@ROOM_CREATED", OnRoomCreated
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@ROOM_JOINED", OnRoomJoined
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@WAITING", OnWaiting
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@PLAYER_JOINED", OnPlayerJoined
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@PLAYER_LEFT", OnPlayerLeft
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@START", OnPokerStart
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@TABLE_STATE", OnTableState
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@ERROR", OnServerError
			);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@PLAYER_TIMEOUT",
				OnPlayerTimeout
			);

			Server.Instance.UnregisterEvent(
	"SGDSOFT@PLAYER_TURN",
	OnPlayerTurn
);

			Server.Instance.UnregisterEvent(
	"SGDSOFT@POKER_ACTION_OPTIONS",
	OnPokerActionOptions
);

			Server.Instance.UnregisterEvent(
	"SGDSOFT@MATCH_RESULT",
	OnMatchResult
);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@DEAL_HOLE_CARDS",
				OnDealHoleCards
			);

			Server.Instance.UnregisterEvent(
	"SGDSOFT@DEBUG_ALL_HOLE_CARDS",
	OnDebugAllHoleCards
);

			Server.Instance.UnregisterEvent(
				"SGDSOFT@SHOWDOWN_RESULT",
				OnShowdownResult
			);

			Server.Instance.UnregisterEvent(
	"SGDSOFT@BETTING_ROUND_FINISHED",
	OnBettingRoundFinished
);

			Server.Instance.UnregisterEvent(
"SGDSOFT@BETTING_ROUND_UPDATED",
OnBettingRoundUpdated
);

			Server.Instance.UnregisterEvent(
	"SGDSOFT@COUNTDOWN",
	OnCountdown
);

			Server.Instance.UnregisterEvent(
"SGDSOFT@EMPTY_ROOM_COUNTDOWN",
OnEmptyRoomCountdown
);

		}
	}
}
