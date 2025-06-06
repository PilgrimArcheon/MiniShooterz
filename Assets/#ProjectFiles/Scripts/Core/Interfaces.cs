public interface ICombat
{
    public void PerformShoot();
    public void TakeDamage();
}

public interface IStates
{
    public void SetState(States state);
    public bool CheckCurrentState(States[] states);
    public States GetStates();
}