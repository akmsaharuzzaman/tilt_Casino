using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class APIServices : MonoBehaviour
{
    public static APIServices Instance { get; set; }
	public Queue<APIRequest> requestQueue = new Queue<APIRequest>();
	private bool isProcessing = false;

	private int currentProcessing = 0;
	[SerializeField] private int maxConcurrentRequests = 3;

	private const string TOKEN_KEY = "ACCESS_TOKEN";
	private Dictionary<string,string> ownToken = new Dictionary<string, string>();

	private HttpClient httpClient;


	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
			ServicePointManager.SecurityProtocol =
					SecurityProtocolType.Tls12 | SecurityProtocolType.Tls12;

			httpClient = new HttpClient();
			httpClient.Timeout = TimeSpan.FromSeconds(15);
			httpClient.DefaultRequestHeaders.ConnectionClose = false;

			

#if UNITY_EDITOR

				PlayerPrefs.DeleteAll();
			/*if (PlayerPrefs.HasKey("ACCESS_TOKEN"))
			{
				UpdateToken(PlayerPrefs.GetString("ACCESS_TOKEN"));
			}*/
#else
if(Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.WebGLPlayer)
			{
				if (PlayerPrefs.HasKey("ACCESS_TOKEN"))
				{
					UpdateToken(PlayerPrefs.GetString("ACCESS_TOKEN"));
				}
			}
			else
			{
				PlayerPrefs.DeleteAll();
			}
			/*if (PlayerPrefs.HasKey("ACCESS_TOKEN"))
			{
				UpdateToken(PlayerPrefs.GetString("ACCESS_TOKEN"));
			}*/
			//PlayerPrefs.DeleteAll();
			
#endif



		}
		else
		{
			Destroy(gameObject);
		}

	}
	private void Update()
	{
		if (!isProcessing && requestQueue.Count > 0)
		{
			while (currentProcessing < maxConcurrentRequests && requestQueue.Count > 0)
			{
				APIRequest req = requestQueue.Dequeue();
				currentProcessing++;
#if UNITY_WEBGL
				StartCoroutine(ProcessRequest_Webgl(req));
#else
				_ = ProcessRequestAsync(req);
#endif
			}
			//StartCoroutine(ProcessRequest(requestQueue.Dequeue()));
		}
	}

	public string GetOwnToken()
	{
		if (ownToken.TryGetValue(TOKEN_KEY, out string token))
		{
			return token;
		}
		return null;
	}

	public bool AvailableOwnToken()
	{
		if (ownToken.TryGetValue(TOKEN_KEY, out string token))
		{
			return true;
		}
		return false;
	}

	public void UpdateToken(string newToken)
	{
		if (string.IsNullOrEmpty(newToken))
			return;

		if (ownToken.Count > 0)
		{
			ownToken.Remove(TOKEN_KEY);
			ownToken[TOKEN_KEY] = newToken;
		}
		else
		{
			ownToken.Add(TOKEN_KEY, newToken);
		}

		PlayerPrefs.SetString("ACCESS_TOKEN",newToken);
		PlayerPrefs.Save();
	}

	public void ClearToken()
	{
		ownToken.Clear();

		PlayerPrefs.DeleteKey(TOKEN_KEY);
		PlayerPrefs.Save();

		Debug.Log("ACCESS_TOKEN Cleared Successfully");
	}

	public void EnqueueRequest(string url, string method, string jsonBody, Action<string,string> onSuccess, Action<string, string> onError, string accessToken = null)
	{
		Debug.Log("[LOGIN JSON] " + jsonBody);

		APIRequest req = new APIRequest
		{
			url = url,
			method = method,
			jsonBody = jsonBody,
			accessToken = accessToken,
			onSuccess = onSuccess,
			onError = onError
		};
		requestQueue.Enqueue(req);
	}

	private async Task ProcessRequestAsync(APIRequest req)
	{
		isProcessing = true;

		try
		{
			HttpRequestMessage request;

			if (req.method == "POST")
			{
				request = new HttpRequestMessage(HttpMethod.Post, req.url);
				request.Content = new StringContent(
					req.jsonBody ?? "{}",
					Encoding.UTF8,
					"application/json"
				);
			}else if(req.method == "PUT")
			{
				request = new HttpRequestMessage(HttpMethod.Put, req.url);
				request.Content = new StringContent(
					req.jsonBody ?? "{}",
					Encoding.UTF8,
					"application/json"
				);
			}
			else
			{
				request = new HttpRequestMessage(HttpMethod.Get, req.url);
			}

			if (!string.IsNullOrEmpty(req.accessToken))
			{
				request.Headers.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue(
						"Bearer", req.accessToken);
			}

			HttpResponseMessage response = await httpClient.SendAsync(request);
			string responseText = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				req.onError?.Invoke(responseText, response.StatusCode.ToString());
			}
			else
			{
				req.onSuccess?.Invoke(responseText, response.StatusCode.ToString());
			}
		}
		catch (Exception ex)
		{
			req.onError?.Invoke(ex.Message, "");
		}

		finally
		{
			currentProcessing--;
		}

		isProcessing = false;
	}
	private IEnumerator ProcessRequest_Webgl(APIRequest req)
	{
		isProcessing = true;


		UnityWebRequest request;

		if (req.method == "POST")
		{
			request = new UnityWebRequest(req.url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(req.jsonBody ?? "{}");
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
		}
		else if (req.method == "PUT")
		{
			request = new UnityWebRequest(req.url, "PUT");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(req.jsonBody ?? "{}");
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
		}
		else
		{
			request = UnityWebRequest.Get(req.url);
		}

		if (!string.IsNullOrEmpty(req.accessToken))
		{
			request.SetRequestHeader("Authorization", "Bearer " + req.accessToken);
		}

		yield return request.SendWebRequest();

		if (request.result != UnityWebRequest.Result.Success)
		{
			req.onError?.Invoke(request.error, request.responseCode.ToString());
		}
		else
		{
			string responseText = request.downloadHandler.text;
			req.onSuccess?.Invoke(responseText, request.responseCode.ToString());
		}

		isProcessing = false;
		currentProcessing--;
	}

	public void EnqueueMultipartRequest(string url, WWWForm form, Action<string> onSuccess, Action<string> onError)
	{
		StartCoroutine(ProcessMultipartRequest(url, form, onSuccess, onError));
	}

	private IEnumerator ProcessMultipartRequest(string url, WWWForm form, Action<string> onSuccess, Action<string> onError)
	{
		using (UnityWebRequest request = UnityWebRequest.Post(url, form))
		{
			yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
			if (request.result != UnityWebRequest.Result.Success)
#else
		if (request.isNetworkError || request.isHttpError)
#endif
			{
				Debug.LogError("Multipart request failed: " + request.error);	
				onError?.Invoke(request.error);
			}
			else
			{
				onSuccess?.Invoke(request.downloadHandler.text);
			}
		}
	}

	public class APIRequest
	{
		public string url;
		public string method;
		public string jsonBody;
		public string accessToken;
		public Action<string,string> onSuccess;
		public Action<string, string> onError;
	}
}


