
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CommunityCardController : MonoBehaviour
{
	
	[Header("Card Slots")]
	[SerializeField] private Image[] cardSlots = new Image[5];
	[SerializeField] private CardDealerAnimator[] cardDealerAnimators = new CardDealerAnimator[5];


	[Header("Image Settings")]
	[SerializeField] private string cardFolder = "Image/PokerSprites/Cards/";
	[SerializeField] private string cardExtension = ".png";
	[SerializeField] private string cardBackPath = "Image/PokerSprites/Cards/BACK.png";

	[Header("Reveal Settings")]
	[SerializeField] private float revealInterval = 0.3f;

	private BGCStudioImage[] cardImages = new BGCStudioImage[5];

	private Coroutine revealCoroutine;

	private const string COMMUNITY_CARDS_DEALT =
		"SGDSOFT@COMMUNITY_CARDS_DEALT";

	private const string HAND_STARTED =
		"SGDSOFT@HAND_STARTED";


	[Header("Community Ranking Event")]
	public Action<string> OnCommunityRankingUpdated;

	private void Awake()
	{
		InitializeCards();
	}

	private void Start()
	{
		if (Server.Instance == null) return;

		Server.Instance.RegisterEvent(
			COMMUNITY_CARDS_DEALT,
			OnCommunityCardsDealt
		);

		Server.Instance.RegisterEvent(
			HAND_STARTED,
			OnHandStarted
		);
	}

	private void OnDisable()
	{
		if (Server.Instance != null)
		{
			Server.Instance.UnregisterEvent(
				COMMUNITY_CARDS_DEALT,
				OnCommunityCardsDealt
			);

			Server.Instance.UnregisterEvent(
				HAND_STARTED,
				OnHandStarted
			);
		}

		StopReveal();
	}

	private void InitializeCards()
	{
		for (int i = 0; i < cardSlots.Length; i++)
		{
			if (cardSlots[i] == null) continue;

			cardImages[i] = cardSlots[i].GetComponent<BGCStudioImage>();

			if (cardImages[i] == null)
			{
				cardImages[i] =
					cardSlots[i].gameObject.AddComponent<BGCStudioImage>();
			}
		}
	}

	private void OnHandStarted(JObject data)
	{
		ResetCommunityCards();
		PersonalHandRankingController.Instance?.ResetRanking();
	}

	private void OnCommunityCardsDealt(JObject data)
	{
		JArray cards = data["cards"] as JArray;

		if (cards == null || cards.Count == 0)
		{
			Debug.LogWarning("[CARDS] No new cards received.");
			return;
		}

		// Community Ranking
		JArray communityCards = data["communityCards"] as JArray;

		if (communityCards != null)
		{
			var parsedCards =
				PokerHandEvaluator.ParseCards(communityCards);

			Debug.Log("[COMMUNITY] Parsed Cards: " + parsedCards.Count);

			string ranking = "WAITING";

			if (parsedCards.Count >= 3)
			{
				ranking = PokerHandEvaluator.EvaluateBestHand(parsedCards);
			}

			Debug.Log("[COMMUNITY] Calculated Ranking: " + ranking);

			InvokeCommunityRanking(ranking);
		}

		// Personal Ranking
		PersonalHandRankingController.Instance?.HandleCommunityCardsDealt(data);




		string street = data["street"]?.ToString() ?? "";

		int startIndex = street switch
		{
			"FLOP" => 0,
			"TURN" => 3,
			"RIVER" => 4,
			_ => -1
		};

		if (startIndex < 0)
		{
			Debug.LogWarning("[CARDS] Unknown street: " + street);
			return;
		}

		Debug.Log(
			$"[CARDS] {street} received. New cards: {cards.Count}"
		);

		StopReveal();

		revealCoroutine = StartCoroutine(
			RevealCards(cards, startIndex)
		);
	}

	private IEnumerator RevealCards(JArray cards, int startIndex)
	{
		for (int i = 0; i < cards.Count; i++)
		{
			int slotIndex = startIndex + i;

			if (slotIndex >= cardSlots.Length)
				break;

			JObject card = cards[i] as JObject;

			if (card == null)
				continue;

			string code = card["code"]?.ToString();
			if (string.IsNullOrEmpty(code))
			{
				string rank = card["rank"]?.ToString();
				string suit = card["suit"]?.ToString();

				code = $"{rank}_{suit}";
			}

			code = code.ToUpperInvariant();

			BGCStudioImage image = cardImages[slotIndex];

			if (image == null)
				continue;

			string path = cardFolder + card["suit"]?.ToString() + "/" + code + cardExtension;//$"{cardFolder}{suit}/{code}{cardExtension}"; //cardFolder + code + cardExtension;

			cardSlots[slotIndex].gameObject.SetActive(true);
			cardDealerAnimators[slotIndex].Throw();

			image.SetImage(path);

			Debug.Log($"[CARDS] Loading: {path}");

			yield return new WaitForSecondsRealtime(revealInterval);
		}

		revealCoroutine = null;
	}

	public void ResetCommunityCards()
	{
		StopReveal();

		foreach (Image slot in cardSlots)
		{
			if (slot == null) continue;

			slot.gameObject.SetActive(false);
		}
	}

	public void ShowCardBack(int index)
	{
		if (index < 0 || index >= cardImages.Length)
			return;

		if (cardImages[index] == null)
			return;

		cardSlots[index].gameObject.SetActive(true);
		cardDealerAnimators[index].Throw();
		cardImages[index].SetImage(cardBackPath);
	}

	private void StopReveal()
	{
		if (revealCoroutine != null)
		{
			StopCoroutine(revealCoroutine);
			revealCoroutine = null;
		}
	}
	private void InvokeCommunityRanking(string ranking)
	{
		OnCommunityRankingUpdated!.Invoke(ranking);
	}

}
