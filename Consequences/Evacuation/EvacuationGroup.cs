using Consequences.Hazards;
using Consequences.LifeLossEnums;
using Consequences.Stability;

namespace Consequences.Evacuation;

/// <summary>
/// Evacuations groups represent the population of the study. All Lifeloss simulations 
/// utilize this concept
/// </summary>
public struct EvacuationGroup
{
        //we don't expect an evacuating group size to ever exceed 256
    public byte Under65 { get; }
    public byte Over65 { get; }
    public int OriginIndex { get; }

    public float WarningTime { get; }
    public float InitialMobilizeTime { get; }

    public TransportationMode ModeOfTransportation { get; }
    public float DepthThreshold { get; }
    public StabilityThreshold StabilityCriteria { get; }
    public bool HasGPS { get; set; }

    public int GroupIndex { get; set; }
    public float ActualMobilizeTime { get; set; }
    public bool Warned { get; set; }
    public bool Mobilized { get; set; }
    public bool Safe { get; set; }
    public bool CaughtEvacuating { get; set; }

    public int TotalPopulation => Under65 + Over65;

    public EvacuationGroup(
        byte under65,
        byte over65,
        TransportationMode modeOfTransportation,
        StabilityThreshold stabilityCriteria,
        float depthThreshold,
        int originIndex,
        float warningTime,
        float initialMobilizeTime,
        bool hasGPS)
    {
        Under65 = under65;
        Over65 = over65;
        ModeOfTransportation = modeOfTransportation;
        StabilityCriteria = stabilityCriteria;
        DepthThreshold = depthThreshold;
        OriginIndex = originIndex;
        WarningTime = warningTime;
        InitialMobilizeTime = initialMobilizeTime;
        HasGPS = hasGPS;
        ActualMobilizeTime = initialMobilizeTime;
    }

}
