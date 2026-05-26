using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "GameData/Audio/Sound Database")]
public class SoundDatabaseSO : ScriptableObject
{
    [SerializeField] private List<SfxEntry> sfxEntries = new();
    [SerializeField] private List<BgmEntry> bgmEntries = new();

    private Dictionary<ESoundId, SfxEntry> _sfxLookup;
    private Dictionary<EBgmId, BgmEntry> _bgmLookup;

    private void OnEnable() => RebuildLookups();

    private void OnValidate() => RebuildLookups();

    public bool TryGetSfx(ESoundId id, out SfxEntry entry)
    {
        EnsureLookups();
        if (id == ESoundId.None)
        {
            entry = null;
            return false;
        }

        return _sfxLookup.TryGetValue(id, out entry);
    }

    public bool TryGetBgm(EBgmId id, out BgmEntry entry)
    {
        EnsureLookups();
        if (id == EBgmId.None)
        {
            entry = null;
            return false;
        }

        return _bgmLookup.TryGetValue(id, out entry);
    }

    private void EnsureLookups()
    {
        if (_sfxLookup == null || _bgmLookup == null)
            RebuildLookups();
    }

    private void RebuildLookups()
    {
        _sfxLookup = new Dictionary<ESoundId, SfxEntry>();
        for (int i = 0; i < sfxEntries.Count; i++)
        {
            SfxEntry entry = sfxEntries[i];
            if (entry == null || entry.Id == ESoundId.None)
                continue;

            _sfxLookup[entry.Id] = entry;
        }

        _bgmLookup = new Dictionary<EBgmId, BgmEntry>();
        for (int i = 0; i < bgmEntries.Count; i++)
        {
            BgmEntry entry = bgmEntries[i];
            if (entry == null || entry.Id == EBgmId.None)
                continue;

            _bgmLookup[entry.Id] = entry;
        }
    }
}
