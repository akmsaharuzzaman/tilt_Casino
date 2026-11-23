
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SiteDataClasses;
using TMPro;

public class AuthenticationUI : MonoBehaviour
{
	[Header("Authentication Panels")]
	[SerializeField] private GameObject loginPanel;
	[SerializeField] private GameObject registerPanel;
	[SerializeField] private GameObject mainMenuPanel;

	[Header("Login UI")]
	[SerializeField] private TMP_InputField loginUsername;
	[SerializeField] private TMP_InputField loginPin;
	[SerializeField] private TMP_Dropdown loginSecurityQuestion;
	[SerializeField] private TMP_InputField loginSecurityAnswer;
	[SerializeField] private TextMeshProUGUI loginMessage;
	[SerializeField] private Button loginButton;

	[Header("Register UI")]
	[SerializeField] private TMP_InputField registerUsername;
	[SerializeField] private TMP_InputField registerPin;
	[SerializeField] private TMP_InputField confirmPin;
	[SerializeField] private TMP_Dropdown registerSecurityQuestion;
	[SerializeField] private TMP_InputField registerSecurityAnswer;
	//[SerializeField] private TMP_Dropdown avatarDropdown;
	[SerializeField] private TextMeshProUGUI registerMessage;
	[SerializeField] private Button registerButton;

	[Header("Main Menu UI")]
	[SerializeField] private TextMeshProUGUI usernameText;
	[SerializeField] private TextMeshProUGUI balanceText;

	[Header("Avatar Configuration")]
	[SerializeField]
	private List<string> avatarIds = new List<string>()
	{
		"avatar_01",
		"avatar_02",
		"avatar_03",
		"avatar_04"
	};

	private Authentication auth;

	private bool questionsLoaded;
	private bool isLoading;

	// ----------------------------------------
	// INITIALIZATION
	// ----------------------------------------

	private void Start()
	{
		auth = Authentication.Instance;

		if (auth == null)
		{
			Debug.LogError(
				"Authentication instance not found!"
			);
			return;
		}

		SubscribeEvents();

		SetupInputFields();
		//SetupAvatarDropdown();

		ShowLogin();

		auth.GetSecurityQuestions();

		// A locally stored token does not restore
		// the user profile without a /me endpoint.
	}

	private void OnDestroy()
	{
		UnsubscribeEvents();
	}

	private void SubscribeEvents()
	{
		auth.onSecurityQuestionsLoaded.AddListener(
			OnQuestionsLoaded
		);

		auth.onRegisterSuccess.AddListener(
			OnRegisterSuccess
		);

		auth.onLoginSuccess.AddListener(
			OnLoginSuccess
		);

		auth.onLogout.AddListener(
			OnLogout
		);

		auth.onError.AddListener(
			OnError
		);

		auth.onLoadingChanged.AddListener(
			HandleLoading
		);
	}

	private void UnsubscribeEvents()
	{
		if (auth == null)
			return;

		auth.onSecurityQuestionsLoaded.RemoveListener(
			OnQuestionsLoaded
		);

		auth.onRegisterSuccess.RemoveListener(
			OnRegisterSuccess
		);

		auth.onLoginSuccess.RemoveListener(
			OnLoginSuccess
		);

		auth.onLogout.RemoveListener(
			OnLogout
		);

		auth.onError.RemoveListener(
			OnError
		);

		auth.onLoadingChanged.RemoveListener(
			HandleLoading
		);
	}

	// ----------------------------------------
	// INPUT FIELD SETUP
	// ----------------------------------------

	private void SetupInputFields()
	{
		if (loginPin != null)
			loginPin.contentType = TMP_InputField.ContentType.Pin;

		if (registerPin != null)
			registerPin.contentType = TMP_InputField.ContentType.Pin;

		if (confirmPin != null)
			confirmPin.contentType = TMP_InputField.ContentType.Pin;
	}

	/*private void SetupAvatarDropdown()
	{
		if (avatarDropdown == null)
			return;

		avatarDropdown.ClearOptions();

		avatarDropdown.AddOptions(avatarIds);
	}*/

	// ----------------------------------------
	// PANEL NAVIGATION
	// ----------------------------------------

	public void ShowLogin()
	{
		SetActivePanel(loginPanel);
		ClearMessages();
	}

	public void ShowRegister()
	{
		SetActivePanel(registerPanel);
		ClearMessages();
	}

	private void ShowMainMenu()
	{
		SetActivePanel(mainMenuPanel);
		UpdateUserInfo();
	}

	private void SetActivePanel(GameObject target)
	{
		if (loginPanel != null)
			loginPanel.SetActive(target == loginPanel);

		if (registerPanel != null)
			registerPanel.SetActive(target == registerPanel);

		if (mainMenuPanel != null)
			mainMenuPanel.SetActive(target == mainMenuPanel);
	}

	// ----------------------------------------
	// SECURITY QUESTIONS
	// ----------------------------------------

	private void OnQuestionsLoaded()
	{
		questionsLoaded = true;

		List<string> options = new List<string>();

		foreach (SecurityQuestionData question in auth.SecurityQuestions)
		{
			options.Add(question.question);
		}

		if (loginSecurityQuestion != null)
		{
			loginSecurityQuestion.ClearOptions();
			loginSecurityQuestion.AddOptions(options);
		}

		if (registerSecurityQuestion != null)
		{
			registerSecurityQuestion.ClearOptions();
			registerSecurityQuestion.AddOptions(options);
		}
	}

