using UnityEngine;
using TMPro; 

public class TreasureChest : MonoBehaviour
{
    [Header("Chest Settings")]
    [Tooltip("상자를 여는 데 필요한 골드(비용)")]
    public int requiredGold = 100; // 나중에 연동

    [Header("3D UI Reference")]
    [Tooltip("상자 위에 띄워줄 3D TextMeshPro 오브젝트를 연결하세요.")]
    public TextMeshPro interactText;
    public TextMeshPro requiredGoldText;

    private Animator chestAnimator; // [핵심 추가] 애니메이터 레퍼런스 변수

    private bool isOpened = false;
    private bool isPlayerInZone = false;

    private void Start()
    {
        // 시작할 때는 상호작용 텍스트를 꺼둡니다.
        if (interactText != null && requiredGoldText != null)
        {
            // 비용을 텍스트에 미리 세팅해 둡니다
            interactText.text = "Press E to Open";
            requiredGoldText.text = $"${requiredGold}";

            interactText.gameObject.SetActive(false);
            requiredGoldText.gameObject.SetActive(false);
        }

        chestAnimator = GetComponent<Animator>();

    }

    private void Update()
    {
        // 상자가 안 열렸고, 플레이어가 범위 안에 있고, E키를 눌렀을 때
        if (!isOpened && isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            AttemptOpenChest();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isOpened && other.CompareTag("Player"))
        {
            isPlayerInZone = true;

            // 플레이어가 다가오면 3D 텍스트를 켭니다!
            if (interactText != null) interactText.gameObject.SetActive(true);
            if (requiredGoldText != null) requiredGoldText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;

            // 플레이어가 멀어지면 3D 텍스트를 끕니다!
            if (interactText != null) interactText.gameObject.SetActive(false);
            if (requiredGoldText != null) requiredGoldText.gameObject.SetActive(false);

        }
    }

    private void AttemptOpenChest()
    {
        // TODO:  현재 소지 골드를 확인하고 차감 돈없으면 안열리게 구현
        
        OpenChest();
    }

    private void OpenChest()
    {
        Debug.Log($"상자를 열었습니다! (소모 비용: {requiredGold}G)");

        isOpened = true;

        // 상자가 열렸으니 더 이상 텍스트가 보일 필요가 없습니다.
        if (interactText != null) interactText.gameObject.SetActive(false);

        if (requiredGoldText != null) requiredGoldText.gameObject.SetActive(false);

        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("PressE");
        }

        if (RewardManager.TryGetInstance(out RewardManager rewardManager))
            rewardManager.BeginChestReward();
    }
}