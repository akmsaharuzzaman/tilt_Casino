
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Poker
{
	public class RaisePopupUI : MonoBehaviour
	{
		[Header("Popup")]
		[SerializeField] private GameObject popupRoot;

		[Header("Buttons")]
		[SerializeField] private Button halfPotButton;
		[SerializeField] private Button fullPotButton;
		[SerializeField] private Button confirmButton;
		[SerializeField] private Button closeButton;

		[Header("Slider")]
		[SerializeField] private Slider raiseSlider;

		[Header("Selected Amount")]
		[SerializeField] private TMP_Text selectedRaiseToText;
		public TextMeshProUGUI raiseAmountText;


		[Header("Preset Buttons")]
		[SerializeField] private TMP_Text halfPotButtonText;
		[SerializeField] private TMP_Text fullPotButtonText;

		public event Action<string, long> OnActionConfirmed;

		[SerializeField] private long pot;
		[SerializeField] private long playerStack;
		[SerializeField] private long tableCurrentBet;
		[SerializeField] private long playerCurrentBet;
		[SerializeField] private long minRaiseTo;

		[SerializeField] private long maxRaiseTo;
		[SerializeField] private long selectedRaiseTo;

		private bool isInitialized;

		private void Awake()
		{
			Initialize();
			Close();
		}

		private void Initialize()
		{
			if (isInitialized)
				return;

			if (halfPotButton != null)
				halfPotButton.onClick.AddListener(SetHalfPot);

			if (fullPotButton != null)
				fullPotButton.onClick.AddListener(SetFullPot);

			if (confirmButton != null)
				confirmButton.onClick.AddListener(ConfirmRaise);

			if (closeButton != null)
				closeButton.onClick.AddListener(Close);

			if (raiseSlider != null)
				raiseSlider.onValueChanged.AddListener(OnSliderChanged);

			isInitialized = true;
		}

		public void Open(
			long tablePot,
			long stack,
			long currentBet,
			long myCurrentBet,
			long minimumRaiseTo)
		{
			pot = tablePot;// Math.Max(0, tablePot);
			playerStack = stack;//Math.Max(0, stack);
			tableCurrentBet = currentBet;//Math.Max(0, currentBet);
			playerCurrentBet = myCurrentBet;// Math.Max(0, myCurrentBet);

			//minRaiseTo = tableCurrentBet + ;

			if (raiseAmountText != null)
			{
				raiseAmountText.text = "$" + minRaiseTo.ToString("N0");
			}

			long callAmount = Math.Max(
				0,
				tableCurrentBet - playerCurrentBet
			);

			maxRaiseTo = playerCurrentBet + playerStack;

			long potAfterCall = pot + callAmount;

			long calculatedHalfPot =
				playerCurrentBet +
				callAmount +
				potAfterCall / 2;

			long calculatedFullPot =
				playerCurrentBet +
				callAmount +
				potAfterCall;

			minRaiseTo = (minimumRaiseTo + tableCurrentBet + 1);
			/*minRaiseTo = Math.Max(
				minRaiseTo,
				tableCurrentBet + 1
			);

			minRaiseTo = Math.Min(
				minRaiseTo,
				maxRaiseTo
			);*/

			if (popupRoot != null)
				popupRoot.SetActive(true);

			if (raiseSlider != null)
			{
				raiseSlider.minValue = minRaiseTo;
				raiseSlider.maxValue = maxRaiseTo;
				raiseSlider.wholeNumbers = true;

				raiseSlider.interactable =
					maxRaiseTo > minRaiseTo;
			}

			if (halfPotButton != null)
			{
				halfPotButton.interactable =
					calculatedHalfPot >= minRaiseTo;
			}

			if (fullPotButton != null)
			{
				fullPotButton.interactable =
					calculatedFullPot >= minRaiseTo;
			}

			if (halfPotButtonText != null)
			{
				halfPotButtonText.text =
					"1/2 POT";
			}

			if (fullPotButtonText != null)
			{
				fullPotButtonText.text =
					"FULL POT";
			}

			selectedRaiseTo = minRaiseTo;

			SetRaiseTo(selectedRaiseTo);
		}

		private void SetHalfPot()
		{
			long callAmount = Math.Max(
				0,
				tableCurrentBet - playerCurrentBet
			);

			long potAfterCall = pot + callAmount;

			long target =
				playerCurrentBet +
				callAmount +
				potAfterCall / 2;

			SetRaiseTo(target);
		}

		private void SetFullPot()
		{
			long callAmount = Math.Max(
				0,
				tableCurrentBet - playerCurrentBet
			);

			long potAfterCall = pot + callAmount;

			long target =
				playerCurrentBet +
				callAmount +
				potAfterCall;

			SetRaiseTo(target);
		}

		private void OnSliderChanged(float value)
		{
			selectedRaiseTo = (long)Math.Round(value);

			UpdateUI();
		}

		private void SetRaiseTo(long target)
		{
			selectedRaiseTo = Math.Max(
				minRaiseTo,
				Math.Min(target, maxRaiseTo)
			);

			if (raiseSlider != null)
			{
				raiseSlider.SetValueWithoutNotify(
					selectedRaiseTo
				);
			}

			UpdateUI();
		}

		private void UpdateUI()
		{
			if (selectedRaiseToText != null)
			{
				selectedRaiseToText.text = "$" + selectedRaiseTo.ToString("N0");
			}

			if (raiseAmountText != null)
			{
				raiseAmountText.text = "$" + selectedRaiseTo.ToString("N0");
			}

			bool valid =
				selectedRaiseTo >= minRaiseTo &&
				selectedRaiseTo <= maxRaiseTo &&
				selectedRaiseTo > tableCurrentBet;

			if (confirmButton != null)
				confirmButton.interactable = valid;
		}

		private void ConfirmRaise()
		{
			if (selectedRaiseTo < minRaiseTo ||
				selectedRaiseTo > maxRaiseTo ||
				selectedRaiseTo <= tableCurrentBet)
			{
				Debug.LogWarning(
					"[POKER] Invalid Raise To amount."
				);

				return;
			}

			Debug.Log(
				"[POKER] Confirm Raise To: " +
				selectedRaiseTo
			);

			OnActionConfirmed?.Invoke(
				"RAISE",
				selectedRaiseTo
			);

			Close();
		}

		public void Close()
		{
			if (popupRoot != null)
				popupRoot.SetActive(false);
		}

		private void OnDestroy()
		{
			if (halfPotButton != null)
				halfPotButton.onClick.RemoveListener(SetHalfPot);

			if (fullPotButton != null)
				fullPotButton.onClick.RemoveListener(SetFullPot);

			if (confirmButton != null)
				confirmButton.onClick.RemoveListener(ConfirmRaise);

			if (closeButton != null)
				closeButton.onClick.RemoveListener(Close);

			if (raiseSlider != null)
				raiseSlider.onValueChanged.RemoveListener(
					OnSliderChanged
				);
		}
	}
}
