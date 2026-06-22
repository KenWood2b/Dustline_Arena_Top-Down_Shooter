namespace DustlineArena.Runtime.Health
{
    public readonly struct HealthChangedArgs
    {
        public HealthChangedArgs(float current, float max, float delta)
        {
            Current = current;
            Max = max;
            Delta = delta;
        }

        public float Current { get; }
        public float Max { get; }
        public float Delta { get; }
        public float Normalized => Max <= 0f ? 0f : Current / Max;
    }
}
