
using System;
using UnityEngine;

public class DepositService : MonoBehaviour
{
	public static DepositService Instance { get; private set; }

	private const string DepositConfigPath = "/api/deposits/config";
	private const string DepositRequestPath = "/api/deposits/request";
	private const string MyDepositsPath = "/api/deposits/my";

	private string BaseURL => APIEndPoint.baseURL;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	private string GetToken()
	{
		return APIServices.Instance.GetOwnToken();
	}

	public void GetDepositConfig(
		Action<DepositConfigResponse> onSuccess,
		Action<string> onError)
	{
		APIServices.Instance.EnqueueRequest(
			BaseURL + DepositConfigPath,
			"GET",
			null,
			(response, message) =>
			{
				var result =
					JsonUtility.FromJson<DepositConfigResponse>(response);

				if (result != null && result.success)
					onSuccess?.Invoke(result);
				else
					onError?.Invoke(message ?? "Could not load deposit config.");
			},
			(error, message) =>
			{
				onError?.Invoke(message ?? error);
			},
			GetToken()
		);
	}

	public void SubmitDeposit(
		string amount,
		string txHash,
		Action<CreateDepositResponse> onSuccess,
		Action<string> onError)
	{
		if (string.IsNullOrWhiteSpace(amount) ||
			string.IsNullOrWhiteSpace(txHash))
		{
			onError?.Invoke("Amount and transaction hash are required.");
			return;
		}

		DepositRequestBody body = new DepositRequestBody
		{
			amount = amount.Trim(),
			txHash = txHash.Trim()
		};

		string json = JsonUtility.ToJson(body);

		APIServices.Instance.EnqueueRequest(
			BaseURL + DepositRequestPath,
			"POST",
			json,
			(response, message) =>
			{
				var result =
					JsonUtility.FromJson<CreateDepositResponse>(response);

				if (result != null && result.success)
					onSuccess?.Invoke(result);
				else
					onError?.Invoke(message ?? "Deposit submission failed.");
			},
			(error, message) =>
			{
				onError?.Invoke(message ?? error);
			},
			GetToken()
		);
	}

	public void GetMyDeposits(
		Action<DepositListResponse> onSuccess,
		Action<string> onError)
	{
		APIServices.Instance.EnqueueRequest(
			BaseURL + MyDepositsPath,
			"GET",
			null,
			(response, message) =>
			{
				var result =
					JsonUtility.FromJson<DepositListResponse>(response);

				if (result != null && result.success)
					onSuccess?.Invoke(result);
				else
					onError?.Invoke(message ?? "Could not load deposit history.");
			},
			(error, message) =>
			{
				onError?.Invoke(message ?? error);
			},
			GetToken()
		);
	}
}
