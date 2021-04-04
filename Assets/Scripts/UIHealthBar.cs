using UnityEngine;
using UnityEngine.UI;

sealed class UIHealthBar : MonoBehaviour
{
    #region Parameters
    [SerializeField] private Text textValue;
    [SerializeField] private RectTransform filledHealthPointsBar;

    private float widthMultiplier, currentWidth;

    public static UIHealthBar Instance { get; private set; }
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        Instance = Singleton.GetSingleton(gameObject, Instance);
    }

    private void Start()
    {
        widthMultiplier = filledHealthPointsBar.sizeDelta.x / PlayerController.Instance.MaxHealthPoints;
    }
    #endregion

    #region Custom methods
    internal void UpdateHealthBar(float currentHealth)
    {
        currentWidth = currentHealth * widthMultiplier;
        filledHealthPointsBar.sizeDelta = new Vector2(currentWidth, filledHealthPointsBar.sizeDelta.y);
        textValue.text = $"{currentHealth} / {PlayerController.Instance.MaxHealthPoints}";
    }
    #endregion
}
