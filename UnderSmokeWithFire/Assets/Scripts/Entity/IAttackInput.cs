
public interface IAttackInput
{
    int TurnsToHit { get; }
    Entity GetTarget();
    void InflicteDamage(Entity target);
    bool IsAttacking();
}
