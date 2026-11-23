using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class ExternalWebGLLoader : MonoBehaviour
{
	[Header("External WebGL Settings")]
	[SerializeField]
	private string externalGamePath =
		"Games/ExternalGame/index.html";

	[Header("UI Settings")]
	[SerializeField]
	private RectTransform targetPanel;

	[SerializeField]
	private Canvas targetCanvas;

	[Header("Loading Settings")]
	[SerializeField]
	private bool loadOnStart = true;

	[SerializeField]
	private bool hideWhenDisabled = true;

#if UNITY_WEBGL && !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern void ExternalGame_Create(string url);

    [DllImport("__Internal")]
    private static extern void ExternalGame_SetRect(
        float x,
        float y,
        float width,
        float height
    );

    [DllImport("__Internal")]
    private static extern void ExternalGame_SetVisible(bool visible);

    [DllImport("__Internal")]
    private static extern void ExternalGame_Destroy();

#endif

	private bool isCreated;

	private void Start()
	{
		if (targetPanel == null)
			targetPanel = GetComponent<RectTransform>();

		if (targetCanvas == null)
			targetCanvas = GetComponentInParent<Canvas>();

		if (loadOnStart)
			LoadGame();
	}

	private void LateUpdate()
	{
		if (!isCreated)
			return;

		UpdateIframePosition();
	}

	public void LoadGame()
	{
#if UNITY_WEBGL && !UNITY_EDITOR

        if (isCreated)
            return;

        if (targetPanel == null || targetCanvas == null)
        {
            Debug.LogError(
                "ExternalWebGLLoader: Panel or Canvas is missing."
            );
            return;
        }

        ExternalGame_Create(externalGamePath);

        isCreated = true;

        ExternalGame_SetVisible(true);

        UpdateIframePosition();

        Debug.Log("External WebGL Game Loaded: " + externalGamePath);

#else

		Debug.Log(
			"External WebGL Game only works in a WebGL build."
		);

#endif
	}

	private void UpdateIframePosition()
	{
#if UNITY_WEBGL && !UNITY_EDITOR

        if (!isCreated)
            return;

        Camera uiCamera = null;

        if (targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = targetCanvas.worldCamera;

            if (uiCamera == null)
                return;
        }

        Vector3[] corners = new Vector3[4];

        targetPanel.GetWorldCorners(corners);

        Vector2 bottomLeft =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[0]
            );

        Vector2 topRight =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[2]
            );

        float x = bottomLeft.x / Screen.width;

        float y = 1f - (topRight.y / Screen.height);

        float width =
            (topRight.x - bottomLeft.x) / Screen.width;

        float height =
            (topRight.y - bottomLeft.y) / Screen.height;

        ExternalGame_SetRect(x, y, width, height);

#endif
	}

	public void ShowGame()
	{
#if UNITY_WEBGL && !UNITY_EDITOR

        if (isCreated)
            ExternalGame_SetVisible(true);

#endif
	}

	public void HideGame()
	{
#if UNITY_WEBGL && !UNITY_EDITOR

        if (isCreated)
            ExternalGame_SetVisible(false);

#endif
	}

	private void OnEnable()
	{
#if UNITY_WEBGL && !UNITY_EDITOR

        if (isCreated)
            ExternalGame_SetVisible(true);

#endif
	}

	private void OnDisable()
	{
#if UNITY_WEBGL && !UNITY_EDITOR

        if (hideWhenDisabled && isCreated)
            ExternalGame_SetVisible(false);

#endif
	}

	private void OnDestroy()
	{
#if UNITY_WEBGL && !UNITY_EDITOR

        if (isCreated)
        {
            ExternalGame_Destroy();
            isCreated = false;
        }

#endif
	}
}