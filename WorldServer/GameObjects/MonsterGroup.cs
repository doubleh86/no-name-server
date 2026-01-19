using System.Numerics;
using DataTableLoader.Models;
using NetworkProtocols.Socket.WorldServerProtocols.Models;

namespace WorldServer.GameObjects;

public class MonsterGroup
{
    public long Id { get; set; }
    private MonsterTGroup _monsterTGroup { get; set; }
    public float RoamRadius { get; set; }
    
    private readonly List<MonsterObject> _monsters = new();
    
    public Vector3 AnchorPosition => _monsterTGroup.AnchorPosition;
    public readonly int ZoneId;
    
    public bool IsAnyMemberInCombat { get; set; }
    public PlayerObject TargetPlayer { get; set; }
    
    public int MonsterCount => _monsters.Count;
    public List<MonsterObject> Monsters => _monsters;

    public MonsterGroup(MonsterTGroup monsterTGroup, int zoneId)
    {
        _monsterTGroup = monsterTGroup;
        ZoneId = zoneId;
    }

    public void AddMember(MonsterObject spawnedMonster)
    {
        _monsters.Add(spawnedMonster);
    }
    
    public void UpdateIsAnyMemberInCombat(PlayerObject targetPlayer)
    {
        IsAnyMemberInCombat = _monsters.Any(monster => monster.GetState() == AIState.Attack 
                                                       || monster.GetState() == AIState.Chase);
        if (IsAnyMemberInCombat == false)
        {
            TargetPlayer = null;
            return;
        }
            
        TargetPlayer = targetPlayer;
    }
}