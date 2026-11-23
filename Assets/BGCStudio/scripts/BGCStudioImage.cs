
using UnityEngine;
using UnityEngine.UI;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class BGCStudioImage : MonoBehaviour
{
	[Header("BGCStudio Image Settings")]
	[SerializeField] private string imagePath = "Image/logo.png";

	[SerializeField] private bool loadOnStart = true;

	[SerializeField] private bool preserveAspect = true;

	private MasterRemoteImageLoader loader;
	private Image targetImage;

#if UNITY_EDITOR
	[Header("Editor Preview")]
	[SerializeField] private bool showEditorPreview = true;

	[System.NonSerialized] private Sprite previewSprite;
	[System.NonSerialized] private Texture2D previewTexture;
	[System.NonSerialized] private Sprite originalSprite;
	[System.NonSerialized] private string lastPreviewPath;
#endif

	private void Awake()
	{
		targetImage = GetComponent<Image>();

		if (Application.isPlaying)
		{
			InitializeRuntime();
		}
#if UNITY_EDITOR
		else
		{
			RefreshPreview();
		}
#endif
	}

	private void OnEnable()
	{
		targetImage = GetComponent<Image>();

#if UNITY_EDITOR
		if (!Application.isPlaying)
			SchedulePreview();
#endif
	}

	private void InitializeRuntime()
	{
		if (targetImage == null)
			targetImage = GetComponent<Image>();

		if (loader == null)
			loader = GetComponent<MasterRemoteImageLoader>();

		if (loader == null)
			loader = gameObject.AddComponent<MasterRemoteImageLoader>();

		loader.SetPreserveAspect(preserveAspect);
		loader.SetLoadOnStart(false);
	}

	private void Start()
	{
		if (Application.isPlaying)
		{
			LoadImage();
		}
	}

#if UNITY_EDITOR

	private void OnValidate()
	{
		if (targetImage == null)
			targetImage = GetComponent<Image>();

		if (targetImage != null)
			targetImage.preserveAspect = preserveAspect;

		if (!Application.isPlaying && isActiveAndEnabled)
			SchedulePreview();
	}

	private void SchedulePreview()
	{
		EditorApplication.delayCall -= RefreshPreview;
		EditorApplication.delayCall += RefreshPreview;
	}

	private void RefreshPreview()
	{
		if (this == null || Application.isPlaying)
			return;

		if (targetImage == null)
			targetImage = GetComponent<Image>();

		if (targetImage == null)
			return;

		targetImage.preserveAspect = preserveAspect;

		if (!showEditorPreview)
		{
			ClearPreview();
			return;
		}

		if (string.IsNullOrWhiteSpace(imagePath))
		{
			return;
		}

		if (lastPreviewPath == imagePath && previewSprite != null)
		{
			return;
		}

		string fullPath = Path.Combine(
			Application.streamingAssetsPath,
			imagePath.Replace('/', Path.DirectorySeparatorChar)
		);

		if (!File.Exists(fullPath))
		{
			Debug.LogWarning(
				"BGCStudio Preview: Image not found!\n" + fullPath,
				this
			);
			return;
		}

		try
		{
			byte[] imageBytes = File.ReadAllBytes(fullPath);

			Texture2D newTexture = new Texture2D(
				2,
				2,
				TextureFormat.RGBA32,
				false
			);

			if (!newTexture.LoadImage(imageBytes))
			{
				DestroyImmediate(newTexture);
				Debug.LogError("BGCStudio: Could not load preview image!", this);
				return;
			}

			newTexture.hideFlags = HideFlags.HideAndDontSave;

			Sprite newSprite = Sprite.Create(
				newTexture,
				new Rect(0, 0, newTexture.width, newTexture.height),
				new Vector2(0.5f, 0.5f)
			);

			newSprite.hideFlags = HideFlags.HideAndDontSave;

			if (previewSprite == null)
				originalSprite = targetImage.sprite;

			ClearPreview();

			previewTexture = newTexture;
			previewSprite = newSprite;

			targetImage.sprite = previewSprite;
			targetImage.preserveAspect = preserveAspect;

			lastPreviewPath = imagePath;

			EditorUtility.SetDirty(targetImage);

			EditorApplication.QueuePlayerLoopUpdate();
			SceneView.RepaintAll();

			/*Debug.Log(
				"BGCStudio: Preview loaded - " + imagePath,
				this
			);*/
		}
		catch (System.Exception exception)
		{
			Debug.LogError(
				"BGCStudio Preview Error: " + exception.Message,
				this
			);
		}
	}

	private void ClearPreview()
	{
		if (targetImage != null && targetImage.sprite == previewSprite)
			targetImage.sprite = originalSprite;

		if (previewSprite != null)
		{
			DestroyImmediate(previewSprite);
			previewSprite = null;
		}

		if (previewTexture != null)
		{
			DestroyImmediate(previewTexture);
			previewTexture = null;
		}

		lastPreviewPath = null;
	}

	[ContextMenu("Refresh Image Preview")]
	public void RefreshImagePreview()
	{
		lastPreviewPath = null;
		SchedulePreview();
	}

	private void OnDestroy()
	{
		EditorApplication.delayCall -= RefreshPreview;

		if (!Application.isPlaying)
			ClearPreview();
	}

#endif

	public void LoadImage()
	{
		this.GetComponent<Image>().enabled = false;

		if (!Application.isPlaying)
			return;

		if (loader == null)
			InitializeRuntime();

		if (loader != null)
			loader.SetImagePath(imagePath);
	}

	public void SetImage(string path)
	{
		imagePath = path;

#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			lastPreviewPath = null;
			SchedulePreview();
			return;
		}
#endif

		if (loader != null)
		{
			loader.SetImagePath(imagePath);
		}
	}

	public void SetPreserveAspect(bool value)
	{
		preserveAspect = value;

		if (targetImage == null)
			targetImage = GetComponent<Image>();

		if (targetImage != null)
			targetImage.preserveAspect = value;

		if (loader != null)
			loader.SetPreserveAspect(value);
	}
}
