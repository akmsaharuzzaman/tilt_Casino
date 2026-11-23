
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker
{
	public class PokerUIController : MonoBehaviour
	{
		public static PokerUIController Instance { get; private set; }

		[Header("Action Panel")]
		[SerializeField] private GameObject actionPanel;

		[Header("Pre Define")]
		public PokerCheckFoldUI actionFoldUI;
		[SerializeField] private bool _autoCheckAction;
		public bool autoCheckAction => _autoCheckAction;
		[SerializeField] private GameObject autoCheckActionMark;
		[SerializeField] private bool _autoFoldAction;
		public bool autoFoldAction => _autoFoldAction;
		[SerializeField] private GameObject autoFoldActionMark;

		[Header("Action Buttons")]
		[SerializeField] private Button checkButton;
		[SerializeField] private Button callButton;
		[SerializeField] private Button foldButton;
		[SerializeField] private Button betButton;
		[SerializeField] private Button raiseButton;
		[SerializeField] private Button allInButton;

		[Header("Action Amount Texts")]
		[SerializeField] private TextMeshProUGUI callAmountText;
		[SerializeField] private TextMeshProUGUI betAmountText;

		[Header("Raise Popup")]
		[SerializeField] private RaisePopupUI raisePopupUI;

		private bool isMyTurn;

		private long callAmount;
		private long minBetAmount;
		private long maxBetAmount;
		private long minRaiseAmount;
		private long minRaiseTo;
		private long maxRaiseTo;
		private long allInAmount;

		private long tablePot;
		private long tableCurrentBet;
		private long playerCurrentBet;
		private long playerStack;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			HideActionPanel();
		}

		private void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		public void AutoCheckAction()
		{
			_autoCheckAction = _autoCheckAction ? false : true;
			autoCheckActionMark.SetActive(_autoCheckAction);

			_autoFoldAction = false;
			autoFoldActionMark.SetActive(_autoFoldAction);

			actionFoldUI.SetCheckFold(false);
		}
		public void AutoFoldAction()
		{
			_autoFoldAction = _autoFoldAction ? false : true;
			autoFoldActionMark.SetActive(_autoFoldAction);

			_autoCheckAction = false;
			autoCheckActionMark.SetActive(_autoCheckAction);

			actionFoldUI.SetCheckFold(false);
		}

		public void TurnOffAutoCheckAndFoldAction()
		{
			_autoFoldAction = false;
			_autoCheckAction = false;
			autoCheckActionMark.SetActive(_autoCheckAction);
			autoFoldActionMark.SetActive(_autoFoldAction);
		}

		public void ApplyActionOptions(JObject data)
		{
			Debug.Log("[UNITY ACTION OPTIONS RECEIVED] " + data.ToString());

			if (data == null)
				return;

			JObject actions = data["actions"] as JObject;

			if (actions == null)
				return;

			isMyTurn = data["playerTurn"]?.Value<bool>() ?? true;

			tablePot = GetLong(data, "pot");
			tableCurrentBet = GetLong(data, "currentBet");
			callAmount = GetLong(data, "callAmount");
			//playerStack = GetLong(data, "stack", "playerStack");

			playerStack = GetLong(data, "myStack");
			playerCurrentBet = GetLong(data, "myBet");

			//playerCurrentBet = GetLong(data, "myCurrentBet");

			if (data["myCurrentBet"] == null)
				playerCurrentBet = Mathf.Max(
					0,
					(int)(tableCurrentBet - callAmount)
				);

			JObject check = actions["check"] as JObject;
			JObject call = actions["call"] as JObject;
			JObject fold = actions["fold"] as JObject;
			JObject bet = actions["bet"] as JObject;
			JObject raise = actions["raise"] as JObject;
			JObject allIn = actions["allIn"] as JObject;

			bool canCheck = IsEnabled(check);
			bool canCall = IsEnabled(call);
			bool canFold = IsEnabled(fold);
			bool canBet = IsEnabled(bet);
			bool canRaise = IsEnabled(raise);
			bool canAllIn = IsEnabled(allIn);

			if (call != null)
				callAmount = GetLong(call, "amount");

			if (bet != null)
			{
				minBetAmount = GetLong(bet, "minAmount");
				maxBetAmount = GetLong(bet, "maxAmount");
			}

			if (raise != null)
			{
				minRaiseAmount = GetLong(raise, "minAmount");
				maxRaiseTo = GetLong(raise, "maxAmount");
			}

			if (data["minRaiseTo"] != null)
				minRaiseTo = GetLong(data, "minRaiseTo");
			else if (raise != null && raise["minRaiseTo"] != null)
				minRaiseTo = GetLong(raise, "minRaiseTo");
			else
				minRaiseTo = tableCurrentBet + minRaiseAmount;

			if (allIn != null)
				allInAmount = GetLong(allIn, "amount");

			UpdateAmountTexts();

			Debug.Log("[UNITY TURN CHECK] " +
	"IsMyTurn: " + isMyTurn +
	" | Raise: " + canRaise +
	" | Stack: " + playerStack +
	" | Call: " + callAmount);

			if (!isMyTurn)
			{
				HideActionPanel();
				return;
			}

			ShowActionPanel();

			SetButton(checkButton, canCheck);
			SetButton(callButton, canCall);
			SetButton(foldButton, canFold);
			SetButton(betButton, canBet);
			SetButton(raiseButton, canRaise);
			SetButton(allInButton, canAllIn);
		}

		private void UpdateAmountTexts()
		{
			if (callAmountText != null)
				callAmountText.text = callAmount.ToString();

			if (betAmountText != null)
				betAmountText.text = minBetAmount.ToString();

			if (raisePopupUI.raiseAmountText != null)
			{
				raisePopupUI.raiseAmountText.text = "$" + minRaiseTo.ToString("N0");
			}
		}

		public void CheckAction()
		{
			if (!isMyTurn)
				return;

			PokerActionManager.Instance?.Check();
			FinishAction();
		}

		public void CallAction()
		{
			if (!isMyTurn)
				return;

			PokerActionManager.Instance?.Call();
			FinishAction();
		}

		public void FoldAction()
		{
			if (!isMyTurn)
				return;

			PokerActionManager.Instance?.Fold();
			FinishAction();
		}

		public void BetAction()
		{
			if (!isMyTurn)
				return;

			if (minBetAmount <= 0)
			{
				Debug.LogWarning("[POKER UI] Invalid minimum bet amount.");
				return;
			}

			if (maxBetAmount > 0 && minBetAmount > maxBetAmount)
			{
				Debug.LogWarning("[POKER UI] Minimum bet exceeds maximum bet.");
				return;
			}

			if (minBetAmount > int.MaxValue)
			{
				Debug.LogWarning("[POKER UI] Bet amount exceeds supported integer range.");
				return;
			}

			Debug.Log($"[POKER UI] BET: {minBetAmount}");

			PokerActionManager.Instance?.Bet((int)minBetAmount);
			FinishAction();
		}

		public void OpenRaisePopup()
		{
			if (!isMyTurn)
				return;

			if (raisePopupUI == null)
			{
				Debug.LogWarning("[POKER UI] RaisePopupUI reference is missing.");
				return;
			}

			if (maxRaiseTo <= 0 || minRaiseTo <= 0)
			{
				Debug.LogWarning("[POKER UI] Invalid raise amount.");
				return;
			}

			raisePopupUI.Open(
				tablePot,
				playerStack,
				tableCurrentBet,
				playerCurrentBet,
				minRaiseTo
			);
		}

		public void AllInAction()
		{
			if (!isMyTurn)
				return;

			PokerActionManager.Instance?.AllIn();
			FinishAction();
		}

		public void FinishAction()
		{
			isMyTurn = false;
			HideActionPanel();
			_autoCheckAction = false;
			_autoFoldAction = false;
			autoCheckActionMark.SetActive(_autoCheckAction);
			autoFoldActionMark.SetActive(_autoFoldAction);
		}

		public void ShowActionPanel()
		{
			if (actionPanel != null)
				actionPanel.SetActive(true);
		}

		public void HideActionPanel()
		{
			if (actionPanel != null)
				actionPanel.SetActive(false);

			SetButton(checkButton, false);
			SetButton(callButton, false);
			SetButton(foldButton, false);
			SetButton(betButton, false);
			SetButton(raiseButton, false);
			SetButton(allInButton, false);
		}

		private void SetButton(Button button, bool enabled)
		{
			if (button != null)
			{
				button.interactable = enabled;
				button.gameObject.SetActive(enabled);
			}
		}

		private bool IsEnabled(JObject action)
		{
			return action?["enabled"]?.Value<bool>() ?? false;
		}

		private long GetLong(JObject data, params string[] keys)
		{
			if (data == null)
				return 0;

			foreach (string key in keys)
			{
				JToken value = data[key];

				if (value != null && value.Type != JTokenType.Null)
				{
					if (long.TryParse(value.ToString(), out long result))
						return result;
				}
			}

			return 0;
		}
	}
}
