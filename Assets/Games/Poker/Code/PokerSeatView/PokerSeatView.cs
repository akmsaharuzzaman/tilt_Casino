
using DG.Tweening;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Poker
{
	public class PokerSeatView : MonoBehaviour
	{
		[Header("Panels")]
		[SerializeField] private GameObject playerPanel;
		public GameObject PlayerPanel => playerPanel;
		[SerializeField] private GameObject emptyPanel;

		[Header("Player Information")]
		[SerializeField] private TextMeshProUGUI playerNameText;
		[SerializeField] private TextMeshProUGUI stackText;
		[SerializeField] private TextMeshProUGUI statusText;

		[Header("Player Bet")]
		[SerializeField] private RectTransform targetCurrentBetPrefab;
		[SerializeField] private GameObject currentBetPrefab;
		[SerializeField] private GameObject totalContributionTextPanel;
		[SerializeField] private TextMeshProUGUI totalContributionText;

		[Header("Badges")]
		[SerializeField] private GameObject meBadge;
		[SerializeField] private GameObject dealerBadge;
		[SerializeField] private GameObject smallBlindBadge;
		[SerializeField] private GameObject bigBlindBadge;

		[Header("Hole Cards")]
		[SerializeField] private GameObject cardRoot;
		[SerializeField] private BGCStudioImage[] cardImages = new BGCStudioImage[2];

		[SerializeField] private string cardFolder = "Image/PokerSprites/Cards/";
		[SerializeField] private string cardExtension = ".png";

		private const string CARD_BACK = "Image/PokerSprites/Cards/BACK.png";

		[Header("Turn Timer")]
		[SerializeField] private GameObject ownTurn;
		[SerializeField] private GameObject turnTimerRoot;
		[SerializeField] private Image turnTimerFill;

		private Coroutine timerCoroutine;

		public string PlayerId { get; private set; }

		[Header("Player Action UI")]
		[SerializeField] private GameObject actionRoot;
		[SerializeField] private TextMeshProUGUI actionText;
		[SerializeField] private TextMeshProUGUI actionAmountText;

		[Header("Event")]
		public UnityEvent<bool> activeTurn;


		[Header("Poker Ranking UI")]
		[SerializeField] private GameObject cardShowTimePersonalRankingParent;
		[SerializeField] private TMP_Text cardShowTimePersonalRankingText;

		public CardDealerAnimator[] cardDealerAnimator;

		Coroutine co_AutoActionCall;
		private void Start()
		{
			activeTurn.AddListener((active) => {
				if (PlayerId == Server.Instance.PlayerId) {
					if (active)
					{
						PokerGameController.instance.bottomButtonActivityPanel_NotMyTurn.SetActive(false);
						PokerGameController.instance.bottomButtonActivityPanel.SetActive(true);
						if (co_AutoActionCall != null) {
							StopCoroutine(co_AutoActionCall);
						}
						co_AutoActionCall = StartCoroutine(AutoActionCall());
					}
					else
					{
						PokerGameController.instance.bottomButtonActivityPanel_NotMyTurn.SetActive(true);
						PokerGameController.instance.bottomButtonActivityPanel.SetActive(false);
					}
				}
			});

		}

		IEnumerator AutoActionCall()
		{
			yield return new WaitForSeconds(0.2f);
			if (PokerUIController.Instance)
			{
				if (PokerUIController.Instance.autoCheckAction)
				{
					PokerUIController.Instance.CheckAction();
				}
				if (PokerUIController.Instance.autoFoldAction)
				{
					PokerUIController.Instance.FoldAction();
				}
			}
		}

		// ==============================
		// PLAYER UI
		// ==============================

		public void SetEmpty()
		{
			PlayerId = "";
			ClearUpdateTotalContribution();

			if (playerPanel != null)
				playerPanel.SetActive(false);

			if (emptyPanel != null)
				emptyPanel.SetActive(true);

			HideCards();
			StopTurnTimer();
			HidePlayerAction();
			HidePublicRanking();
		}

		public void ShowPlayerAction(string action, long amount)
		{

			if(timerCoroutine != null)
			{
				StopCoroutine(timerCoroutine);
				timerCoroutine = null;
			}
			if (turnTimerFill != null)
			{
				turnTimerFill.fillAmount = 1;
			}

			if (turnTimerRoot != null)
			{
				turnTimerRoot.SetActive(false);
			}

			if (PlayerId == Server.Instance.PlayerId)
			{
				ownTurn.SetActive(false);				
			}

			activeTurn!.Invoke(false);

			if (actionRoot != null)
				actionRoot.SetActive(true);

			if (actionText != null)
			{
				actionText.text = action switch
				{
					"ALL_IN" => "ALL IN",
					"CHECK_FOLD" => "CHECK / FOLD",
					_ => action
				};
			}

			bool showAmount =
				amount > 0 &&
				action != "CHECK" &&
				action != "FOLD";

			if (actionAmountText != null)
			{
				actionAmountText.gameObject.SetActive(showAmount);

				if (showAmount)
				{
					actionAmountText.text = amount.ToString("N0");
					GameObject amountField = Instantiate(currentBetPrefab, transform);
					amountField.transform.SetParent(this.transform, false);
					amountField.transform.localScale = Vector3.one;
					amountField.GetComponent<MasterUIMove>().StartInit(this.GetComponent<RectTransform>(), targetCurrentBetPrefab, $"${amount}");
				}
				else
				{
					GameObject amountField = Instantiate(currentBetPrefab, transform);
					amountField.transform.SetParent(this.transform, false);
					amountField.transform.localScale = Vector3.one;
					amountField.GetComponent<MasterUIMove>().FadeIn(statusText.GetComponent<RectTransform>(), $"{action}");

				}
			}
		}

		public void HidePlayerAction()
		{
			if (actionRoot != null)
				actionRoot.SetActive(false);
		}

		public void SetPlayer(
			 string playerId,
	string playerName,
	string stack,
	string currentBet,
	string totalContribution,
	bool isMe,
	bool isDealer,
	bool isSmallBlind,
	bool isBigBlind,
	string status)
		{
			PlayerId = playerId;

			if (playerPanel != null)
				playerPanel.SetActive(true);

			if (emptyPanel != null)
				emptyPanel.SetActive(false);

			if (playerNameText != null)
				playerNameText.text = playerName;

			if (stackText != null)
				stackText.text = stack;

			if (statusText != null)
				statusText.text = status;

			UpdateTotalContribution(totalContribution);

			if (meBadge != null)
				meBadge.SetActive(isMe);

			if (dealerBadge != null)
				dealerBadge.SetActive(isDealer);

			if (smallBlindBadge != null)
				smallBlindBadge.SetActive(isSmallBlind);

			if (bigBlindBadge != null)
				bigBlindBadge.SetActive(isBigBlind);

			/*if (PlayerId != Server.Instance.PlayerId)
			{
				ShowCardBack();
			}*/
		}

		public void UpdateTotalContribution(string value)
		{
			if (totalContributionText == null)
				return;

			if (!long.TryParse(
				value,
				NumberStyles.Integer,
				CultureInfo.InvariantCulture,
				out long amount))
			{
				amount = 0;
			}

			totalContributionText.text = amount.ToString(
				"N0",
				CultureInfo.InvariantCulture
			);

			totalContributionTextPanel.gameObject.SetActive(amount > 0);
			totalContributionText.gameObject.SetActive(amount > 0);
		}
		public void ClearUpdateTotalContribution()
		{
			UpdateTotalContribution("0");
		}

		// ==============================
		// HOLE CARDS
		// ==============================

		public void ResetHoleCards()
		{
			ShowCardBack();
		}

		public void ResetCards()
		{
			cardShowTimePersonalRankingParent.SetActive(false);
			for (int i = 0; i < cardDealerAnimator.Length; i++)
			{
				cardDealerAnimator[i].ReStart();
				if (cardRoot != null)
				{
					cardRoot.SetActive(false);
				}
			}
		}

		public void ShowCardBack()
		{
			if (cardRoot != null)
				cardRoot.SetActive(true);

			for (int i = 0; i < cardImages.Length; i++)
			{
				if (cardImages[i] == null)
					continue;

				cardImages[i].gameObject.SetActive(true);
				cardImages[i].SetImage(CARD_BACK);
				
			}
		}

		public void OpponentShowCards()
		{

			for (int slotIndex = 0; slotIndex < 2; slotIndex++) {
				cardImages[slotIndex].gameObject.SetActive(true);
			}
			for (int i = 0; i < cardDealerAnimator.Length; i++)
			{
				cardDealerAnimator[i].Throw();
			}
		}

		public void HideOpponentCard()
		{
			for (int slotIndex = 0; slotIndex < 2; slotIndex++)
			{
				cardImages[slotIndex].gameObject.SetActive(false);
			}
		}

		public void ShowHoleCards(JArray cards,bool beforePrePot)
		{
			if (cards == null || cards.Count != 2)
				return;

			if (cardRoot != null)
			{
				cardRoot.SetActive(true);
			}

			for (int slotIndex = 0; slotIndex < 2; slotIndex++)
			{
				JObject card = cards[slotIndex] as JObject;

				if (card == null)
					continue;

				string code = card["code"]?.ToString();

				string suit = card["suit"]?.ToString();

				if (string.IsNullOrEmpty(code))
				{
					string rank = card["rank"]?.ToString();

					if (string.IsNullOrEmpty(rank) || string.IsNullOrEmpty(suit))
						continue;

					code = $"{rank}_{suit}";
				}

				if (string.IsNullOrEmpty(suit))
					continue;

				code = code.ToUpperInvariant();
				suit = suit.ToUpperInvariant();

				cardImages[slotIndex].gameObject.SetActive(true);
				BGCStudioImage image = cardImages[slotIndex];

				if (image == null)
					continue;

				
				string path = cardFolder + card["suit"]?.ToString() + "/" + code + cardExtension;
				Debug.Log("BGCSTUDIO_path: " + path);
				image.SetImage(path);


				for (int i = 0; i < cardDealerAnimator.Length; i++)
				{
					if (beforePrePot)
					{
						cardDealerAnimator[i].Throw();
					}
					else
					{
						cardDealerAnimator[i].JustVisible();
					}
				}
				
			}
		}

		
		public void HideCards()
		{
			if (cardRoot != null)
				cardRoot.SetActive(false);

			HidePublicRanking();
			HideOpponentCard();
		}

		// ==============================
		// TURN TIMER
		// ==============================

		public void StartTurnTimer(float duration)
		{

			StopTurnTimer();

			if (duration <= 0)
				return;

			if (turnTimerRoot != null)
				turnTimerRoot.SetActive(true);

			if (turnTimerFill != null)
				turnTimerFill.fillAmount = 1f;

			activeTurn!.Invoke(true);
			ownTurn.SetActive(true);
			if (timerCoroutine != null)
			{
				StopCoroutine(timerCoroutine);
			}
			timerCoroutine = StartCoroutine(AnimateTimer(duration));
		}

		private IEnumerator AnimateTimer(float duration)
		{
			float remaining = duration;

			while (remaining > 0)
			{
				remaining -= Time.unscaledDeltaTime;

				float fill = Mathf.Clamp01(remaining / duration);

				if (turnTimerFill != null)
					turnTimerFill.fillAmount = fill;

				yield return null;
			}

			if (turnTimerFill != null)
			{
				turnTimerFill.fillAmount = 0;
			}


			if (turnTimerRoot != null)
			{
				turnTimerRoot.SetActive(false);
			}
			ownTurn.SetActive(false);
			activeTurn!.Invoke(false);
			timerCoroutine = null;

		}

		public void StopTurnTimer()
		{
			ownTurn.SetActive(false);
			activeTurn!.Invoke(false);
			if (timerCoroutine != null)
			{
				StopCoroutine(timerCoroutine);
				timerCoroutine = null;
			}

			if (turnTimerRoot != null)
				turnTimerRoot.SetActive(false);
		}



		private void OnDisable()
		{
			StopTurnTimer();
		}

		// Show public ranking after showdown
		public void ShowPublicRanking(string ranking)
		{
			if (string.IsNullOrEmpty(ranking))
				return;

			if (cardShowTimePersonalRankingParent != null)
				cardShowTimePersonalRankingParent.SetActive(true);

			if (cardShowTimePersonalRankingText != null)
				cardShowTimePersonalRankingText.text = ranking.Replace("_", " ");
		}

		// Hide ranking when a new hand starts
		public void HidePublicRanking()
		{
			if (cardShowTimePersonalRankingParent != null)
				cardShowTimePersonalRankingParent.SetActive(false);

			if (cardShowTimePersonalRankingText != null)
				cardShowTimePersonalRankingText.text = "";


			totalContributionTextPanel.gameObject.SetActive(false);
			totalContributionText.text = "";
		}

		#region WIN And LOSS Animation System
		public void WinLossEffect(bool isWin,long winAmount)
		{
			StartCoroutine(WinLossEffectInit(isWin, winAmount));
		}

		IEnumerator WinLossEffectInit(bool isWin, long winAmount)
		{
			bool isPlayerHighlight = false;
			playerPanel.transform.DOScale(isWin ? 1.2f : 0.8f, 1f).OnComplete(() => {
				isPlayerHighlight = true;
			});
			while (!isPlayerHighlight) {
				yield return new WaitForSeconds(0.1f);
			}
			if (isWin)
			{
				GameObject amountField = Instantiate(currentBetPrefab, targetCurrentBetPrefab.transform);
				amountField.transform.SetParent(this.transform, false);
				amountField.transform.localScale = Vector3.one;
				amountField.GetComponent<MasterUIMove>().StartInit(targetCurrentBetPrefab, this.GetComponent<RectTransform>(), $"${winAmount}");
			}
			yield return new WaitForSeconds(2f);

			playerPanel.transform.DOScale(1f, 1f);
		}

		#endregion
	}
}
