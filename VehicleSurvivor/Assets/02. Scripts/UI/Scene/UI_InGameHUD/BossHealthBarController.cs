using System.Collections.Generic;
using UnityEngine;

public class BossHealthBarController : MonoBehaviour
{
    [Tooltip("비워 있을 경우 자식 BossHealthBarPanel을 자동으로 찾습니다.")]
    [SerializeField] private BossHealthBarPanel[] slots;

    private readonly Dictionary<EntityId, BossHealthBarPanel> _bossToSlot = new();

    public void Initialize()
    {
        EnsureSlots();
        ResetAll();
    }

    public void ResetAll()
    {
        _bossToSlot.Clear();

        if (slots == null)
            return;

        foreach (BossHealthBarPanel slot in slots)
        {
            if (slot != null)
                slot.Hide();
        }
    }

    public void OnBossAppeared(BossAppearedEvent e)
    {
        if (_bossToSlot.ContainsKey(e.BossId))
            return;

        BossHealthBarPanel slot = FindFreeSlot();
        if (slot == null)
        {
            Debug.LogWarning($"[BossHealthBarController] 사용 가능한 슬롯이 없습니다. BossId={e.BossId}");
            return;
        }

        _bossToSlot[e.BossId] = slot;
        slot.Show(e.DisplayName, e.Current, e.Max);
    }

    public void OnBossHpChanged(BossHpChangedEvent e)
    {
        if (!_bossToSlot.TryGetValue(e.BossId, out BossHealthBarPanel slot))
            return;

        slot.UpdateHp(e.Current, e.Max);
    }

    public void OnBossDefeated(BossDefeatedEvent e)
    {
        if (!_bossToSlot.Remove(e.BossId, out BossHealthBarPanel slot))
            return;

        slot.Hide();
    }

    private void EnsureSlots()
    {
        if (slots != null && slots.Length > 0)
            return;

        slots = GetComponentsInChildren<BossHealthBarPanel>(true);
    }

    private BossHealthBarPanel FindFreeSlot()
    {
        if (slots == null)
            return null;

        foreach (BossHealthBarPanel slot in slots)
        {
            if (slot == null || slot.IsOccupied || IsSlotAssigned(slot))
                continue;

            return slot;
        }

        return null;
    }

    private bool IsSlotAssigned(BossHealthBarPanel slot)
    {
        foreach (BossHealthBarPanel assigned in _bossToSlot.Values)
        {
            if (assigned == slot)
                return true;
        }

        return false;
    }
}
