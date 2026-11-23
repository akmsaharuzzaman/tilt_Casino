using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardDealerAnimator : MonoBehaviour
{
	[Header("Card & Positions")]
	public Image backCard;
	public Image frontCard;
	[SerializeField] private RectTransform existingCard;
	[SerializeField] private RectTransform startPosition;
	[SerializeField] private RectTransform endPosition;
	[SerializeField] private Sprite testCardFrontSprite;

	[Header("Animation Settings")]
	[SerializeField] private float moveDuration = 0.55f;
	[SerializeField] private float flipDuration = 0.35f;
	[SerializeField] private float arcHeight = 80f;
	[SerializeField] private float flipPopScale = 1.25f;
	[SerializeField] private float flightTiltAngle = -15f;

	Coroutine coroutine;

	public void ReStart()
	{
		backCard.color = new Color(backCard.color.r, backCard.color.g, backCard.color.b, 0.0f);
		frontCard.color = new Color(frontCard.color.r, frontCard.color.g, frontCard.color.b, 0.0f);
	}

	public void JustVisible()
	{
		backCard.color = new Color(backCard.color.r, backCard.color.g, backCard.color.b, 0.0f);
		frontCard.color = new Color(frontCard.color.r, frontCard.color.g, frontCard.color.b, 1.0f);
	}

	public void Throw()
	{
		backCard.color = new Color(backCard.color.r, backCard.color.g, backCard.color.b, 0.0f);
		frontCard.color = new Color(frontCard.color.r, frontCard.color.g, frontCard.color.b, 0.0f);

		if (coroutine != null)
		{
			StopCoroutine(coroutine);
		}
		coroutine = StartCoroutine(TestExistingCardRoutine());
	}

	private IEnumerator TestExistingCardRoutine()
	{
		yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));

		if (existingCard != null && startPosition != null && endPosition != null)
		{
			MoveAndFlipCard(existingCard, startPosition, endPosition, testCardFrontSprite);
		}
	}

	public void MoveAndFlipCard(RectTransform cardRect, RectTransform fromPos, RectTransform toPos, Sprite frontCardSprite)
	{
		// Kill any active tweens on this card
		cardRect.DOKill();

		// Reset initial state (Start Y at 180 so it flips cleanly to 0)
		cardRect.gameObject.SetActive(true);
		cardRect.position = fromPos.position;
		cardRect.localScale = Vector3.one * 0.4f;
		cardRect.localRotation = Quaternion.Euler(0, 180f, 0);

		// Calculate curved mid-point for natural throw feel
		Vector3 startPoint = fromPos.position;
		Vector3 endPoint = toPos.position;
		Vector3 midPoint = (startPoint + endPoint) / 2f + new Vector3(0, arcHeight, 0);
		Vector3[] flightPath = new Vector3[] { startPoint, midPoint, endPoint };

		Sequence dealSequence = DOTween.Sequence();

		backCard.color = new Color(backCard.color.r, backCard.color.g, backCard.color.b, 1.0f);
		frontCard.color = new Color(frontCard.color.r, frontCard.color.g, frontCard.color.b, 0.0f);

		// 1. Curved Path Movement + Scale Up + Dynamic Tilt (Keep Y at 180 during flight)
		dealSequence.Append(cardRect.DOPath(flightPath, moveDuration, PathType.CatmullRom).SetEase(Ease.OutCubic));
		dealSequence.Join(cardRect.DOScale(Vector3.one, moveDuration * 0.8f).SetEase(Ease.OutBack));
		dealSequence.Join(cardRect.DOLocalRotate(new Vector3(0, 180f, flightTiltAngle), moveDuration * 0.6f).SetEase(Ease.OutQuad));

		// 2. Start 3D Flip slightly before landing (Mid-air flip vibe)
		float flipStartTime = moveDuration * 0.65f;

		// Pop scale up & rotate from 180 to 90 degrees while straightening Z-tilt
		dealSequence.Insert(flipStartTime, cardRect.DOScale(Vector3.one * flipPopScale, flipDuration * 0.5f).SetEase(Ease.OutQuad));
		dealSequence.Insert(flipStartTime, cardRect.DOLocalRotate(new Vector3(15f, 90f, 0f), flipDuration * 0.5f).SetEase(Ease.InQuad));

		// 3. Swap card face at 90 degrees
		dealSequence.InsertCallback(flipStartTime + (flipDuration * 0.5f), () =>
		{
			backCard.color = new Color(backCard.color.r, backCard.color.g, backCard.color.b, 0.0f);
			frontCard.color = new Color(frontCard.color.r, frontCard.color.g, frontCard.color.b, 1.0f);
		});

		// 4. Complete 3D Flip (from 90 to 0 degrees) & settle back to normal scale
		float secondHalfStart = flipStartTime + (flipDuration * 0.5f);
		dealSequence.Insert(secondHalfStart, cardRect.DOLocalRotate(new Vector3(0f, 0f, 0f), flipDuration * 0.5f).SetEase(Ease.OutBack));
		dealSequence.Insert(secondHalfStart, cardRect.DOScale(Vector3.one, flipDuration * 0.5f).SetEase(Ease.OutBounce));
	}
}
