
public interface IAttackAreaInput 
{
    bool IsWaitingForExecuteTurn();
    bool WaitingIterationEnd();
    bool ExecuteAttack();
    void CancelAttack();
    void AttackAreaEnd();
}
