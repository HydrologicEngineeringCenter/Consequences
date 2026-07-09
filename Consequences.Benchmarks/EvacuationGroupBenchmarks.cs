using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Evacuation;
using Consequences.Hazards;
using Consequences.Occupancy;
using Numerics.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consequences.Benchmarks
{
    [MemoryDiagnoser]
    public class EvacuationGroupBenchmarks
    {

        [Params(1_000, 100_000)]
        public int StructureCount;

        private LifeLossBuilding[] _buildings = Array.Empty<LifeLossBuilding>();
        private EvacuationParameters _parameters;
        private EmergencyPlanningZone _emergencyPlanningZone;

        private float[] _depths = Array.Empty<float>();
        private float[] _velocities = Array.Empty<float>();

        private DepthHazard[] _depthHazards = Array.Empty<DepthHazard>();
        private DepthVelocity[] _depthVelocityHazards = Array.Empty<DepthVelocity>();
        private HydraulicTimeSeries[] _timeSeriesHazards = Array.Empty<HydraulicTimeSeries>();

        [GlobalSetup]
        public void Setup()
        {
            var structureCurve = new OrderedPairedData(
                new double[] { 0.0, 10.0 }, new double[] { 0.0, 1.0 },
                strictOnX: true, SortOrder.Ascending, strictOnY: true, SortOrder.Ascending);
            var contentCurve = new OrderedPairedData(
                new double[] { 0.0, 8.0 }, new double[] { 0.0, 1.0 },
                strictOnX: true, SortOrder.Ascending, strictOnY: true, SortOrder.Ascending);

            var occupancy = new OccupancyType
            {
                Name = "RES1",
                FoundationHeightOffset = 0f,
                StructureDamageFunction = structureCurve,
                ContentDamageFunction = contentCurve
            };

            var rng = new Random(42);
            _buildings = new LifeLossBuilding[StructureCount];

            //_depths = new float[StructureCount];
            //_velocities = new float[StructureCount];
            //_depthHazards = new DepthHazard[StructureCount];
            //_depthVelocityHazards = new DepthVelocity[StructureCount];
            // _timeSeriesHazards = new HydraulicTimeSeries[StructureCount];

            for (int i = 0; i < StructureCount; i++)
            {
                var b = new Building
                {
                    OccupancyType = occupancy,
                    Value = (float)(100_000 + rng.NextDouble() * 200_000),
                    ContentValue = (float)(50_000 + rng.NextDouble() * 100_000),
                    FoundationHeight = (float)(rng.NextDouble() * 3.0)
                };

                _buildings[i] = new LifeLossBuilding
                {
                    Building = b,
                    NumberOfStories = rng.Next(1, 4),
                    AtticHeight = 6f,
                    OtherFloorHeight = 9f,
                    GroundFloorHeight = 9f,
                    AbleBodiedPeople = rng.Next(1, 10),
                    LimitedMobilityPeople = rng.Next(0, 5),
                    AccessToAttic = rng.NextDouble() < 0.5,
                    AccessToRoof = rng.NextDouble() < 0.5
                };

                float depth = (float)(rng.NextDouble() * 12.0);
                float velocity = (float)(rng.NextDouble() * 5.0);
                _depths[i] = depth;
                _velocities[i] = velocity;

                _depthHazards[i] = new DepthHazard(depth);
                _depthVelocityHazards[i] = new DepthVelocity(depth, velocity);
                _timeSeriesHazards[i] = BuildTimeSeries(depth, velocity);
            }



            _parameters = new EvacuationParameters()
            {
                HighClearanceStability = new Stability.StabilityThreshold(new OrderedPairedData(new double[] { 0.0, 10.0 }, new double[] { 10.0, 1.0 }, false, SortOrder.Ascending, false, SortOrder.Descending)),
                HighClearanceRoadEntryCdf = new OrderedPairedData(new double[] { 0.0, 0.0 }, new double[] { 4.0, 1.0 }, false, SortOrder.Ascending, false, SortOrder.Ascending),
                LowClearanceStability = new Stability.StabilityThreshold(new OrderedPairedData(new double[] { 0.0, 10.0 }, new double[] { 10.0, 1.0 }, false, SortOrder.Ascending, false, SortOrder.Descending)),
                LowClearanceRoadEntryCdf = new OrderedPairedData(new double[] { 0.0, 0.0 }, new double[] { 3.0, 1.0 }, false, SortOrder.Ascending, false, SortOrder.Ascending),
                PedestrianStability = new Stability.StabilityThreshold(new OrderedPairedData(new double[] { 0.0, 10.0 }, new double[] { 10.0, 1.0 }, false, SortOrder.Ascending, false, SortOrder.Descending))
            };

            var firstAlertCDF = new OrderedPairedData(new double[] { 0.0, 0.0 }, new double[] { 10.0, 1.0 }, false, SortOrder.Ascending, false, SortOrder.Ascending);
            var paiCDF = new OrderedPairedData(new double[] { 0.0, 0.0 }, new double[] { 10.0, 1.0 }, false, SortOrder.Ascending, false, SortOrder.Ascending);
            _emergencyPlanningZone = new EmergencyPlanningZone(firstAlertCDF, paiCDF, 0, 0, 0);
        }

        private static HydraulicTimeSeries BuildTimeSeries(float peakDepth, float peakVelocity)
        {
            // Triangular rise-and-fall whose peak matches the scalar base data,
            // so all hazard types resolve to the same Depth/Velocity values.
            float[] times = { 0f, 30f, 60f, 90f, 120f };
            float[] depths = { 0f, peakDepth * 0.5f, peakDepth, peakDepth * 0.5f, 0f };
            float[] velocities = { 0f, peakVelocity * 0.5f, peakVelocity, peakVelocity * 0.5f, 0f };
            return new HydraulicTimeSeries(times, depths, velocities, pointReductionTolerance: 0.001f);
        }

        [Benchmark]
        public void GenerateEvacGroups()
        {
            for (int c = 0; c < 50; c++)
            {
                Random groupRandy = new Random(123456);
                List<EvacuationGroup> evacGroupList = new List<EvacuationGroup>();
                for (int i = 0; i < _buildings.Length; i++)
                {
                    _buildings[i].GenerateEvacGroups(groupRandy, _emergencyPlanningZone, _parameters, i, evacGroupList);
                }
            }

        }

    }
}
