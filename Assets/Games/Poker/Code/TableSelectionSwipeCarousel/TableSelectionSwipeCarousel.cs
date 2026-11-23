using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TableSelectionSwipeCarousel : MonoBehaviour
{


	[Serializable]
	public class TableCarouselData
	{
		public string imagePath;
		public string title;
		public string discription;
		public string playButtonText;
		public PokerTableConfig pokerTableConfig;
		public Image smallCardBG;
	}

	[Header("Carousel Data")]
	[SerializeField]
	private List<TableCarouselData> imagePaths =
		new List<TableCarouselData>();

	[Header("References")]
	[SerializeField] private RectTransform contentRoot;
	[SerializeField] private RectTransform cardPrefab;

	[Header("Navigation Buttons")]
	[SerializeField] private Button leftButton;
	[SerializeField] private Button rightButton;

	[Header("Card Layout")]
	[SerializeField] private float spacing = 280f;
	[SerializeField] private float selectedY = 0f;
	[SerializeField] private float sideY = -10f;
	[SerializeField] private float sideRotation = 5f;
	[SerializeField] private float visibleRange = 2f;

	[Header("Card Scale")]
	[SerializeField] private float selectedScale = 1.15f;
	[SerializeField] private float sideScale = 0.75f;

	[Header("Appearance")]
	[Range(0f, 1f)]
	[SerializeField] private float sideAlpha = 0.7f;

	[Header("DOTween Settings")]
	[SerializeField] private float duration = 0.4f;
	[SerializeField] private Ease ease = Ease.OutCubic;

	private readonly List<RectTransform> cards =
		new List<RectTransform>();

	private readonly List<CanvasGroup> canvasGroups =
		new List<CanvasGroup>();

	private readonly List<PokerTableSelection> pokerTableSelections =
		new List<PokerTableSelection>();

	private readonly List<CarouselCardUI> cardUIs =
		new List<CarouselCardUI>();

	private float currentIndex;

	private int targetIndex;

	private Tween snapTween;

	// =================================
	// PUBLIC DATA
	// =================================

	public int SelectedIndex
	{
		get
		{
			return Mathf.Clamp(
				Mathf.RoundToInt(currentIndex),
				0,
				Mathf.Max(0, imagePaths.Count - 1)
			);
		}
	}

	public TableCarouselData SelectedData
	{
		get
		{
			if (imagePaths.Count == 0)
				return null;

			return imagePaths[SelectedIndex];
		}
	}

	public string SelectedImagePath
	{
		get
		{
			return SelectedData != null
				? SelectedData.imagePath
				: string.Empty;
		}
	}

	public event Action<TableCarouselData> OnSelectionChanged;

	// =================================
	// INITIALIZE
	// =================================

	private void Start()
	{
		if (leftButton != null)
			leftButton.onClick.AddListener(Previous);

		if (rightButton != null)
			rightButton.onClick.AddListener(Next);

		BuildCards();
	}

	private void OnDestroy()
	{
		snapTween?.Kill();

		if (leftButton != null)
			leftButton.onClick.RemoveListener(Previous);

		if (rightButton != null)
			rightButton.onClick.RemoveListener(Next);
	}

	// =================================
	// BUILD CARDS
	// =================================

	public void BuildCards()
	{
		snapTween?.Kill();

		// Destroy existing cards
		for (int i = 0; i < cards.Count; i++)
		{
			if (cards[i] != null)
				Destroy(cards[i].gameObject);
		}

		cards.Clear();
		canvasGroups.Clear();
		pokerTableSelections.Clear();
		cardUIs.Clear();

		if (contentRoot == null || cardPrefab == null)
		{
			Debug.LogError(
				"MasterSwipeCarousel: Missing ContentRoot or CardPrefab!",
				this
			);

			UpdateNavigationButtons();
			return;
		}

		// Create cards from CarouselData
		for (int i = 0; i < imagePaths.Count; i++)
		{
			if (imagePaths[i] == null)
				continue;

			CreateCard(imagePaths[i], i);
		}

		if (cards.Count == 0)
		{
			currentIndex = 0;
			targetIndex = 0;

			UpdateNavigationButtons();
			return;
		}

		currentIndex = Mathf.Clamp(
			currentIndex,
			0f,
			cards.Count - 1
		);

		targetIndex = Mathf.RoundToInt(currentIndex);

		UpdateCards();
		UpdateCardOrder();
		UpdateNavigationButtons();

		NotifySelectionChanged();
	}

	private void CreateCard(TableCarouselData data, int index)
	{
		RectTransform card = Instantiate(
			cardPrefab,
			contentRoot
		);

		card.name = "SwipeCard_" + index;

		card.anchorMin = new Vector2(0.5f, 0.5f);
		card.anchorMax = new Vector2(0.5f, 0.5f);
		card.pivot = new Vector2(0.5f, 0.5f);

		PokerTableSelection tableSelection = card.GetComponent<PokerTableSelection>();
		if(tableSelection != null)
		{
			tableSelection.SelectedTable = data.pokerTableConfig;
			tableSelection.smallCardBG = data.smallCardBG;

			if (index == 0)
			{
				tableSelection.parentImage.color = new Color(255, 255, 255, 255);				
				tableSelection.unselected.SetActive(false);
				tableSelection.smallCardBG.color = new Color(255, 255, 255, 255);

			}
			else
			{
				tableSelection.parentImage.color = new Color(255, 255, 255, 0);
				tableSelection.smallCardBG.color = new Color(255, 255, 255, 0);
				tableSelection.unselected.SetActive(true);
			}
			pokerTableSelections.Add(tableSelection);
		}

		// Get Card UI Component
		CarouselCardUI cardUI =
			card.GetComponent<CarouselCardUI>();

		if (cardUI == null)
		{
			Debug.LogError(
				"CarouselCardUI missing on Card Prefab!",
				card
			);

			Destroy(card.gameObject);
			return;
		}

		// Update Image and TMP Text
		cardUI.SetData(data);

		// Canvas Group
		CanvasGroup group = card.GetComponent<CanvasGroup>();

		if (group == null)
			group = card.gameObject.AddComponent<CanvasGroup>();

		group.blocksRaycasts = false;
		group.interactable = false;

		cards.Add(card);
		canvasGroups.Add(group);
		cardUIs.Add(cardUI);
	}

	// =================================
	// CARD POSITION / SCALE
	// =================================

	private void UpdateCards()
	{
		for (int i = 0; i < cards.Count; i++)
		{
			RectTransform card = cards[i];

			if (card == null)
				continue;

			float distance = i - currentIndex;

			float absDistance = Mathf.Abs(distance);

			float normalized =
				1f - Mathf.Clamp01(absDistance);

			// Scale
			float scale = Mathf.Lerp(
				sideScale,
				selectedScale,
				normalized
			);

			// Vertical Position
			float y = Mathf.Lerp(
				sideY,
				selectedY,
				normalized
			);

			// Rotation
			float rotation = -distance * sideRotation;

			// Position
			card.anchoredPosition = new Vector2(
				distance * spacing,
				y
			);

			// Scale
			card.localScale = Vector3.one * scale;

			// Rotation
			card.localRotation = Quaternion.Euler(
				0f,
				0f,
				rotation
			);

			// Visibility
			bool visible = absDistance <= visibleRange;

			if (card.gameObject.activeSelf != visible)
				card.gameObject.SetActive(visible);

			// Alpha
			canvasGroups[i].alpha = Mathf.Lerp(
				sideAlpha,
				1f,
				normalized
			);

			// Button Interaction
			bool isSelected = Mathf.Abs(distance) < 0.01f;

			canvasGroups[i].blocksRaycasts = isSelected;
			canvasGroups[i].interactable = isSelected;
			pokerTableSelections[i].parentImage.color = new Color(255, 255, 255, isSelected ? 255 : 0);
			pokerTableSelections[i].smallCardBG.color = new Color(255, 255, 255, isSelected ? 255 : 0);
			pokerTableSelections[i].unselected.SetActive(!isSelected);
		}
	}

	// =================================
	// CARD DRAW ORDER
	// =================================

	private void UpdateCardOrder()
	{
		List<int> order = new List<int>();

		for (int i = 0; i < cards.Count; i++)
			order.Add(i);

		// Farthest cards first, selected card last
		order.Sort((a, b) =>
		{
			float da = Mathf.Abs(a - currentIndex);
			float db = Mathf.Abs(b - currentIndex);

			return db.CompareTo(da);
		});

		for (int i = 0; i < order.Count; i++)
		{
			if (cards[order[i]] != null)
				cards[order[i]].SetAsLastSibling();
		}
	}

	// =================================
	// NEXT / PREVIOUS BUTTON
	// =================================

	public void Next()
	{
		if (cards.Count == 0)
			return;

		GoTo(targetIndex + 1);
	}

	public void Previous()
	{
		if (cards.Count == 0)
			return;

		GoTo(targetIndex - 1);
	}

	// =================================
	// GOTO SELECTED CARD
	// =================================

	public void GoTo(int index)
	{
		if (cards.Count == 0)
			return;

		index = Mathf.Clamp(
			index,
			0,
			cards.Count - 1
		);

		snapTween?.Kill();

		targetIndex = index;

		UpdateNavigationButtons();

		snapTween = DOTween.To(
			() => currentIndex,

			value =>
			{
				currentIndex = value;

				UpdateCards();
				UpdateCardOrder();
			},

			targetIndex,
			duration
		)
		.SetEase(ease)
		.OnComplete(() =>
		{
			currentIndex = targetIndex;

			UpdateCards();
			UpdateCardOrder();
			UpdateNavigationButtons();

			NotifySelectionChanged();
		});
	}

	// =================================
	// NAVIGATION BUTTON STATE
	// =================================

	private void UpdateNavigationButtons()
	{
		if (leftButton != null)
		{
			leftButton.interactable =
				targetIndex > 0;
		}

		if (rightButton != null)
		{
			rightButton.interactable =
				targetIndex < cards.Count - 1;
		}
	}

	// =================================
	// SELECTION EVENT
	// =================================

	private void NotifySelectionChanged()
	{
		TableCarouselData selected = SelectedData;

		if (selected != null)
			OnSelectionChanged?.Invoke(selected);
	}

	// =================================
	// DYNAMIC DATA MANAGEMENT
	// =================================

	public void AddImage(TableCarouselData data)
	{
		if (data == null)
			return;

		imagePaths.Add(data);

		BuildCards();
	}

	public void SetCarouselData(List<TableCarouselData> data)
	{
		imagePaths = data != null
			? new List<TableCarouselData>(data)
			: new List<TableCarouselData>();

		currentIndex = 0;
		targetIndex = 0;

		BuildCards();
	}

	public void UpdateImage(int index, TableCarouselData data)
	{
		if (data == null)
			return;

		if (index < 0 || index >= imagePaths.Count)
			return;

		imagePaths[index] = data;

		BuildCards();
	}

	public void RemoveImage(int index)
	{
		if (index < 0 || index >= imagePaths.Count)
			return;

		imagePaths.RemoveAt(index);

		currentIndex = Mathf.Clamp(
			currentIndex,
			0f,
			Mathf.Max(0, imagePaths.Count - 1)
		);

		targetIndex = Mathf.RoundToInt(currentIndex);

		BuildCards();
	}

	public void ClearImages()
	{
		imagePaths.Clear();

		currentIndex = 0;
		targetIndex = 0;

		BuildCards();
	}

	// =================================
	// SELECT BY INDEX
	// =================================

	public void SelectFirst()
	{
		GoTo(0);
	}

	public void SelectLast()
	{
		GoTo(cards.Count - 1);
	}
}
