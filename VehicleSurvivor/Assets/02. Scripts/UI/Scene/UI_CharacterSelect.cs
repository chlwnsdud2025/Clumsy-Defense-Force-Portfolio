using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterSelect : UIScene
{
    [Header("Character Slot Pool")]
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private CharacterSlot _slotPrefab;
    [Min(0)]
    [SerializeField] private int _initialPoolSize = 0;

    [Header("Character Profile")]
    [SerializeField] private Image _profileIconImage;
    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private TMP_Text _descriptionText;
    [Header("Character Preview")]
    [SerializeField] private Transform _previewSpawnRoot;

    private GameObject _currentPreview;

    private readonly List<CharacterSlot> _slotPool = new();
    private CharacterDefinitionSO _selectedCharacter;
    private CharacterSlot _selectedSlot;

    private ICharacterService characterService;

    public Transform ContentRoot => _contentRoot;
    public CharacterSlot SlotPrefab => _slotPrefab;
    public int InitialPoolSize => _initialPoolSize;


    public void SetPreviewSpawnRoot(Transform previewSpawnRoot)
    {
        _previewSpawnRoot = previewSpawnRoot;

        if (_selectedCharacter != null)
        {
            RefreshCharacterVisual(_selectedCharacter);
        }
    }
    public override void OnShow()
    {
        base.OnShow();

        if (characterService == null)
            characterService = ServiceLocator.Get<ICharacterService>();

        InitializeSlotPool();
        RefreshCharacterSlots();
    }

    private void OnValidate()
    {
        _initialPoolSize = Mathf.Max(0, _initialPoolSize);
    }

    private void InitializeSlotPool()
    {
        if (_contentRoot == null || _slotPool.Count > 0)
            return;

        _slotPool.Clear();

        for (int i = 0; i < _contentRoot.childCount; i++)
        {
            Transform child = _contentRoot.GetChild(i);
            CharacterSlot slot = child.GetComponent<CharacterSlot>();
            if (slot == null)
                slot = child.gameObject.AddComponent<CharacterSlot>();

            slot.Clicked -= HandleSlotClicked;
            slot.Clicked += HandleSlotClicked;
            slot.Clear();
            _slotPool.Add(slot);
        }
    }

    private void EnsureSlotPoolSize(int requiredCount)
    {
        if (_contentRoot == null || _slotPrefab == null)
            return;

        while (_slotPool.Count < requiredCount)
        {
            CharacterSlot slotInstance = Instantiate(_slotPrefab, _contentRoot);
            slotInstance.Clicked -= HandleSlotClicked;
            slotInstance.Clicked += HandleSlotClicked;
            slotInstance.Clear();
            _slotPool.Add(slotInstance);
        }
    }

    private void RefreshCharacterSlots()
    {
        IReadOnlyList<CharacterDefinitionSO> characters = characterService?.GetAllCharacters();
        int characterCount = characters?.Count ?? 0;
        int requiredSlotCount = Mathf.Max(_initialPoolSize, characterCount);

        EnsureSlotPoolSize(requiredSlotCount);

        CharacterDefinitionSO targetCharacter = GetInitialSelectedCharacter(characters);
        _selectedCharacter = null;
        _selectedSlot = null;

        for (int i = 0; i < _slotPool.Count; i++)
        {
            CharacterSlot slot = _slotPool[i];
            if (i < characterCount)
            {
                CharacterDefinitionSO character = characters[i];
                slot.Bind(character);

                bool isSelected = IsSameCharacter(character, targetCharacter);
                slot.SetSelected(isSelected);

                if (isSelected)
                {
                    _selectedCharacter = character;
                    _selectedSlot = slot;
                }

                continue;
            }

            slot.Clicked -= HandleSlotClicked;
            slot.Clicked += HandleSlotClicked;
            slot.Clear();
        }

        if (_selectedCharacter == null && characterCount > 0)
            SetSelectedCharacter(characters[0], _slotPool[0], true);
        else
        {
            RefreshProfilePanel(_selectedCharacter);
            RefreshCharacterVisual(_selectedCharacter);
        }
    }

    private void HandleSlotClicked(CharacterSlot slot)
    {
        if (slot == null || !slot.HasCharacter)
            return;

        SetSelectedCharacter(slot.CharacterDefinition, slot);
    }

    public void OnClickStartGame()
    {
        if (characterService == null)
            characterService = ServiceLocator.Get<ICharacterService>();

        if (_selectedCharacter == null)
        {
            Debug.LogWarning("[UI_CharacterSelect] 선택된 캐릭터가 없습니다.");
            return;
        }

        characterService.SetSelectedCharacter(_selectedCharacter);

        GameManager.Instance.LoadInGameScene();
    }

    private void SetSelectedCharacter(CharacterDefinitionSO characterDefinition, CharacterSlot selectedSlot, bool skipServiceUpdate = false)
    {
        if (_selectedSlot == selectedSlot && IsSameCharacter(_selectedCharacter, characterDefinition))
            return;

        CharacterSlot previousSelectedSlot = _selectedSlot;
        _selectedCharacter = characterDefinition;
        _selectedSlot = selectedSlot;

        if (previousSelectedSlot != null && previousSelectedSlot != _selectedSlot)
            previousSelectedSlot.SetSelected(false);

        if (_selectedSlot != null)
            _selectedSlot.SetSelected(true);

        if (!skipServiceUpdate && characterService != null && _selectedCharacter != null)
            characterService.SetSelectedCharacter(_selectedCharacter);

        RefreshProfilePanel(_selectedCharacter);
        RefreshCharacterVisual(_selectedCharacter);
    }

    private void RefreshProfilePanel(CharacterDefinitionSO characterDefinition)
    {
        if (_profileIconImage != null)
            _profileIconImage.sprite = characterDefinition != null ? characterDefinition.Icon : null;

        if (_playerNameText != null)
            _playerNameText.text = characterDefinition != null ? characterDefinition.DisplayName : string.Empty;

        if (_descriptionText != null)
            _descriptionText.text = characterDefinition != null ? characterDefinition.Description : string.Empty;
    }

    private CharacterDefinitionSO GetInitialSelectedCharacter(IReadOnlyList<CharacterDefinitionSO> characters)
    {
        if (characters == null || characters.Count == 0)
            return null;

        CharacterDefinitionSO currentSelection = characterService?.GetSelectedCharacter();
        if (currentSelection != null)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (IsSameCharacter(characters[i], currentSelection))
                    return characters[i];
            }
        }

        return characters[0];
    }

    private void RefreshCharacterVisual(CharacterDefinitionSO characterDefinition)
    {
        if (_previewSpawnRoot == null)
        {
            Debug.LogWarning("[UI_CharacterSelect] Preview Spawn Root가 할당되지 않았습니다.");
            return;
        }

        if (_currentPreview != null)
        {
            Destroy(_currentPreview);
        }

        if (characterDefinition == null || characterDefinition.VisualPrefab == null)
            return;

        _currentPreview = Instantiate(
            characterDefinition.VisualPrefab,
            _previewSpawnRoot.position,
            _previewSpawnRoot.rotation,
            _previewSpawnRoot
        );

        _currentPreview.transform.localPosition = Vector3.zero;
        _currentPreview.transform.localRotation = Quaternion.identity;
        _currentPreview.transform.localScale = Vector3.one;
    }

    private bool IsSameCharacter(CharacterDefinitionSO left, CharacterDefinitionSO right)
    {
        if (left == null || right == null)
            return false;

        return left.CharacterId == right.CharacterId;
    }
}