using UnityEngine;

public class DepositUI : MonoBehaviour
{
	private DepositConfigResponse config;

	private void Start()
	{
		LoadDepositConfig();
	}

	public void LoadDepositConfig()
	{
		DepositService.Instance.GetDepositConfig(
			result =>
			{
				config = result;

				Debug.Log("Network: " + config.network);
				Debug.Log("Wallet: " + config.walletAddress);
				Debug.Log("Minimum: " + config.minDeposit);

				// Set wallet address text.
				// Set network text.
				// Set minimum amount text.
			},
			error =>
			{
				Debug.LogError(error);
			}
		);
	}

	public void CopyWalletAddress()
	{
		if (config == null)
			return;

		GUIUtility.systemCopyBuffer = config.walletAddress;
	}

	public void Submit(
		string amount,
		string txHash)
	{
		DepositService.Instance.SubmitDeposit(
			amount,
			txHash,
			result =>
			{
				Debug.Log(result.message);

				// Show: Deposit request submitted.
				// Refresh deposit history.
			},
			error =>
			{
				Debug.LogError(error);
			}
		);
	}

	public void LoadHistory()
	{
		DepositService.Instance.GetMyDeposits(
			result =>
			{
				foreach (var deposit in result.deposits)
				{
					Debug.Log(
						$"Deposit: {deposit.amount} | Status: {deposit.status}"
					);
				}
			},
			error =>
			{
				Debug.LogError(error);
			}
		);
	}
}