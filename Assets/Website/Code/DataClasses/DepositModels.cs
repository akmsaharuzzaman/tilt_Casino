
using System;

[Serializable]
public class DepositConfigResponse
{
	public bool success;
	public string asset;
	public string network;
	public int chainId;
	public string contract;
	public string walletAddress;
	public string minDeposit;
	public int decimals;
}

[Serializable]
public class DepositRequestBody
{
	public string amount;
	public string txHash;
}

[Serializable]
public class DepositItem
{
	public string id;
	public string asset;
	public string network;
	public string amount;
	public string tx_hash;
	public string status;
	public string rejection_reason;
	public string requested_at;
	public string reviewed_at;
}

[Serializable]
public class CreateDepositResponse
{
	public bool success;
	public string message;
	public DepositItem deposit;
}

[Serializable]
public class DepositListResponse
{
	public bool success;
	public DepositItem[] deposits;
}
