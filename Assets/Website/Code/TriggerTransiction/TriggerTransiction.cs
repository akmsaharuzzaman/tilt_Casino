using UnityEngine;
using DG.Tweening;

public class TriggerTransition : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField] private GameObject currentUI;
	[SerializeField] private GameObject targetUI;

	[Header("Settings")]
	[SerializeField] private TransitionType transitionType = TransitionType.Scale;
	[SerializeField] private float duration = 0.4f;

	public void Open()
	{
		if (currentUI == null || targetUI == null)
		{
			Debug.LogWarning("Assign Current UI and Target UI");
			return;
		}

		switch (transitionType)
		{
			case TransitionType.Fade:
				FadeTransition();
				break;

			case TransitionType.Scale:
				ScaleTransition();
				break;

			case TransitionType.SlideLeft:
				SlideTransition(Vector2.left);
				break;

			case TransitionType.SlideRight:
				SlideTransition(Vector2.right);
				break;

			case TransitionType.SlideUp:
				SlideTransition(Vector2.up);
				break;

			case TransitionType.SlideDown:
				SlideTransition(Vector2.down);
				break;

			case TransitionType.Rotate:
				RotateTransition();
				break;

			case TransitionType.Pop:
				PopTransition();
				break;

			case TransitionType.Bounce:
				BounceTransition();
				break;

			case TransitionType.None:
				DisableUI(currentUI);
				EnableUI(targetUI);
				break;
		}
	}

	#region Fade

	private void FadeTransition()
	{
		CanvasGroup currentCG = GetCanvasGroup(currentUI);

		currentCG.DOFade(0f, duration).OnComplete(() =>
		{
			DisableUI(currentUI);

			EnableUI(targetUI);

			CanvasGroup targetCG = GetCanvasGroup(targetUI);
			targetCG.alpha = 0;

			targetCG.DOFade(1f, duration);
		});
	}

	#endregion

	#region Scale

	private void ScaleTransition()
	{
		currentUI.transform
			.DOScale(Vector3.zero, duration)
			.SetEase(Ease.InBack)
			.OnComplete(() =>
			{
				DisableUI(currentUI);

				EnableUI(targetUI);

				targetUI.transform.localScale = Vector3.zero;

				targetUI.transform
					.DOScale(Vector3.one, duration)
					.SetEase(Ease.OutBack);
			});
	}

	#endregion

	#region Slide

	private void SlideTransition(Vector2 direction)
	{
		RectTransform currentRect = currentUI.GetComponent<RectTransform>();

		Vector2 moveDistance = direction * 1500f;

		currentRect.DOAnchorPos(moveDistance, duration)
			.SetEase(Ease.InQuart)
			.OnComplete(() =>
			{
				DisableUI(currentUI);

				EnableUI(targetUI);

				RectTransform targetRect =
					targetUI.GetComponent<RectTransform>();

				targetRect.anchoredPosition = -moveDistance;

				targetRect.DOAnchorPos(Vector2.zero, duration)
					.SetEase(Ease.OutQuart);
			});
	}

	#endregion

	#region Rotate

	private void RotateTransition()
	{
		currentUI.transform
			.DORotate(new Vector3(0, 0, 180), duration)
			.SetEase(Ease.InBack)
			.OnComplete(() =>
			{
				DisableUI(currentUI);

				EnableUI(targetUI);

				targetUI.transform.rotation =
					Quaternion.Euler(0, 0, -180);

				targetUI.transform
					.DORotate(Vector3.zero, duration)
					.SetEase(Ease.OutBack);
			});
	}

	#endregion

	#region Pop

	private void PopTransition()
	{
		currentUI.transform
			.DOScale(0f, duration)
			.SetEase(Ease.InBack)
			.OnComplete(() =>
			{
				DisableUI(currentUI);

				EnableUI(targetUI);

				targetUI.transform.localScale = Vector3.zero;

				targetUI.transform
					.DOScale(1f, duration)
					.SetEase(Ease.OutElastic);
			});
	}

	#endregion

	#region Bounce

	private void BounceTransition()
	{
		currentUI.transform
			.DOScale(0.8f, duration * 0.5f)
			.OnComplete(() =>
			{
				DisableUI(currentUI);

				EnableUI(targetUI);

				targetUI.transform.localScale = Vector3.zero;

				targetUI.transform
					.DOScale(1f, duration)
					.SetEase(Ease.OutBounce);
			});
	}

	#endregion

	#region Helpers

	private CanvasGroup GetCanvasGroup(GameObject obj)
	{
		CanvasGroup cg = obj.GetComponent<CanvasGroup>();

		if (cg == null)
			cg = obj.AddComponent<CanvasGroup>();

		return cg;
	}

	private void EnableUI(GameObject ui)
	{
		ui.SetActive(true);

		CanvasGroup cg = GetCanvasGroup(ui);
		cg.alpha = 1f;

		RectTransform rect = ui.GetComponent<RectTransform>();

		if (rect != null)
		{
			rect.localScale = Vector3.one;
			rect.localRotation = Quaternion.identity;
			rect.anchoredPosition = Vector2.zero;
		}
	}

	private void DisableUI(GameObject ui)
	{
		CanvasGroup cg = GetCanvasGroup(ui);

		cg.alpha = 1f;

		RectTransform rect = ui.GetComponent<RectTransform>();

		if (rect != null)
		{
			rect.localScale = Vector3.one;
			rect.localRotation = Quaternion.identity;
			rect.anchoredPosition = Vector2.zero;
		}

		ui.SetActive(false);
	}

	#endregion
}

public enum TransitionType
{
	Fade,
	Scale,
	SlideLeft,
	SlideRight,
	SlideUp,
	SlideDown,
	Rotate,
	Pop,
	Bounce,
	None
}