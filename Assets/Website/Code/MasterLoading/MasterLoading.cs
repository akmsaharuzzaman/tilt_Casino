
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MasterLoading : MonoBehaviour
{
    public static MasterLoading Instance { get; private set; }

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Image loadingIcon;

    [Header("Settings")]
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool rotateIcon = true;
    [SerializeField] private float rotationSpeed = 250f;

    private Coroutine loadingCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (hideOnAwake)
            Hide();
    }

    private void Update()
    {
        if (rotateIcon && loadingIcon != null &&
            loadingPanel != null && loadingPanel.activeSelf)
        {
            loadingIcon.transform.Rotate(
                0f,
                0f,
                -rotationSpeed * Time.unscaledDeltaTime
            );
        }
    }

    public void Show(string message = "Loading...")
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        SetMessage(message);
    }

    public void Hide()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void SetMessage(string message)
    {
        if (loadingText != null)
            loadingText.text = message;
    }

    public void ShowForSeconds(float seconds, string message = "Loading...")
    {
        if (loadingCoroutine != null)
            StopCoroutine(loadingCoroutine);

        Show(message);
        loadingCoroutine = StartCoroutine(HideAfter(seconds));
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        Hide();
        loadingCoroutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
