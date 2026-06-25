namespace Consequences.Hazards;

public interface IHydraulicTimeseriesHazard : IDepthVelocityHazard
{
    // <summary>
    /// Array of times for depth and velocity in minutes from hydraulic start.
    /// </summary>
    public float[] TimeMinutes { get; }

    /// <summary>
    /// Array of depths at various points in time from hydraulic start.
    /// </summary>
    public float[] Depths { get; }

    /// <summary>
    /// Array of velocities at various points in time from hydraulic start.
    /// </summary>
    public float[] Velocities { get; }

}
