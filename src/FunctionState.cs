public sealed class FunctionState
{
    public DateTime? IdleStarted { get; set; }
    public bool IsScalingUp { get; set; }
    public bool IsScalingDown { get; set; }

    public bool IsIdling( TimeSpan idleDuration )
        => IdleStarted.HasValue && ( ( DateTime.UtcNow - IdleStarted.Value ) > idleDuration );
}
