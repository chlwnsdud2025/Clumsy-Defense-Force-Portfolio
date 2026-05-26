using TMPro;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private int requiredGold = 100;

    [Header("Interact Prompt UI")]
    [SerializeField] private GameObject interactPromptRoot;
    [SerializeField] private Canvas interactCanvas;
    [SerializeField] private TextMeshProUGUI goldAmountText;
    [SerializeField] private Color goldAffordableColor = Color.white;
    [SerializeField] private Color goldUnaffordableColor = Color.red;

    private Animator chestAnimator;
    private IEventBus eventBus;
    private Billboard promptBillboard;

    private bool isOpened;
    private bool isPlayerInZone;
    private bool isInteractionEnabled;
    private bool isSubscribed;

    private void Awake()
    {
        chestAnimator = GetComponent<Animator>();

        if (interactPromptRoot != null)
            promptBillboard = interactPromptRoot.GetComponent<Billboard>();

        if (goldAmountText != null)
            goldAmountText.text = requiredGold.ToString("N0");

        HidePrompt();
    }

    private void Start() => AssignPromptCamera();

    private void OnEnable()
    {
        TrySubscribeGoldChanged();
        if (isPlayerInZone)
            RefreshInteractionState();
    }

    private void OnDisable() => UnsubscribeGoldChanged();

    private void Update()
    {
        if (isOpened || !isPlayerInZone || !isInteractionEnabled)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            AttemptOpenChest();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened || !other.CompareTag("Player"))
            return;

        isPlayerInZone = true;
        ShowPrompt();
        RefreshInteractionState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInZone = false;
        HidePrompt();
    }

    private void ShowPrompt()
    {
        TrySubscribeGoldChanged();
        AssignPromptCamera();

        if (interactPromptRoot != null)
            interactPromptRoot.SetActive(true);
    }

    private void HidePrompt()
    {
        if (interactPromptRoot != null)
            interactPromptRoot.SetActive(false);
    }

    private void AssignPromptCamera()
    {
        if (interactCanvas == null)
            return;

        Camera cam = promptBillboard != null ? promptBillboard.TargetCamera : null;
        if (cam == null)
            cam = Camera.main;

        interactCanvas.worldCamera = cam;
    }

    private void RefreshInteractionState()
    {
        bool canAfford = RunManager.TryGetInstance(out RunManager runManager)
            && runManager.CanAfford(requiredGold);

        isInteractionEnabled = canAfford;

        if (goldAmountText != null)
            goldAmountText.color = canAfford ? goldAffordableColor : goldUnaffordableColor;
    }

    private void AttemptOpenChest()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return;

        if (!runManager.TrySpendGold(requiredGold))
            return;

        OpenChest();
    }

    private void OpenChest()
    {
        isOpened = true;
        HidePrompt();

        if (chestAnimator != null)
            chestAnimator.SetTrigger("PressE");

        if (RewardManager.TryGetInstance(out RewardManager rewardManager))
            rewardManager.BeginChestReward();
    }

    private void TrySubscribeGoldChanged()
    {
        if (isSubscribed || !ServiceLocator.TryGet(out IEventBus bus))
            return;

        eventBus = bus;
        eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        isSubscribed = true;
    }

    private void UnsubscribeGoldChanged()
    {
        if (!isSubscribed || eventBus == null)
            return;

        eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        isSubscribed = false;
    }

    private void OnGoldChanged(GoldChangedEvent _)
    {
        if (isPlayerInZone && !isOpened)
            RefreshInteractionState();
    }
}
