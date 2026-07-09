using Consequences.Evacuation;
using Consequences.LifeLossEnums;
using Consequences.Occupancy;
using Consequences.Stability;

namespace Consequences.Buildings;

public readonly struct LifeLossBuilding
{
    public required Building Building { get; init; }
    public int NumberOfStories { get; init; }
    public float AtticHeight { get; init; }
    public float OtherFloorHeight { get; init; }
    public float GroundFloorHeight { get; init; }
    public int AbleBodiedPeople { get; init; }
    public int LimitedMobilityPeople { get; init; }
    public bool AccessToAttic { get; init; }
    public bool AccessToRoof { get; init; }

    /// <summary>
    /// Generates evacuation groups for the building based on the occupancy type and evacuation parameters.
    /// </summary>
    /// <param name="random">Random number generator for stochastic processes.</param>
    /// <param name="epz">Emergency planning zone information.</param>
    /// <param name="parameters">Evacuation parameters.</param>
    /// <param name="buildingIndex">Index of the building.</param>
    /// <param name="result">List to store generated evacuation groups.</param>
    public void GenerateEvacGroups(Random random, EmergencyPlanningZone epz, EvacuationParameters parameters, int buildingIndex, List<EvacuationGroup> result)
    {
        if (AbleBodiedPeople <= 0 && LimitedMobilityPeople <= 0) return;
        OccupancyType occtype = Building.OccupancyType;

        int tempAble = AbleBodiedPeople;
        int tempLimited = LimitedMobilityPeople;
        double maxMobRate = epz.ProtectiveActionInitiationCdf[^1].Y;
        float timeToWarned;
        float timeToMobilized;
        byte vehicleCapacity = occtype.VehicleOccupancyRate;

        if (occtype.CollectivelyWarned && occtype.CollectivelyMobilize)
        {
            timeToWarned = (float)(epz.FirstAlertCdf.GetXFromY(random.NextDouble()) + epz.WarningIssuanceOffsetMinutes);
            timeToMobilized = GetTimeToMobilized(timeToWarned, maxMobRate, random.NextDouble(), epz);
            // Offset successive vehicles entering the road so they don't all stack on the same position.
            // 4 seconds in minutes ≈ 0.0667.
            const double timeMobilizedOffset = 0.066666667;
            int counter = 0;
            while (tempAble != 0 || tempLimited != 0)
            {
                var g = GenerateGroup(tempAble, tempLimited, random, parameters, buildingIndex, timeToWarned, (float)(timeToMobilized + timeMobilizedOffset * counter), vehicleCapacity);
                tempAble -= g.Under65;
                tempLimited -= g.Over65;
                result.Add(g);
                counter += 1;
            }
        }
        else if (occtype.CollectivelyWarned && !occtype.CollectivelyMobilize)
        {
            timeToWarned = (float)(epz.FirstAlertCdf.GetXFromY(random.NextDouble()) + epz.WarningIssuanceOffsetMinutes);
            while (tempAble != 0 || tempLimited != 0)
            {
                timeToMobilized = GetTimeToMobilized(timeToWarned, maxMobRate, random.NextDouble(), epz);
                var g = GenerateGroup(tempAble, tempLimited, random, parameters, buildingIndex, timeToWarned, timeToMobilized, vehicleCapacity);
                tempAble -= g.Under65;
                tempLimited -= g.Over65;
                result.Add(g);
            }
        }
        else if (!occtype.CollectivelyWarned && !occtype.CollectivelyMobilize)
        {
            while (tempAble != 0 || tempLimited != 0)
            {
                timeToWarned = (float)(epz.FirstAlertCdf.GetXFromY(random.NextDouble()) + epz.WarningIssuanceOffsetMinutes);
                timeToMobilized = GetTimeToMobilized(timeToWarned, maxMobRate, random.NextDouble(), epz);
                var g = GenerateGroup(tempAble, tempLimited, random, parameters, buildingIndex, timeToWarned, timeToMobilized, vehicleCapacity);
                tempAble -= g.Under65;
                tempLimited -= g.Over65;
                result.Add(g);
            }
        }
        else
        {
            // Single mobilize time for the building, but per-group warning times.
            timeToMobilized = GetTimeToMobilized(0, maxMobRate, random.NextDouble(), epz);
            while (tempAble != 0 || tempLimited != 0)
            {
                timeToWarned = (float)(epz.FirstAlertCdf.GetXFromY(random.NextDouble()) + epz.WarningIssuanceOffsetMinutes);
                var g = GenerateGroup(tempAble, tempLimited, random, parameters, buildingIndex, timeToWarned, timeToMobilized, vehicleCapacity);
                tempAble -= g.Under65;
                tempLimited -= g.Over65;
                result.Add(g);
            }
        }
    }

    /// <summary>
    /// Determines the hazard level for the building based on stability criteria, mobility limitations, and swimming ability.
    /// </summary>
    /// <param name="depth">The depth of water at the building.</param> 
    /// <param name="velocity">The velocity of water at the building.</param>
    /// <param name="noStoriesHumanStabilityCriteria">Stability criteria for buildings with no stories.</param>
    /// <param name="limitedMobility">Indicates if the occupants have limited mobility.</param>
    /// <param name="canSwim">Indicates if the occupants can swim.</param>
    /// <param name="safeFlowThreshold">The threshold for safe flow velocity.</param>
    /// <param name="hasCollapsed">Indicates if the building has collapsed. If not then tests stability criteria.</param>
    /// <returns>The hazard level for the building.</returns>
    public HazardLevel GetHazardLevel(float depth, float velocity, StabilityThreshold noStoriesHumanStabilityCriteria, bool limitedMobility, bool canSwim, float safeFlowThreshold, bool hasCollapsed)
    {
        if (hasCollapsed) { return HazardLevel.High; }
        else if (Building.StabilityThreshold?.Collapsed(depth, velocity) == true)
        {
            return HazardLevel.High;

        }
        if (depth <= 0) { return HazardLevel.None; }
        // Paul Risher believes that if the depth is above zero at the home then it should be at least low hazard even if it is not above foundation height.
        // I have given in to Paul's complaints. If depth on home is above zero the occupants will be at least low hazard regardless of foundation height.
        var occ = Building.OccupancyType;
        float effectiveDepth = depth - Building.FoundationHeight - occ.FoundationHeightOffset;
        if (effectiveDepth <= 0) { return HazardLevel.Low; }
        // 
        if (NumberOfStories <= 0)
        {
            if (noStoriesHumanStabilityCriteria.Collapsed(effectiveDepth, velocity))
            {
                if (velocity < safeFlowThreshold)
                {
                    return canSwim == true ? HazardLevel.Low : HazardLevel.High;
                }
                return HazardLevel.High;
            }
        }
        else
        {
            double chanceDepth;
            if (limitedMobility)
            {
                if (NumberOfStories == 1) { chanceDepth = occ.FromFloorDepthThreshold; }
                else { chanceDepth = GroundFloorHeight + (OtherFloorHeight * (NumberOfStories - 2)) + occ.FromFloorDepthThreshold; } //Top Floor (from floor)
            }
            else if (AccessToAttic == false)
            { chanceDepth = GroundFloorHeight + OtherFloorHeight * (NumberOfStories - 1) - occ.FromCeilingDepthThreshold; } //Top Floor (from ceiling)
            else if (AccessToRoof == false)
            { chanceDepth = GroundFloorHeight + OtherFloorHeight * (NumberOfStories - 1) + AtticHeight - occ.FromCeilingDepthThreshold; } //Attic (from ceiling)
            else
            { chanceDepth = GroundFloorHeight + OtherFloorHeight * (NumberOfStories - 1) + AtticHeight + occ.FromFloorDepthThreshold; } //Roof (from floor)

            if (chanceDepth <= effectiveDepth) { return HazardLevel.High; }
        }
        // 
        return HazardLevel.Low;
    }

    private static EvacuationGroup GenerateGroup(int tempAble, int tempLimited, Random random, EvacuationParameters parameters, int buildingIndex, float timeToWarned, float timeToMobilized, byte vehicleCapacity)
    {
        byte popAble = 0;
        byte popLimited = 0;

        if (tempAble + tempLimited <= vehicleCapacity)
        {
            popAble = (byte)tempAble;
            popLimited = (byte)tempLimited;
        }
        else if (tempLimited == 0)
        {
            popAble = vehicleCapacity;
        }
        else if (tempAble == 0)
        {
            popLimited = vehicleCapacity;
        }
        else
        {
            while (popLimited + popAble < vehicleCapacity)
            {
                if (tempLimited - popLimited > 0)
                {
                    popLimited += 1;
                    if (popLimited + popAble == vehicleCapacity) break;
                }
                if (tempAble - popAble > 0) popAble += 1;
            }
        }

        bool willRerouteForTraffic = random.NextDouble() < parameters.FractionTrafficReroute;
        TransportationMode transportationMode = TransportationMode.LowClearanceVehicle;
        StabilityThreshold groupStability = parameters.LowClearanceStability;
        float depthThreshold = 2f;

        //if (parameters.SimulateTraffic)
        //{
        //    if (random.NextDouble() < parameters.FractionInVehicles)
        //    {
        //        if (random.NextDouble() < parameters.FractionInCars)
        //        {
        //            transportationMode = TransportationMode.LowClearanceVehicle;
        //            groupStability = parameters.LowClearanceStability;
        //            depthThreshold = (float)Math.Max(parameters.LowClearanceRoadEntryCdf.GetXFromY(random.NextDouble()), 0);
        //        }
        //        else
        //        {
        //            transportationMode = TransportationMode.HighClearanceVehicle;
        //            groupStability = parameters.HighClearanceStability;
        //            depthThreshold = (float)Math.Max(parameters.HighClearanceRoadEntryCdf.GetXFromY(random.NextDouble()), 0);
        //        }
        //    }
        //    else
        //    {
        //        transportationMode = TransportationMode.Foot;
        //        groupStability = parameters.PedestrianStability;
        //    }
        //}

        return new EvacuationGroup(popAble, popLimited, transportationMode, groupStability, depthThreshold, buildingIndex, timeToWarned, timeToMobilized, willRerouteForTraffic);
    }

    private static float GetTimeToMobilized(float timeToWarned, double maxFractionMobilized, double randomNumber, EmergencyPlanningZone epz)
    {
        if (randomNumber > maxFractionMobilized) return float.MinValue;
        return (float)(timeToWarned + epz.ProtectiveActionInitiationCdf.GetXFromY(randomNumber));
    }
}
