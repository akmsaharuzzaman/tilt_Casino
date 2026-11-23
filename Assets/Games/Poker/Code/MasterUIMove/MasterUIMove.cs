
using UnityEngine;
using DG.Tweening;
using TMPro;

public class MasterUIMove : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private float duration = 1f;
	[SerializeField] private Ease ease = Ease.OutCubic;
	[SerializeField] private TextMeshProUGUI text;

	private Tween moveTween;

	public void StartInit(RectTransform currentPos, RectTransform _target, string _text)
	{
		if (currentPos == null || _target == null)
		{
			Destroy(gameObject);
			return;
		}

		RectTransform rect = GetComponent<RectTransform>();

		// Set starting world position
		rect.position = currentPos.position;

		// Set text
		if (text != null)
			text.text = _text;

		// Kill previous tween
		moveTween?.Kill();

		// Move to target world position
		moveTween = rect.DOMove(_target.position, duration)
			.SetEase(ease)
			.SetLink(gameObject)
			.OnComplete(() =>
			{
				Destroy(gameObject);
			});
	}

	public void FadeIn(RectTransform currentPos, string _text)
	{
		if (text != null)
			text.text = _text;

		CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

		if (canvasGroup == null)
			canvasGroup = gameObject.AddComponent<CanvasGroup>();

		canvasGroup.alpha = 0f;

		this.transform.GetComponent<RectTransform>().position = currentPos.position;

		canvasGroup.DOFade(2f, 0.2f)
			.SetEase(ease)
			.SetLink(gameObject)
			.OnComplete(() =>
			{
				Destroy(gameObject);
			});
	}

	private void OnDestroy()
	{
		moveTween?.Kill();
	}
}
