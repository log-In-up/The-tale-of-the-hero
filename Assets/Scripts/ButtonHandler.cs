using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(Image))]
public class ButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    #region Parameters
    [SerializeField] private Color pressedButtonColor = Color.gray;

    internal bool isPressed = false;
    private Color normalColor;
    private Image image = null;
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        image = GetComponent<Image>();
        normalColor = image.color;
    }
    #endregion

    #region Custom methods
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        image.color = pressedButtonColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        image.color = normalColor;
    }
    #endregion
}
