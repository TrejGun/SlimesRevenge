namespace SlimesRevenge
{
    /// <summary>One discrete act for a creature turn. Deciders pick; <see cref="CreatureMoves"/> performs.</summary>
    public enum CreatureIntent
    {
        None = 0,
        Attack,
        Chase,
        Flee,
        Wander,
        Idle,
    }
}
