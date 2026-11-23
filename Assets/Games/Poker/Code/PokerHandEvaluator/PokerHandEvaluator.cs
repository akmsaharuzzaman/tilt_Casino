
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct PokerCard
{
	public int Rank;
	public string Suit;

	public PokerCard(int rank, string suit)
	{
		Rank = rank;
		Suit = suit;
	}
}

public static class PokerHandEvaluator
{
	public static string EvaluateBestHand(List<PokerCard> cards)
	{
		if (cards == null || cards.Count < 3)
			return "WAITING";

		int bestCategory = -1;

		int requiredCards = Mathf.Min(cards.Count, 5);

		for (int a = 0; a < cards.Count - requiredCards + 1; a++)
		{
			for (int b = a + 1; b < cards.Count - requiredCards + 2; b++)
			{
				for (int c = b + 1; c < cards.Count - requiredCards + 3; c++)
				{
					if (requiredCards == 3)
					{
						int category = EvaluateThreeCards(new List<PokerCard>
					{
						cards[a], cards[b], cards[c]
					});

						bestCategory = Mathf.Max(bestCategory, category);
					}
				}
			}
		}

		if (cards.Count >= 4)
		{
			for (int a = 0; a < cards.Count - 3; a++)
				for (int b = a + 1; b < cards.Count - 2; b++)
					for (int c = b + 1; c < cards.Count - 1; c++)
						for (int d = c + 1; d < cards.Count; d++)
						{
							int category = EvaluateFourCards(new List<PokerCard>
			{
				cards[a], cards[b], cards[c], cards[d]
			});

							bestCategory = Mathf.Max(bestCategory, category);
						}
		}

		if (cards.Count >= 5)
		{
			return EvaluateFiveCardBestHand(cards);
		}

		return GetHandName(bestCategory);
	}
	private static int EvaluateThreeCards(List<PokerCard> cards)
	{
		var groups = cards.GroupBy(c => c.Rank)
			.Select(g => g.Count())
			.OrderByDescending(x => x)
			.ToList();

		if (groups[0] == 3) return 3;
		if (groups[0] == 2) return 1;

		return 0;
	}

	private static int EvaluateFourCards(List<PokerCard> cards)
	{
		var groups = cards.GroupBy(c => c.Rank)
			.Select(g => g.Count())
			.OrderByDescending(x => x)
			.ToList();

		bool flush = cards.All(c => c.Suit == cards[0].Suit);

		if (groups[0] == 4) return 7;
		if (groups[0] == 3) return 3;
		if (groups[0] == 2 && groups.Count > 1 && groups[1] == 2) return 2;
		if (groups[0] == 2) return 1;
		if (flush) return 5;

		return 0;
	}
	private static string EvaluateFiveCardBestHand(List<PokerCard> cards)
	{
		int bestCategory = -1;

		for (int a = 0; a < cards.Count - 4; a++)
			for (int b = a + 1; b < cards.Count - 3; b++)
				for (int c = b + 1; c < cards.Count - 2; c++)
					for (int d = c + 1; d < cards.Count - 1; d++)
						for (int e = d + 1; e < cards.Count; e++)
						{
							int category = EvaluateFiveCards(new List<PokerCard>
		{
			cards[a], cards[b], cards[c], cards[d], cards[e]
		});

							bestCategory = Mathf.Max(bestCategory, category);
						}

		return GetHandName(bestCategory);
	}
	private static int EvaluateFiveCards(List<PokerCard> cards)
	{
		var groups = cards
			.GroupBy(c => c.Rank)
			.Select(g => new
			{
				Rank = g.Key,
				Count = g.Count()
			})
			.OrderByDescending(g => g.Count)
			.ThenByDescending(g => g.Rank)
			.ToList();

		bool flush = cards.All(c => c.Suit == cards[0].Suit);

		int[] ranks = cards
			.Select(c => c.Rank)
			.Distinct()
			.OrderByDescending(r => r)
			.ToArray();

		int straightHigh = 0;

		if (ranks.Length == 5)
		{
			if (ranks[0] - ranks[4] == 4)
				straightHigh = ranks[0];
			else if (ranks.SequenceEqual(new[] { 14, 5, 4, 3, 2 }))
				straightHigh = 5;
		}

		if (flush && straightHigh == 14)
			return 9;

		if (flush && straightHigh > 0)
			return 8;

		if (groups[0].Count == 4)
			return 7;

		if (groups[0].Count == 3 && groups[1].Count == 2)
			return 6;

		if (flush)
			return 5;

		if (straightHigh > 0)
			return 4;

		if (groups[0].Count == 3)
			return 3;

		if (groups[0].Count == 2 && groups[1].Count == 2)
			return 2;

		if (groups[0].Count == 2)
			return 1;

		return 0;
	}

	public static string GetHandName(int category)
	{
		switch (category)
		{
			case 9: return "ROYAL FLUSH";
			case 8: return "STRAIGHT FLUSH";
			case 7: return "FOUR OF A KIND";
			case 6: return "FULL HOUSE";
			case 5: return "FLUSH";
			case 4: return "STRAIGHT";
			case 3: return "THREE OF A KIND";
			case 2: return "TWO PAIR";
			case 1: return "ONE PAIR";
			default: return "HIGH CARD";
		}
	}

	public static List<PokerCard> ParseCards(
		Newtonsoft.Json.Linq.JArray cards)
	{
		List<PokerCard> result = new List<PokerCard>();

		if (cards == null)
			return result;

		foreach (Newtonsoft.Json.Linq.JToken token in cards)
		{
			string rank = token["rank"]?.ToString();
			string suit = token["suit"]?.ToString();

			if (string.IsNullOrEmpty(rank) ||
				string.IsNullOrEmpty(suit))
			{
				string code = token["code"]?.ToString();

				if (string.IsNullOrEmpty(code))
					continue;

				string[] parts = code.Split('_');

				if (parts.Length != 2)
					continue;

				rank = parts[0];
				suit = parts[1];
			}

			int rankValue;

			switch (rank.ToUpperInvariant())
			{
				case "A": rankValue = 14; break;
				case "K": rankValue = 13; break;
				case "Q": rankValue = 12; break;
				case "J": rankValue = 11; break;
				case "T":
				case "10": rankValue = 10; break;
				default:
					if (!int.TryParse(rank, out rankValue))
						continue;
					break;
			}

			result.Add(new PokerCard(
				rankValue,
				suit.ToUpperInvariant()
			));
		}

		return result;
	}
}
