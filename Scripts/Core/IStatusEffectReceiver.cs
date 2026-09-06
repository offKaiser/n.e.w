public interface IStatusEffectReceiver
{
    void ApplySlow(float multiplier, float duration);
    void ActivateSpeedBoost(float multiplier, float duration);
}
