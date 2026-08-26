namespace SlimesRevenge
{
    /// <summary>
    /// Marker for substances that apply a ticking DoT (burn / poison / corrode).
    /// Floor puddles of these stay walkable; pathfinding only applies a soft cost penalty.
    /// </summary>
    public interface IDamageOverTime
    {
    }
}
