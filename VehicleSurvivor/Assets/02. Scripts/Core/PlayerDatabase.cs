using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDatabase", menuName = "Scriptable Objects/PlayerDatabase")]
public class PlayerDatabase : ScriptableObject
{
    [field: SerializeField] public List<PlayerInfo> Players { get; private set; }
}