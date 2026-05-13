using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterSelect : UIScene
{
    [Header("Data")]
    [SerializeField] private PlayerDatabase playerDatabase;

    [Header("Icon List")]
    [SerializeField] private UI_PlayerIcon playerIconPrefab;
    [SerializeField] private Transform content;

    [Header("Right Info Panel")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private PlayerInfo _selectedPlayer;
    private bool _isInitialized;

    public override void OnShow()
    {
        base.OnShow();

        if (_isInitialized)
        {
            return;

        }

        _isInitialized = InitItems();

        if (_isInitialized && playerDatabase.Players.Count > 0)
        {
            SelectPlayer(playerDatabase.Players[0]);
        }
    }

    private bool InitItems()
    {
        if (playerDatabase == null)
        {
            Debug.LogError("[UI_CharacterSelect] PlayerDatabase가 할당되지 않았습니다.");
            return false;
        }

        if (playerIconPrefab == null)
        {
            Debug.LogError("[UI_CharacterSelect] PlayerIconPrefab이 할당되지 않았습니다.");
            return false;
        }

        if (content == null)
        {
            Debug.LogError("[UI_CharacterSelect] Content가 할당되지 않았습니다.");
            return false;
        }

        foreach (PlayerInfo playerInfo in playerDatabase.Players)
        {
            UI_PlayerIcon icon = Instantiate(playerIconPrefab, content);
            icon.Initialize(playerInfo, SelectPlayer);
        }

        return true;
    }

    private void SelectPlayer(PlayerInfo playerInfo)
    {
        _selectedPlayer = playerInfo;
        UpdateInfoPanel(playerInfo);
    }

    private void UpdateInfoPanel(PlayerInfo playerInfo)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = playerInfo.Portrait;
        }

        if (nameText != null)
        {
            nameText.text = playerInfo.CharacterName;
        }

        if (hpText != null)
        {
            hpText.text = $"HP : {playerInfo.MaxHp}";
        }

        if (speedText != null)
        {
            speedText.text = $"Speed : {playerInfo.MoveSpeed}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = playerInfo.Description;
        }
    }
    public void OnClickStartGame()
    {
        if (_selectedPlayer == null)
        {
            Debug.LogWarning("[UI_CharacterSelect] 선택된 캐릭터가 없습니다.");
            return;
        }

        TitleSceneManager.Instance.RequestStartGame();
    }
}