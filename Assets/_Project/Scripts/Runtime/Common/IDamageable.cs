namespace DustlineArena.Runtime.Common
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damage);
    }
}
