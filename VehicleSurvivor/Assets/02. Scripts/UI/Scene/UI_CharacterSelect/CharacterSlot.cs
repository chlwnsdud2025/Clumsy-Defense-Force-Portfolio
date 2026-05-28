using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterSlot : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private GameObject _focusObject;
    [SerializeField] private Button _button;

    private CharacterDefinitionSO _characterDefinition;

    public CharacterDefinitionSO CharacterDefinition => _characterDefinition;
    public bool HasCharacter => _characterDefinition != null;
    public event Action<CharacterSlot> Clicked;

    private void Awake()
    {
        RegisterButtonClick();
    }

    private void OnDestroy()
    {
        UnregisterButtonClick();
    }

    public void Bind(CharacterDefinitionSO characterDefinition)
    {
        _characterDefinition = characterDefinition;

        if (_iconImage != null)
            _iconImage.sprite = _characterDefinition != null ? _characterDefinition.Icon : null;

        if (_button != null)
            _button.interactable = HasCharacter;

        SetSelected(false);
        gameObject.SetActive(_characterDefinition != null);
    }

    public void Clear()
    {
        _characterDefinition = null;

        if (_iconImage != null)
            _iconImage.sprite = null;

        if (_button != null)
            _button.interactable = false;

        SetSelected(false);
        gameObject.SetActive(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (_focusObject != null)
            _focusObject.SetActive(isSelected && HasCharacter);
    }

    private void RegisterButtonClick()
    {
        if (_button == null)
            return;

        _button.onClick.RemoveListener(HandleButtonClicked);
        _button.onClick.AddListener(HandleButtonClicked);
    }

    private void UnregisterButtonClick()
    {
        if (_button == null)
            return;

        _button.onClick.RemoveListener(HandleButtonClicked);
    }

    private void HandleButtonClicked()
    {
        if (!HasCharacter)
            return;

        SoundPlayback.PlaySfx(ESoundId.SFX_UI_Click);
        Clicked?.Invoke(this);
    }
}
