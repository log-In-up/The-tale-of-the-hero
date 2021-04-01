using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using static UnityEngine.RectTransformUtility;
using static UnityEngine.Input;

[RequireComponent(typeof(Image))]
sealed class JoystickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    #region Parameters
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] RectTransform stick = null;

    private Image joystick, joystickController;
    private Vector2 joystickBias;
    public static JoystickController Instance { get; private set; }
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        #region Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        #endregion

        joystick = GetComponent<Image>();
        joystickController = stick.GetComponent<Image>();
    }
    #endregion

    #region Custom methods
    public void OnDrag(PointerEventData eventData)
    {
        if (ScreenPointToLocalPointInRectangle(joystick.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 position))
        {
            position.x /= joystick.rectTransform.sizeDelta.x;

            joystickBias = new Vector2(position.x, 0.0f);
            joystickBias = (joystickBias.magnitude > 1.0f) ? joystickBias.normalized : joystickBias;

            joystickController.rectTransform.anchoredPosition = new Vector2(joystickBias.x * (joystick.rectTransform.sizeDelta.x / 2.0f), 0.0f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        joystickBias = Vector2.zero;
        joystickController.rectTransform.anchoredPosition = Vector2.zero;
    }

    internal float Horizontal()
    {
        if (joystickBias.x != 0)
        {
            return joystickBias.x;
        }
        else
        { 
            return GetAxisRaw(horizontalAxis);
        }
    }
    #endregion
}