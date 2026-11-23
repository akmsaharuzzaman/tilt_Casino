
using MessagePack;
using MessagePack.Resolvers;
using NativeWebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Server : MonoBehaviour
{
	public static Server Instance { get; private set; }

	[Header("Server Settings")]
	[SerializeField] private string serverURL = "ws://127.0.0.1:3001/";

	private WebSocket ws;
	private MessagePackSerializerOptions options;

	private readonly Dictionary<string, Action<JObject>> eventListeners = new();

	private Coroutine pingCoroutine;
	private bool isQuitting;

	public string RoomId { get; private set; }
	public string PlayerId { get; private set; }

	public bool IsConnected =>
		ws != null && ws.State == WebSocketState.Open;

	public bool IsAuthenticated { get; private set; }

	// Transport lifecycle events
	public event Action TransportConnected;
	public event Action TransportDisconnected;

	

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		options = MessagePackSerializerOptions.Standard
			.WithResolver(ContractlessStandardResolver.Instance);
	}

	private async void Start()
	{
		await Connect();
	}

	#region Connection

	private async Task Connect()
	{
		if (isQuitting)
			return;

		try
		{
			ws = new WebSocket(serverURL);

			ws.OnOpen += OnOpen;
			ws.OnMessage += OnMessage;
			ws.OnClose += OnClose;
			ws.OnError += OnError;

			await ws.Connect();
		}
		catch (Exception ex)
		{
			Debug.LogError("[SERVER] Connection failed: " + ex.Message);
		}
	}

	private void OnOpen()
	{
		Debug.Log("[SERVER] WebSocket connected.");

		IsAuthenticated = false;
		PlayerId = null;

		if (pingCoroutine != null)
			StopCoroutine(pingCoroutine);

		pingCoroutine = StartCoroutine(PingPong());

		TransportConnected?.Invoke();
	}

	private void OnClose(WebSocketCloseCode code)
	{
		Debug.LogWarning("[SERVER] Disconnected: " + code);

		IsAuthenticated = false;
		RoomId = null;
		PlayerId = null;

		if (pingCoroutine != null)
		{
			StopCoroutine(pingCoroutine);
			pingCoroutine = null;
		}

		TransportDisconnected?.Invoke();
	}

	private void OnError(string error)
	{
		Debug.LogError("[SERVER] WebSocket Error: " + error);
	}

	public async void Reconnect()
	{
		if (IsConnected)
			return;

		await Connect();
	}

	#endregion

	#region Authentication

	// Called by the individual game after WebSocket connection.
	public async Task Authenticate(
		string userName,
		string avatarId,
		string email,int amount, JSONObject details)
	{
		if (!IsConnected)
		{
			Debug.LogWarning("[SERVER] Cannot authenticate. Not connected.");
			return;
		}

		JSONObject data = new JSONObject();

		data.AddField("connect", true);
		data.AddField("userName", userName);
		data.AddField("avatarId", avatarId);
		data.AddField("email", email);
		data.AddField("amount", amount);
		data.AddField("details", details);

		await SGDSOFT_On("SGDSOFT@Connected", data);

		Debug.Log("[SERVER] Authentication request sent.");
	}

	// Call this only after receiving the backend acknowledgment.
	private void MarkAuthenticated()
	{
		IsAuthenticated = true;

		Debug.Log("[SERVER] Authentication successful.");
	}

	#endregion

	#region Matchmaking

	public async void JoinMatchmaking()
	{
		if (!IsConnected || !IsAuthenticated)
		{
			Debug.LogWarning("[SERVER] Authenticate before matchmaking.");
			return;
		}

		await SGDSOFT_On("SGDSOFT@JOIN", new JSONObject());

		Debug.Log("[SERVER] Matchmaking request sent.");
	}

	public async void LeaveRoom()
	{
		if (!IsConnected)
			return;

		await SGDSOFT_On("SGDSOFT@LEAVE", new JSONObject());

		Debug.Log("[SERVER] Leave room request sent.");
	}

	public async void RoundFinished()
	{
		if (!IsConnected || string.IsNullOrEmpty(RoomId))
			return;

		JSONObject data = new JSONObject();
		data.AddField("roomId", RoomId);

		await SGDSOFT_On("SGDSOFT@ROUND_FINISHED", data);
	}

	#endregion

	#region Event Registration

	public void RegisterEvent(
		string eventName,
		Action<JObject> callback)
	{
		if (string.IsNullOrEmpty(eventName) || callback == null)
			return;

		if (!eventListeners.ContainsKey(eventName))
			eventListeners[eventName] = null;

		eventListeners[eventName] += callback;
	}

	public void UnregisterEvent(
		string eventName,
		Action<JObject> callback)
	{
		if (string.IsNullOrEmpty(eventName) || callback == null)
			return;

		if (!eventListeners.TryGetValue(eventName, out var listeners))
			return;

		listeners -= callback;

		if (listeners == null)
			eventListeners.Remove(eventName);
		else
			eventListeners[eventName] = listeners;
	}

	private void DispatchEvent(string eventName, JObject data)
	{
		if (eventListeners.TryGetValue(eventName, out var listeners))
		{
			try
			{
				listeners?.Invoke(data);
			}
			catch (Exception ex)
			{
				Debug.LogError(
					$"[SERVER] Event callback error ({eventName}): {ex}"
				);
			}
		}
	}

	#endregion

	#region Send Message

	public async Task SGDSOFT_On(
		string eventName,
		JSONObject data)
	{
		if (!IsConnected)
		{
			Debug.LogWarning("[SERVER] WebSocket not connected.");
			return;
		}

		try
		{
			string dataJson = data != null
				? data.ToString()
				: "{}";

			var packet = new Dictionary<string, object>
			{
				{ "SGDSOFT_EventName", eventName },
				{ "SGDSOFT_data", dataJson }
			};

			byte[] serializedData =
				MessagePackSerializer.Serialize<object>(
					packet,
					options
				);

			await ws.Send(serializedData);
		}
		catch (Exception ex)
		{
			Debug.LogError(
				$"[SERVER] Send failed ({eventName}): {ex.Message}"
			);
		}
	}

	#endregion

	#region Receive Message

	private void OnMessage(byte[] bytes)
	{
		try
		{
			string json = MessagePackSerializer.ConvertToJson(bytes);

			Debug.Log(json);

			JObject packet = JObject.Parse(json);

			string eventName =
				packet["SGDSOFT_EventName"]?.ToString();

			string payload =
				packet["SGDSOFT_data"]?.ToString();

			if (string.IsNullOrEmpty(eventName))
			{
				Debug.LogWarning("[SERVER] Received packet without event name.");
				return;
			}

			JObject data = string.IsNullOrEmpty(payload)
				? new JObject()
				: JObject.Parse(payload);

			HandleEvent(eventName, data);
		}
		catch (Exception ex)
		{
			Debug.LogError("[SERVER] Receive error: " + ex);
		}
	}

	private void HandleEvent(string eventName, JObject data)
	{
		// Global server state management

		switch (eventName)
		{
			case "SGDSOFT@Connected":
				PlayerId =
	   data["id"]?.ToString() ??
	   data["playerId"]?.ToString();

				if (string.IsNullOrEmpty(PlayerId))
				{
					Debug.LogError("[SERVER] Player ID missing in Connected response.");
				}
				else
				{
					Debug.Log("[SERVER] Player ID: " + PlayerId);
				}
				MarkAuthenticated();
				break;

			case "SGDSOFT@ROOM_CREATED":
			case "SGDSOFT@ROOM_JOINED":
			case "SGDSOFT@WAITING":
				RoomId = data["roomId"]?.ToString();
				break;

			case "SGDSOFT@ROOM_EMPTY":
				RoomId = null;
				break;
		}

		// Every event is forwarded to registered game controllers.
		DispatchEvent(eventName, data);
	}

	#endregion

	#region Ping Pong

	private IEnumerator PingPong()
	{
		while (IsConnected && !isQuitting)
		{
			var pongTask = PongAsync();

			while (!pongTask.IsCompleted)
				yield return null;

			yield return new WaitForSeconds(5f);
		}

		pingCoroutine = null;
	}

	private async Task PongAsync()
	{
		if (!IsConnected)
			return;

		try
		{
			JSONObject data = new JSONObject();
			data.AddField("time", Time.time);

			string dataJson = data.ToString();

			var packet = new Dictionary<string, object>
			{
				{ "SGDSOFT_EventName", "SGDSOFT@PONG" },
				{ "SGDSOFT_data", dataJson }
			};

			byte[] serializedData =
				MessagePackSerializer.Serialize<object>(
					packet,
					options
				);

			await ws.Send(serializedData);
		}
		catch (Exception ex)
		{
			Debug.LogError("[SERVER] Pong error: " + ex.Message);
		}
	}

	#endregion

	#region Unity Lifecycle

	private void Update()
	{
#if !UNITY_WEBGL || UNITY_EDITOR
		ws?.DispatchMessageQueue();
#endif
	}

	private async void OnDestroy()
	{
		if (Instance != this)
			return;

		isQuitting = true;

		if (pingCoroutine != null)
		{
			StopCoroutine(pingCoroutine);
			pingCoroutine = null;
		}

		if (ws != null && ws.State == WebSocketState.Open)
		{
			try
			{
				await ws.Close();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[SERVER] Close error: " + ex.Message);
			}
		}

		Instance = null;
	}

	#endregion
}
