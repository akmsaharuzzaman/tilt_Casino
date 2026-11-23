
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TableSelectionSwipeCarousel;

public class CarouselCardUI : MonoBehaviour
{
	[Header("Images")]
	[SerializeField] private BGCStudioImage mainImage;
	[SerializeField] private BGCStudioImage iconImage;

	[Header("Texts")]
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private TMP_Text descriptionText;
	[SerializeField] private TMP_Text playButtonText;



	public void SetData(CarouselData data)
	{
		if (data == null)
			return;

		// Main Image
		if (mainImage != null &&
			!string.IsNullOrWhiteSpace(data.imagePath))
		{
			mainImage.SetImage(data.imagePath);
		}

		// Icon Image
		if (iconImage != null &&
			!string.IsNullOrWhiteSpace(data.iconPath))
		{
			iconImage.SetImage(data.iconPath);
		}

		// Title
		if (titleText != null)
			titleText.text = data.title ?? "";

		// Description
		if (descriptionText != null)
			descriptionText.text = data.discription ?? "";

		// Play Button Text
		if (playButtonText != null)
			playButtonText.text = data.playButtonText ?? "";
	}
	public void SetData(TableCarouselData data)
	{
		if (data == null)
			return;

		// Main Image
		if (mainImage != null &&
			!string.IsNullOrWhiteSpace(data.imagePath))
		{
			mainImage.SetImage(data.imagePath);
		}

		// Title
		if (titleText != null)
			titleText.text = data.title ?? "";

		// Description
		if (descriptionText != null)
			descriptionText.text = data.discription ?? "";

		// Play Button Text
		if (playButtonText != null)
			playButtonText.text = data.playButtonText ?? "";

	}

	public void Play()
	{
		SceneManager.LoadScene(1);
	}
}
