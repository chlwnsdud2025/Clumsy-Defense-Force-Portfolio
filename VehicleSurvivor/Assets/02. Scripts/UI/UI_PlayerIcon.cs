using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerIcon : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    private PlayerInfo _playerInfo;
    private Action<PlayerInfo> _onSelected;

    public void Initialize(PlayerInfo playerInfo, Action<PlayerInfo> onSelected)
    {
        _playerInfo = playerInfo;
        _onSelected = onSelected;

        if (iconImage != null)
        {
            iconImage.sprite = playerInfo.Icon;

        }

        if (nameText != null)
        {
            nameText.text = playerInfo.CharacterName;

        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _onSelected?.Invoke(_playerInfo);
    }
}