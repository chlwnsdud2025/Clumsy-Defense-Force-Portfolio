using TMPro;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [Header("Interact Prompt UI")]
    [SerializeField] private GameObject interactPromptRoot;
    [SerializeField] private Canvas interactCanvas;
    [SerializeField] private TextMeshProUGUI goldAmountText;
    [SerializeField] private Color goldAffordableColor = Color.white;
    [SerializeField] private Color goldUnaffordableColor = Color.red;


    [Header("Respawn Settings")]
    [Tooltip("체크하면 맵에 기본 배치된 상자로 간주되어, 열린 후에도 다음 페이즈에 리스폰됩니다.")]
    public bool isMapDefaultChest = false;

    private Animator chestAnimator;
    private IEventBus eventBus;
    private Billboard promptBillboard;

    private bool isOpened;
    private bool isPlayerInZone;
    private bool isInteractionEnabled;
    private bool isSubscribed;

    private int RequiredGold
    {
        get
        {
            if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
                return stageRunManager.CurrentChestCost;
            return 100;
        }
    }

    private void Awake()
    {
        chestAnimator = GetComponent<Animator>();

        if (interactPromptRoot != null)
            promptBillboard = interactPromptRoot.GetComponent<Billboard>();

        HidePrompt();
    }

    private void Start() => AssignPromptCamera();

    private void OnEnable()
    {
        TrySubscribeGoldChanged();

        if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
            stageRunManager.OnChestCostChanged += RefreshInteractionState;

        if (isPlayerInZone)
            RefreshInteractionState();
    }

    private void OnDisable()
    {
        UnsubscribeGoldChanged();

        if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
            stageRunManager.OnChestCostChanged -= RefreshInteractionState;
    }

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
        if (isOpened) return;

        bool canAfford = RunManager.TryGetInstance(out RunManager runManager)
            && runManager.CanAfford(RequiredGold);

        isInteractionEnabled = canAfford;

        if (goldAmountText != null)
        {
            goldAmountText.text = RequiredGold.ToString("N0");
            goldAmountText.color = canAfford ? goldAffordableColor : goldUnaffordableColor;
        }
    }

    private void AttemptOpenChest()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return;

        if (!runManager.TrySpendGold(RequiredGold))
            return;

        OpenChest();
    }

    private void OpenChest()
    {
        isOpened = true;
        SoundPlayback.PlaySfx(ESoundId.SFX_Chest_Open);
        HidePrompt();

        if (chestAnimator != null)
            chestAnimator.SetTrigger("PressE");

        if (RewardManager.TryGetInstance(out RewardManager rewardManager))
            rewardManager.BeginChestReward();

        // ==========================================
        // 🌟 [핵심 수정] 맵에 배치된 전용 상자일 때만 매니저에 리스폰 위치를 보고합니다!
        // ==========================================
        if (isMapDefaultChest)
        {
            if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
                stageRunManager.HandleChestOpened(transform.position, transform.rotation);
        }

        // 상자가 열리는 애니메이션 연출이 끝날 수 있도록 1.5초 후에 오브젝트를 파괴(제거)합니다.
        Destroy(gameObject, 1.5f);
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