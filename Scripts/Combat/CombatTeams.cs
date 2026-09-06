using Godot;

public static class CombatTeams
{
    public static bool IsEnemy(Node3D caster, Node3D unit)
    {
        MinionTeam team = caster switch
        {
            HeroController hero => hero.Team,
            EnemyController enemy => enemy.Team,
            MinionController minion => minion.Team,
            TowerController tower => tower.Team,
            _ => MinionTeam.Blue
        };
        return unit switch
        {
            MinionController minion => minion.Team != team,
            TowerController tower => tower.Team != team,
            HeroController hero => hero.Team != team,
            EnemyController enemy => enemy.Team != team,
            _ => false
        };
    }
}