	private string GetSelectedQuestionId(TMP_Dropdown dropdown)
	{
		if (!questionsLoaded ||
			auth.SecurityQuestions == null ||
			auth.SecurityQuestions.Count == 0 ||
			dropdown == null)
		{
			return null;
		}

		int index = dropdown.value;

		if (index < 0 ||
			index >= auth.SecurityQuestions.Count)
		{
			return null;
		}

		return auth.SecurityQuestions[index].id;
	}

	// ----------------------------------------
	// LOGIN
	// ----------------------------------------

	public void Login()
	{
		if (isLoading)
			return;

		ClearMessages();

		if (loginUsername == null ||
			loginPin == null ||
			loginSecurityAnswer == null ||
			loginSecurityQuestion == null)
		{
			SetMessage(loginMessage, "Login UI is not configured.");
			return;
		}

		string username = loginUsername.text.Trim();
		string pin = loginPin.text;
		string questionId = GetSelectedQuestionId(
			loginSecurityQuestion
		);
		string answer = loginSecurityAnswer.text.Trim();

		if (string.IsNullOrEmpty(username) ||
			string.IsNullOrEmpty(pin) ||
			string.IsNullOrEmpty(questionId) ||
			string.IsNullOrEmpty(answer))
		{
			SetMessage(
				loginMessage,
				"Please fill in all fields."
			);
			return;
		}

		auth.Login(
			username,
			pin,
			questionId,
			answer
		);
	}

	// ----------------------------------------
	// REGISTER
	// ----------------------------------------

	public void Register()
	{
		if (isLoading)
			return;

		ClearMessages();

		if (registerUsername == null ||
			registerPin == null ||
			confirmPin == null ||
			registerSecurityQuestion == null ||
			registerSecurityAnswer == null)
		{
			SetMessage(
				registerMessage,
				"Register UI is not configured."
			);
			return;
		}

		string username = registerUsername.text.Trim();
		string pin = registerPin.text;
		string confirm = confirmPin.text;

		string questionId = GetSelectedQuestionId(
			registerSecurityQuestion
		);

		string answer = registerSecurityAnswer.text.Trim();

		if (string.IsNullOrEmpty(username) ||
			string.IsNullOrEmpty(pin) ||
			string.IsNullOrEmpty(confirm) ||
			string.IsNullOrEmpty(questionId) ||
			string.IsNullOrEmpty(answer))
		{
			SetMessage(
				registerMessage,
				"Please fill in all fields."
			);
			return;
		}

		if (pin != confirm)
		{
			SetMessage(
				registerMessage,
				"PIN does not match."
			);
			return;
		}

		/*if (avatarIds == null ||
			avatarIds.Count == 0 ||
			avatarDropdown.value < 0 ||
			avatarDropdown.value >= avatarIds.Count)
		{
			SetMessage(
				registerMessage,
				"Please select a valid avatar."
			);
			return;
		}*/

		string avatarId = avatarIds[0];

		auth.Register(
			username,
			pin,
			questionId,
			answer,
			avatarId
		);
	}

	// ----------------------------------------
	// AUTHENTICATION EVENTS
	// ----------------------------------------

	private void OnRegisterSuccess()
	{
		SetMessage(registerMessage, "Registration Success Completed");

		ShowMainMenu();
	}

	private void OnLoginSuccess()
	{
		SetMessage(loginMessage, "Login Success ");

		ShowMainMenu();
	}

	private void OnLogout()
	{
		ShowLogin();
	}

	private void OnError(string message)
	{
		if (registerPanel != null && registerPanel.activeSelf)
			SetMessage(registerMessage, message);
		else
			SetMessage(loginMessage, message);
	}

	// ----------------------------------------
	// MASTER LOADING
	// ----------------------------------------

	private void HandleLoading(bool loading)
	{
		isLoading = loading;

		if (MasterLoading.Instance != null)
		{
			if (loading)
				MasterLoading.Instance.Show("Please wait...");
			else
				MasterLoading.Instance.Hide();
		}

		if (loginButton != null)
			loginButton.interactable = !loading;

		if (registerButton != null)
			registerButton.interactable = !loading;
	}

	// ----------------------------------------
	// USER INFORMATION
	// ----------------------------------------

	private void UpdateUserInfo()
	{
		if (auth == null)
			return;

		UserData user = auth.CurrentUser;

		if (user == null)
			return;

		if (usernameText != null)
			usernameText.text = user.username;

		if (balanceText != null)
			balanceText.text = user.balance.ToString("N0");
	}

	// ----------------------------------------
	// LOGOUT
	// ----------------------------------------

	public void Logout()
	{
		if (auth == null)
			return;

		auth.Logout();
	}

	// ----------------------------------------
	// UI HELPERS
	// ----------------------------------------

	private void ClearMessages()
	{
		SetMessage(loginMessage, "");
		SetMessage(registerMessage, "");
	}

	private void SetMessage(TextMeshProUGUI target, string message)
	{
		if (target != null)
			target.text = message;
	}
}
