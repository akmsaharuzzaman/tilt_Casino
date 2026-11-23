
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

using SiteDataClasses;

public class Authentication : MonoBehaviour
{
	public static Authentication Instance { get; private set; }

	// ==========================================
	// EVENTS
	// ==========================================

	[Serializable]
	public class StringEvent : UnityEvent<string> { }

	[Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	[Header("Authentication Events")]

	public UnityEvent onSecurityQuestionsLoaded;
	public UnityEvent onRegisterSuccess;
	public UnityEvent onLoginSuccess;
	public UnityEvent onLogout;

	[Header("User Events")]

	public StringEvent onUserDataUpdated;
	public StringEvent onError;
	public StringEvent onMessage;

	[Header("Loading Event")]

	public BoolEvent onLoadingChanged;

	[Header("Settings")]

	[SerializeField] private string defaultAvatarId = "avatar-1";
	[SerializeField] private bool debugLogs = true;

	// ==========================================
	// DATA
	// ==========================================

	public List<SecurityQuestionData> SecurityQuestions
	{ get; private set; } = new List<SecurityQuestionData>();

	public UserData CurrentUser { get; private set; }
	public Action userUpdated;

	#region Get Datas
	public long CurrentUserAmount => CurrentUser.balance;

	public string CurrentUserName => CurrentUser.username;
	#endregion

	public bool IsLoading { get; private set; }

	public bool IsLoggedIn =>
		APIServices.Instance != null &&
		APIServices.Instance.AvailableOwnToken();

	private bool requestInProgress;

	private readonly ConcurrentQueue<Action> mainThreadActions =
		new ConcurrentQueue<Action>();


	// ==========================================
	// LIFECYCLE
	// ==========================================

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		while (mainThreadActions.TryDequeue(out Action action))
		{
			action?.Invoke();
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	private void Dispatch(Action action)
	{
		mainThreadActions.Enqueue(action);
	}

	// ==========================================
	// REQUEST CONTROL
	// ==========================================

	private bool CanStartRequest()
	{
		if (APIServices.Instance == null)
		{
			RaiseError("APIServices instance not found.");
			return false;
		}

		if (requestInProgress)
		{
			RaiseError("Another authentication request is processing.");
			return false;
		}

		requestInProgress = true;
		SetLoading(true);

		return true;
	}

	private void FinishRequest()
	{
		requestInProgress = false;
		SetLoading(false);
	}

	private void SetLoading(bool value)
	{
		IsLoading = value;
		onLoadingChanged?.Invoke(value);
	}

	private void RaiseError(string message)
	{
		if (debugLogs)
			Debug.LogError("[Authentication] " + message);

		onError?.Invoke(message);
	}

	private void RaiseMessage(string message)
	{
		if (debugLogs)
			Debug.Log("[Authentication] " + message);

		onMessage?.Invoke(message);
	}

	// ==========================================
	// GET SECURITY QUESTIONS
	// ==========================================

	public void GetSecurityQuestions()
	{
		if (!CanStartRequest())
			return;

		APIServices.Instance.EnqueueRequest(
			APIEndPoint.SecurityQuestionsURL,
			"GET",
			null,

			(response, status) =>
			{
				Dispatch(() =>
				{
					FinishRequest();

					try
					{
						SecurityQuestionResponse data =
							JsonUtility.FromJson<SecurityQuestionResponse>(
								response
							);

						if (data == null ||
							!data.success ||
							data.questions == null)
						{
							RaiseError(
								data?.message ??
								"Unable to load security questions."
							);
							return;
						}

						SecurityQuestions.Clear();
						SecurityQuestions.AddRange(data.questions);

						onSecurityQuestionsLoaded?.Invoke();

						RaiseMessage("Security questions loaded.");
					}
					catch (Exception ex)
					{
						RaiseError(ex.Message);
					}
				});
			},

			(error, status) =>
			{
				Dispatch(() =>
				{
					FinishRequest();
					RaiseError(error);
				});
			}
		);
	}

	// ==========================================
	// REGISTER
	// ==========================================

	public void Register(
		string username,
		string pin,
		string securityQuestionId,
		string securityAnswer,
		string avatarId = null
	)
	{
		username = username?.Trim();
		securityAnswer = securityAnswer?.Trim();

		avatarId = string.IsNullOrWhiteSpace(avatarId)
			? defaultAvatarId
			: avatarId.Trim();

		if (!ValidateCredentials(
			username,
			pin,
			securityQuestionId,
			securityAnswer))
			return;

		if (!CanStartRequest())
			return;

		RegisterRequest data = new RegisterRequest
		{
			username = username,
			pin = pin,
			securityQuestionId = securityQuestionId,
			securityAnswer = securityAnswer,
			avatarId = avatarId
		};

		string json = JsonUtility.ToJson(data);

		Debug.Log("[REGISTER JSON] " + json);

		APIServices.Instance.EnqueueRequest(
			APIEndPoint.RegisterURL,
			"POST",
			JsonUtility.ToJson(data),

			(response, status) =>
			{
				Dispatch(() =>
				{
					FinishRequest();
					HandleAuthResponse(response, true);
				});
			},

			(error, status) =>
			{
				Dispatch(() =>
				{
					FinishRequest();
					HandleErrorResponse(error, "Registration failed.");
				});
			}
		);
	}

	// ==========================================
	// LOGIN
	// ==========================================

	public void Login(
		string username,
		string pin,
		string securityQuestionId,
		string securityAnswer
	)
	{
		username = username?.Trim();
		securityAnswer = securityAnswer?.Trim();

		if (!ValidateCredentials(
			username,
			pin,
			securityQuestionId,
			securityAnswer))
			return;

		if (!CanStartRequest())
			return;

		LoginRequest data = new LoginRequest
		{
			username = username,
			pin = pin,
			securityQuestionId = securityQuestionId,
			securityAnswer = securityAnswer
		};

		APIServices.Instance.EnqueueRequest(
			APIEndPoint.LoginURL,
			"POST",
			JsonUtility.ToJson(data),

			(response, status) =>
			{
				Dispatch(() =>
				{
					FinishRequest();
					HandleAuthResponse(response, false);
				});
			},

			(error, status) =>
			{
				Dispatch(() =>
				{
					FinishRequest();
					HandleErrorResponse(error, "Login failed.");
				});
			}
		);
	}

	// ==========================================
	// HANDLE AUTH RESPONSE
	// ==========================================

	private void HandleAuthResponse(
		string response,
		bool isRegister
	)
	{
		try
		{
			AuthResponse data =
				JsonUtility.FromJson<AuthResponse>(response);

			if (data == null ||
				!data.success ||
				string.IsNullOrEmpty(data.token) ||
				data.user == null)
			{
				RaiseError(data?.message ?? "Invalid server response.");
				return;
			}

			APIServices.Instance.UpdateToken(data.token);

			CurrentUser = data.user;

			onUserDataUpdated?.Invoke(
				JsonUtility.ToJson(CurrentUser)
			);

			Debug.Log(JsonUtility.ToJson(CurrentUser));

			if (isRegister)
			{
				onRegisterSuccess?.Invoke();
			}
			else
			{
				onLoginSuccess?.Invoke();
				userUpdated?.Invoke();
			}
			RaiseMessage(data.message ?? "Authentication successful.");
		}
		catch (Exception ex)
		{
			RaiseError("Authentication error: " + ex.Message);
		}
	}

	// ==========================================
	// LOGOUT
	// ==========================================

	public void Logout()
	{
		if (APIServices.Instance != null)
			APIServices.Instance.ClearToken();

		CurrentUser = null;

		onLogout?.Invoke();

		RaiseMessage("Logout successful.");
	}

	// ==========================================
	// USER DATA
	// ==========================================

	public UserData GetCurrentUser()
	{
		return CurrentUser;
	}

	public string GetCurrentUserJSON()
	{
		return CurrentUser == null
			? null
			: JsonUtility.ToJson(CurrentUser);
	}

	public string GetAccessToken()
	{
		return APIServices.Instance?.GetOwnToken();
	}

	// ==========================================
	// VALIDATION
	// ==========================================

	private bool ValidateCredentials(
		string username,
		string pin,
		string questionId,
		string answer
	)
	{
		if (string.IsNullOrWhiteSpace(username) ||
			!Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,30}$"))
		{
			RaiseError("Invalid username.");
			return false;
		}

		if (string.IsNullOrEmpty(pin) ||
			!Regex.IsMatch(pin, @"^\d{4}$"))
		{
			RaiseError("PIN must contain exactly 4 digits.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(questionId))
		{
			RaiseError("Please select a security question.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(answer) ||
			answer.Trim().Length < 2 ||
			answer.Trim().Length > 100)
		{
			RaiseError("Invalid security answer.");
			return false;
		}

		return true;
	}

	// ==========================================
	// ERROR RESPONSE
	// ==========================================

	private void HandleErrorResponse(
		string response,
		string fallbackMessage
	)
	{
		string message = fallbackMessage;

		try
		{
			AuthResponse data =
				JsonUtility.FromJson<AuthResponse>(response);

			if (data != null &&
				!string.IsNullOrEmpty(data.message))
			{
				message = data.message;
			}
		}
		catch
		{
			if (!string.IsNullOrEmpty(response))
				message = response;
		}

		RaiseError(message);
	}
}
