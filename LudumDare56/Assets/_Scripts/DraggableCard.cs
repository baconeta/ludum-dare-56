using System.Collections;
using _Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour
{
    public Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private GameInput gameInputSystem;
    private Canvas cardCanvas;
    private Rect canvasRect;
    private bool isInHandTrigger;
    private CardTrayUIManager cardTrayUIManager;
    private int handSiblingNumber;
    
    public Image cardImage;
    
    [SerializeField] private AudioClip playCardSound;
    [SerializeField] private float playCardVolume = 0.2f;
    [SerializeField] private AudioClip pickupCardSound;
    [SerializeField] private float pickupCardVolume = 0.2f;
    [SerializeField] private AudioClip discardCardSound;
    [SerializeField] private float discardCardVolume = 0.2f;
    
    [FormerlySerializedAs("lerpDuration")] public float lerpDurationHand = 0.1f; // Duration over which the lerp happens
    public float lerpDurationZone = 0.1f;
    private bool isLerping = false; // To check if lerping is happening
    private float elapsedTime;
    private Vector3 endPosition;

    private void OnEnable()
    {
        RaceManager.OnRaceCompleted += OnRaceComplete;
        CardTrayUIManager.OnOpenCardSelection += CardSelectionOpened;
        CardTrayUIManager.OnCloseCardSelection += CardSelectionClosed;
    }
    
    private void CardSelectionOpened()
    {
        if (isDragging)
        {
            isDragging = false;
            ReturnCardToHand();
        }
        GetComponent<EventTrigger>().enabled = false;
        ChangeMainCardAlpha(0.3f);
    }

    private void ChangeMainCardAlpha(float alpha)
    {
        var color = cardImage.color;
        color.a = alpha;
        cardImage.color = color;
    }

    private void CardSelectionClosed()
    {
        GetComponent<EventTrigger>().enabled = true;
        ChangeMainCardAlpha(1f);
    }

    private void OnDisable()
    {
        RaceManager.OnRaceCompleted -= OnRaceComplete;
        CardTrayUIManager.OnOpenCardSelection -= CardSelectionOpened;
        CardTrayUIManager.OnCloseCardSelection -= CardSelectionClosed;
    }

    private void OnRaceComplete()
    {
        if (isDragging)
        {
            ReturnCardToHand();
            isDragging = false;
        }
        GetComponent<EventTrigger>().enabled = true;
        ChangeMainCardAlpha(0.3f);
    }

    // Start is called before the first frame update
    private void Start()
    {
        mainCamera = Camera.main;
        gameInputSystem = FindFirstObjectByType<GameInput>();
        cardCanvas = FindFirstObjectByType<Canvas>();
        canvasRect = cardCanvas.GetComponent<RectTransform>().rect;
        cardTrayUIManager = FindFirstObjectByType<CardTrayUIManager>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (isDragging)
        {
            // While dragging, update the position of the object to follow the mouse
            transform.position = GetMouseWorldPosition() + offset;
        }
    }

    public void MouseDown()
    {
        if (!RaceManager.Instance.HasRaceFinished && RaceManager.Instance.HasRaceStarted)
        {
            // When the mouse is clicked, start dragging
            isDragging = true;
            isInHandTrigger = true;
            handSiblingNumber = transform.GetSiblingIndex();
            gameObject.transform.SetParent(cardCanvas.gameObject.transform, true);
            AudioSystem.Instance.PlaySound(pickupCardSound, pickupCardVolume);
            
            // Calculate offset between object position and mouse position
            offset = transform.position - GetMouseWorldPosition();
            cardTrayUIManager.SetSlowTime();
        }

    }

    public void MouseUp()
    {
        if (!RaceManager.Instance.HasRaceFinished && RaceManager.Instance.HasRaceStarted)
        {
            // When the mouse is released, stop dragging
            isDragging = false;
            cardTrayUIManager.SetNormalTime();

            if (isInHandTrigger)
            {
                // Put the card back into the hand
                ReturnCardToHand();
            }
            else // Now we play the card, first removing the active zone card
            {
                AudioSystem.Instance.PlaySound(playCardSound, playCardVolume);
                cardTrayUIManager.ZoneCardUsed();

                endPosition = cardTrayUIManager.zoneUIPosition.position;
                StartCoroutine(LerpPositionActiveZone());
            }
        }
    }

    private void ReturnCardToHand()
    {
        endPosition = cardTrayUIManager.transform.position;
        StartCoroutine(LerpPositionHand());
    }

    private IEnumerator LerpPositionHand()
    {
        isLerping = true;
        elapsedTime = 0f;

        Vector3 initialPosition = transform.position;

        while (elapsedTime < lerpDurationHand)
        {
            transform.position = Vector3.Lerp(initialPosition, endPosition, elapsedTime / lerpDurationHand);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null; // Wait until next frame
        }
    
        // Ensure the final position is exactly the target position after lerping
        transform.position = endPosition;
        gameObject.transform.SetParent(cardTrayUIManager.transform, false);
        transform.SetSiblingIndex(handSiblingNumber);
        AudioSystem.Instance.PlaySound(discardCardSound, discardCardVolume);
        isLerping = false;
    }
    
    private IEnumerator LerpPositionActiveZone()
    {
        isLerping = true;
        elapsedTime = 0f;

        Vector3 initialPosition = transform.position;
        
        if (GetComponent<CardBase>().useType == CardBase.UseType.Conditional)
        {
            cardTrayUIManager.PlayCard(GetComponent<CardBase>());
        }
        
        while (elapsedTime < lerpDurationZone)
        {
            transform.position = Vector3.Lerp(initialPosition, endPosition, elapsedTime / lerpDurationZone);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null; // Wait until next frame
        }
    
        // Ensure the final position is exactly the target position after lerping
        transform.position = endPosition;
        
        var pos = cardTrayUIManager.zoneUIPosition;
        GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        gameObject.transform.SetParent(pos, true);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.GetComponent<EventTrigger>().enabled = false;

        if (GetComponent<CardBase>().useType == CardBase.UseType.Instant)
        {
            cardTrayUIManager.PlayCard(GetComponent<CardBase>());
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        var mousePoint = gameInputSystem.GetPrimaryPositionScreen;
        if (cardCanvas == null)
        {
            Debug.LogError("No canvas found");
            return new Vector3();
        }

        var mousePosInViewPointCoords = mainCamera.ScreenToViewportPoint(mousePoint);
        mousePosInViewPointCoords.x *= canvasRect.width * cardCanvas.scaleFactor;
        mousePosInViewPointCoords.y *= canvasRect.height * cardCanvas.scaleFactor;

        var mouseScreenPointWithDepth = new Vector3(mousePosInViewPointCoords.x, mousePosInViewPointCoords.y,
            Mathf.Abs(mainCamera.transform.position.z - transform.position.z));

        return mouseScreenPointWithDepth;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Hand"))
        {
            isInHandTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Hand"))
        {
            isInHandTrigger = false;
        }
    }
}