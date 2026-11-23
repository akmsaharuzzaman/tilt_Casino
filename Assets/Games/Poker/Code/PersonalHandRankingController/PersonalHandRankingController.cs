
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PersonalHandRankingController : MonoBehaviour
{
	public static PersonalHandRankingController Instance { get; private set; }

	[Header("Personal Ranking Event")]
	public Action<string> OnPersonalRankingUpdated;

	private List<PokerCard> holeCards = new List<PokerCard>();
	private List<PokerCard> communityCards = new List<PokerCard>();

	private void Awake()
	{
		Instance = this;
	}

	public string GetCurrentRanking()
	{
		if (holeCards == null || holeCards.Count != 2)
			return "WAITING";

		int rank1 = holeCards[0].Rank;
		int rank2 = holeCards[1].Rank;

		if (rank1 == rank2)
			return $"POCKET {GetRankName(rank1)} PAIR";

		int high = Mathf.Max(rank1, rank2);
		int low = Mathf.Min(rank1, rank2);

		return $"{GetRankName(high)} {GetRankName(low)}";
	}

	private string GetRankName(int rank)
	{
		switch (rank)
		{
			case 14: return "ACE";
			case 13: return "KING";
			case 12: return "QUEEN";
			case 11: return "JACK";
			case 10: return "TEN";
			case 9: return "NINE";
			case 8: return "EIGHT";
			case 7: return "SEVEN";
			case 6: return "SIX";
			case 5: return "FIVE";
			case 4: return "FOUR";
			case 3: return "THREE";
			case 2: return "TWO";
			default: return "UNKNOWN";
		}
	}

	public void ResetRanking()
	{
		holeCards.Clear();
		communityCards.Clear();

		OnPersonalRankingUpdated!.Invoke("WAITING FOR FLOP");
	}

	public void SetHoleCards(JArray cards)
	{
		holeCards = PokerHandEvaluator.ParseCards(cards);

		UpdateRanking();
	}

	public void HandleCommunityCardsDealt(JObject data)
	{
		if (data == null)
			return;

		JArray allCards = data["communityCards"] as JArray;

		if (allCards != null)
		{
			communityCards =
				PokerHandEvaluator.ParseCards(allCards);
		}
		else
		{
			JArray newCards = data["cards"] as JArray;

			if (newCards == null)
				return;

			string street = data["street"]?.ToString();

			List<PokerCard> parsed =
				PokerHandEvaluator.ParseCards(newCards);

			if (street == "FLOP")
				communityCards.Clear();

			communityCards.AddRange(parsed);
		}

		UpdateRanking();
	}

	private void UpdateRanking()
	{
		if (holeCards.Count != 2 ||
			communityCards.Count < 3)
			return;

		List<PokerCard> allCards =
			new List<PokerCard>(holeCards);

		allCards.AddRange(communityCards);

		string ranking =
			PokerHandEvaluator.EvaluateBestHand(allCards);

		Debug.Log(
			$"[PERSONAL RANKING] {ranking} | Cards: {allCards.Count}"
		);

		if (allCards.Count >= 2)
		{
			OnPersonalRankingUpdated!.Invoke(ranking);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
}
