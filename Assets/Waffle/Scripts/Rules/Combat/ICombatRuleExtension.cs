namespace Waffle.Combat
{
    // Optional pure rule hook. Legacy scenes install nothing and retain their existing path.
    public interface ICombatRuleExtension
    {
        void OnDirectHit(float runTime, int serial, int generation, Vec2 position, float damage);
        void AfterProjectiles(float runTime);
    }

    public enum CombatRuleDamageResult { Invalid, Applied, Killed }
}
