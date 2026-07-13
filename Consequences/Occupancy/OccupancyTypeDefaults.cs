// Source: HEC-FIA "Occupancy Type Defaults.sqlite" — the accepted source for the 40
// occupancy types shared with go-consequences occtypes.json (@ dff647c), including the
// HEC-FIA version of the AGR1 structure curve. See docs/adr/0006 for the decision record
// and docs/occupancy-type-defaults/ for the comparison outputs.
//
// Sixteen RES1 curves carry Normal per-ordinate standard deviations in the source; central
// values are transcribed here. The uncertainty data lives in
// docs/occupancy-type-defaults/occtypes-fia.canonical.json for when UncertainOccupancyType matures.

using Numerics.Data;

namespace Consequences.Occupancy;

public static class OccupancyTypeDefaults
{
    /// <summary>
    /// The 40 default occupancy types implemented by both HEC-FIA and go-consequences.
    /// Builds fresh instances on every call; OrderedPairedData is mutable, so callers get
    /// their own copies rather than shared static state.
    /// </summary>
    public static OccupancyType[] GetDefaults() =>
    [
        // Agriculture facilities and offices (Commercial)
        new OccupancyType
        {
            Name = "AGR1",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.06, 0.11, 0.15, 0.19, 0.25, 0.3, 0.35, 0.41, 0.46, 0.51, 0.57, 0.63, 0.7, 0.75, 0.79, 0.82, 0.84, 0.87, 0.89, 0.9, 0.92, 0.93, 0.95, 0.96, 0.96 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.06, 0.2, 0.43, 0.58, 0.65, 0.66, 0.66, 0.67, 0.7, 0.75, 0.76, 0.76, 0.76, 0.77, 0.77, 0.77, 0.78, 0.78, 0.78, 0.79, 0.79, 0.79, 0.79, 0.8, 0.8 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Retail trade (Commercial)
        new OccupancyType
        {
            Name = "COM1",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.01, 0.09, 0.14, 0.16, 0.18, 0.2, 0.23, 0.26, 0.3, 0.34, 0.38, 0.42, 0.47, 0.51, 0.55, 0.58, 0.61, 0.64, 0.67, 0.69, 0.71, 0.74, 0.76, 0.78, 0.8 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.02, 0.26, 0.42, 0.56, 0.68, 0.78, 0.83, 0.85, 0.87, 0.88, 0.89, 0.9, 0.91, 0.92, 0.92, 0.92, 0.93, 0.93, 0.94, 0.94, 0.94, 0.94, 0.94, 0.94, 0.94 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Parking garages (Commercial)
        new OccupancyType
        {
            Name = "COM10",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.03, 0.05, 0.06, 0.07, 0.08, 0.1, 0.13, 0.17, 0.21, 0.25, 0.3, 0.35, 0.41, 0.47, 0.52, 0.58, 0.65, 0.71, 0.76, 0.81, 0.86, 0.91, 0.96, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.11, 0.17, 0.2, 0.23, 0.25, 0.29, 0.35, 0.42, 0.51, 0.63, 0.77, 0.93, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Wholesale trade (Commercial)
        new OccupancyType
        {
            Name = "COM2",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.05, 0.08, 0.11, 0.13, 0.16, 0.19, 0.22, 0.25, 0.29, 0.32, 0.37, 0.41, 0.45, 0.49, 0.52, 0.55, 0.58, 0.61, 0.63, 0.66, 0.68, 0.7, 0.71, 0.73 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.03, 0.16, 0.27, 0.36, 0.49, 0.57, 0.63, 0.69, 0.72, 0.76, 0.8, 0.82, 0.84, 0.86, 0.87, 0.87, 0.88, 0.89, 0.89, 0.89, 0.89, 0.89, 0.89, 0.89, 0.89 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Personal and repair services (Commercial)
        new OccupancyType
        {
            Name = "COM3",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.09, 0.12, 0.13, 0.16, 0.19, 0.22, 0.25, 0.28, 0.32, 0.35, 0.39, 0.43, 0.47, 0.5, 0.54, 0.57, 0.61, 0.64, 0.68, 0.71, 0.75, 0.78, 0.82, 0.85 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.04, 0.29, 0.46, 0.67, 0.79, 0.85, 0.91, 0.92, 0.92, 0.93, 0.94, 0.96, 0.96, 0.97, 0.97, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Professional and technical services (Commercial)
        new OccupancyType
        {
            Name = "COM4",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.02, 0.11, 0.16, 0.22, 0.28, 0.35, 0.38, 0.41, 0.44, 0.47, 0.5, 0.54, 0.57, 0.59, 0.62, 0.66, 0.68, 0.7, 0.72, 0.74, 0.76, 0.77, 0.78, 0.79, 0.8 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.02, 0.18, 0.25, 0.35, 0.43, 0.49, 0.52, 0.55, 0.57, 0.58, 0.6, 0.65, 0.67, 0.68, 0.69, 0.7, 0.71, 0.71, 0.72, 0.72, 0.72, 0.72, 0.72, 0.72, 0.72 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Banks (Commercial)
        new OccupancyType
        {
            Name = "COM5",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.11, 0.11, 0.12, 0.13, 0.15, 0.17, 0.19, 0.22, 0.24, 0.28, 0.31, 0.34, 0.37, 0.4, 0.44, 0.48, 0.51, 0.55, 0.59, 0.63, 0.67, 0.71, 0.75, 0.79 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.5, 0.74, 0.83, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Hospitals (Commercial)
        new OccupancyType
        {
            Name = "COM6",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0.2, 0.25, 0.3, 0.35, 0.4, 0.43, 0.47, 0.5, 0.53, 0.55, 0.57, 0.6, 0.6, 0.6, 0.6, 0.6, 0.6, 0.6, 0.6, 0.6, 0.6, 0.6 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0.1, 0.2, 0.3, 0.65, 0.72, 0.78, 0.85, 0.95, 0.95, 0.95, 0.95, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Medical office and clinic (Commercial)
        new OccupancyType
        {
            Name = "COM7",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.02, 0.11, 0.12, 0.13, 0.14, 0.16, 0.17, 0.18, 0.2, 0.22, 0.24, 0.27, 0.3, 0.34, 0.37, 0.41, 0.44, 0.48, 0.51, 0.54, 0.56, 0.59, 0.61, 0.64, 0.66 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.28, 0.51, 0.6, 0.63, 0.67, 0.71, 0.72, 0.74, 0.77, 0.81, 0.86, 0.92, 0.94, 0.97, 0.99, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Entertainment and recreation (Commercial)
        new OccupancyType
        {
            Name = "COM8",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.01, 0.09, 0.11, 0.12, 0.14, 0.16, 0.18, 0.2, 0.22, 0.26, 0.29, 0.33, 0.37, 0.41, 0.45, 0.5, 0.53, 0.57, 0.6, 0.63, 0.66, 0.69, 0.73, 0.76, 0.78 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.13, 0.45, 0.55, 0.64, 0.73, 0.77, 0.8, 0.82, 0.83, 0.85, 0.87, 0.89, 0.9, 0.91, 0.92, 0.93, 0.94, 0.95, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Theaters (Commercial)
        new OccupancyType
        {
            Name = "COM9",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.02, 0.04, 0.05, 0.05, 0.05, 0.06, 0.08, 0.1, 0.12, 0.15, 0.2, 0.24, 0.29, 0.35, 0.42, 0.49, 0.56, 0.62, 0.68, 0.74, 0.8, 0.86, 0.92, 0.98 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.04, 0.06, 0.08, 0.09, 0.1, 0.12, 0.17, 0.22, 0.3, 0.41, 0.57, 0.66, 0.73, 0.79, 0.84, 0.9, 0.97, 0.98, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Grade schools and administrative offices (Public)
        new OccupancyType
        {
            Name = "EDU1",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.05, 0.07, 0.09, 0.09, 0.1, 0.11, 0.13, 0.15, 0.17, 0.2, 0.24, 0.28, 0.33, 0.39, 0.45, 0.52, 0.59, 0.64, 0.69, 0.74, 0.79, 0.84, 0.89, 0.94 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.27, 0.38, 0.53, 0.64, 0.68, 0.7, 0.72, 0.75, 0.79, 0.83, 0.88, 0.94, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Colleges and universities (Public)
        new OccupancyType
        {
            Name = "EDU2",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.05, 0.07, 0.09, 0.09, 0.1, 0.11, 0.13, 0.15, 0.17, 0.2, 0.24, 0.28, 0.33, 0.39, 0.45, 0.52, 0.59, 0.64, 0.69, 0.74, 0.79, 0.84, 0.89, 0.94 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.27, 0.38, 0.53, 0.64, 0.68, 0.7, 0.72, 0.75, 0.79, 0.83, 0.88, 0.94, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Government - general services (Public)
        new OccupancyType
        {
            Name = "GOV1",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.05, 0.08, 0.13, 0.14, 0.14, 0.15, 0.17, 0.19, 0.22, 0.26, 0.31, 0.37, 0.44, 0.51, 0.59, 0.65, 0.7, 0.74, 0.79, 0.83, 0.87, 0.91, 0.95, 0.98 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.3, 0.59, 0.74, 0.83, 0.9, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Government - emergency response (Public)
        new OccupancyType
        {
            Name = "GOV2",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.07, 0.1, 0.11, 0.12, 0.15, 0.17, 0.2, 0.23, 0.27, 0.31, 0.35, 0.4, 0.44, 0.48, 0.52, 0.56, 0.6, 0.64, 0.68, 0.72, 0.76, 0.8, 0.84, 0.88 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.08, 0.2, 0.38, 0.55, 0.7, 0.81, 0.89, 0.98, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Heavy industrial (Industrial)
        new OccupancyType
        {
            Name = "IND1",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.01, 0.1, 0.12, 0.15, 0.19, 0.22, 0.26, 0.3, 0.35, 0.39, 0.42, 0.48, 0.5, 0.51, 0.53, 0.54, 0.55, 0.55, 0.56, 0.56, 0.57, 0.57, 0.57, 0.58, 0.58 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.15, 0.24, 0.34, 0.41, 0.47, 0.52, 0.57, 0.6, 0.63, 0.64, 0.66, 0.68, 0.69, 0.72, 0.73, 0.73, 0.73, 0.74, 0.74, 0.74, 0.74, 0.75, 0.75, 0.75 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Light industrial (Industrial)
        new OccupancyType
        {
            Name = "IND2",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.01, 0.09, 0.14, 0.17, 0.22, 0.26, 0.3, 0.32, 0.35, 0.37, 0.39, 0.43, 0.46, 0.48, 0.5, 0.51, 0.54, 0.55, 0.57, 0.59, 0.6, 0.62, 0.63, 0.65, 0.66 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.09, 0.23, 0.35, 0.44, 0.52, 0.58, 0.62, 0.65, 0.68, 0.7, 0.73, 0.74, 0.77, 0.78, 0.78, 0.79, 0.8, 0.8, 0.8, 0.8, 0.81, 0.81, 0.81, 0.81 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Food/drugs/chemicals (Industrial)
        new OccupancyType
        {
            Name = "IND3",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.13, 0.14, 0.19, 0.22, 0.25, 0.28, 0.3, 0.33, 0.34, 0.36, 0.39, 0.4, 0.42, 0.42, 0.43, 0.43, 0.44, 0.44, 0.44, 0.44, 0.44, 0.45, 0.45, 0.45 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.02, 0.2, 0.41, 0.51, 0.62, 0.67, 0.71, 0.73, 0.76, 0.78, 0.79, 0.82, 0.83, 0.84, 0.86, 0.87, 0.87, 0.88, 0.88, 0.88, 0.88, 0.88, 0.88, 0.88, 0.88 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Metal/minerals processing (Industrial)
        new OccupancyType
        {
            Name = "IND4",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.1, 0.14, 0.18, 0.22, 0.26, 0.34, 0.41, 0.42, 0.42, 0.45, 0.47, 0.49, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.15, 0.2, 0.26, 0.31, 0.37, 0.4, 0.44, 0.48, 0.53, 0.56, 0.57, 0.6, 0.62, 0.63, 0.63, 0.63, 0.64, 0.65, 0.65, 0.65, 0.65, 0.65, 0.65, 0.65, 0.65 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // High technology (Industrial)
        new OccupancyType
        {
            Name = "IND5",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.13, 0.14, 0.19, 0.22, 0.25, 0.28, 0.3, 0.33, 0.34, 0.36, 0.39, 0.4, 0.42, 0.42, 0.43, 0.43, 0.44, 0.44, 0.44, 0.44, 0.44, 0.45, 0.45, 0.45 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.02, 0.2, 0.41, 0.51, 0.62, 0.67, 0.71, 0.73, 0.76, 0.78, 0.79, 0.82, 0.83, 0.84, 0.86, 0.87, 0.87, 0.88, 0.88, 0.88, 0.88, 0.88, 0.88, 0.88, 0.88 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Construction facilities and offices (Industrial)
        new OccupancyType
        {
            Name = "IND6",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.22, 0.31, 0.37, 0.43, 0.47, 0.5, 0.54, 0.57, 0.61, 0.63, 0.64, 0.65, 0.67, 0.68, 0.69, 0.7, 0.71, 0.72, 0.73, 0.74, 0.75, 0.76, 0.76, 0.77 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.2, 0.35, 0.47, 0.56, 0.59, 0.66, 0.69, 0.71, 0.72, 0.78, 0.79, 0.8, 0.8, 0.81, 0.81, 0.81, 0.82, 0.82, 0.82, 0.83, 0.83, 0.83, 0.83, 0.83 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Churches and non-profit organizations (Public)
        new OccupancyType
        {
            Name = "REL1",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.1, 0.11, 0.11, 0.12, 0.12, 0.13, 0.14, 0.14, 0.15, 0.17, 0.19, 0.24, 0.3, 0.38, 0.45, 0.52, 0.58, 0.64, 0.69, 0.74, 0.78, 0.82, 0.85, 0.88 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.1, 0.52, 0.72, 0.85, 0.92, 0.95, 0.98, 0.99, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Single family residential structure that is 1 story tall with no basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-1SNB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.025, 0.134, 0.233, 0.321, 0.401, 0.471, 0.532, 0.586, 0.632, 0.672, 0.705, 0.732, 0.754, 0.772, 0.785, 0.795, 0.802, 0.807 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.024, 0.081, 0.133, 0.179, 0.22, 0.257, 0.288, 0.315, 0.338, 0.357, 0.372, 0.384, 0.392, 0.397, 0.4, 0.4, 0.4, 0.4 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Single family residential structure that is 1 story tall and has a basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-1SWB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.007, 0.008, 0.024, 0.052, 0.09, 0.138, 0.194, 0.255, 0.32, 0.387, 0.455, 0.522, 0.586, 0.645, 0.698, 0.742, 0.777, 0.801, 0.811, 0.811, 0.811, 0.811, 0.811, 0.811 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0.001, 0.008, 0.021, 0.037, 0.057, 0.08, 0.105, 0.132, 0.16, 0.189, 0.218, 0.247, 0.274, 0.3, 0.324, 0.345, 0.363, 0.377, 0.386, 0.391, 0.391, 0.391, 0.391, 0.391, 0.391 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Single family residential structure that is 2 stories tall with no basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-2SNB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.03, 0.093, 0.152, 0.209, 0.263, 0.314, 0.362, 0.407, 0.449, 0.488, 0.524, 0.557, 0.587, 0.614, 0.638, 0.659, 0.677, 0.692 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.01, 0.05, 0.087, 0.122, 0.155, 0.185, 0.213, 0.239, 0.263, 0.284, 0.303, 0.32, 0.334, 0.347, 0.356, 0.364, 0.369, 0.372 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Single family residential structure that is 2 stories tall and has a basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-2SWB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0.017, 0.017, 0.019, 0.029, 0.047, 0.072, 0.102, 0.139, 0.179, 0.223, 0.27, 0.319, 0.369, 0.419, 0.469, 0.518, 0.564, 0.608, 0.648, 0.684, 0.714, 0.737, 0.754, 0.764, 0.764 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.01, 0.023, 0.037, 0.052, 0.068, 0.084, 0.101, 0.119, 0.138, 0.157, 0.177, 0.198, 0.22, 0.243, 0.267, 0.291, 0.317, 0.344, 0.372, 0.4, 0.43, 0.461, 0.493, 0.526 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Single family residential structure that is 3 stories tall with no basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-3SNB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.03, 0.093, 0.152, 0.209, 0.263, 0.314, 0.362, 0.407, 0.449, 0.488, 0.524, 0.557, 0.587, 0.614, 0.638, 0.659, 0.677, 0.692 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.01, 0.05, 0.087, 0.122, 0.155, 0.185, 0.213, 0.239, 0.263, 0.284, 0.303, 0.32, 0.334, 0.347, 0.356, 0.364, 0.369, 0.372 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Single family residential structure that is 3 stories tall and has a basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-3SWB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0.017, 0.017, 0.019, 0.029, 0.047, 0.072, 0.102, 0.139, 0.179, 0.223, 0.27, 0.319, 0.369, 0.419, 0.469, 0.518, 0.564, 0.608, 0.648, 0.684, 0.714, 0.737, 0.754, 0.764, 0.764 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.01, 0.023, 0.037, 0.052, 0.068, 0.084, 0.101, 0.119, 0.138, 0.157, 0.177, 0.198, 0.22, 0.243, 0.267, 0.291, 0.317, 0.344, 0.372, 0.4, 0.43, 0.461, 0.493, 0.526 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Single family residential structure that is a split level structure with no basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-SLNB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.064, 0.072, 0.094, 0.129, 0.174, 0.228, 0.289, 0.355, 0.423, 0.492, 0.561, 0.626, 0.686, 0.739, 0.784, 0.817, 0.838, 0.844 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0.022, 0.029, 0.047, 0.075, 0.111, 0.153, 0.201, 0.252, 0.305, 0.357, 0.409, 0.458, 0.502, 0.541, 0.572, 0.594, 0.605, 0.605 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Single family residential structure that is a split level structure and has a basement. (Residential)
        new OccupancyType
        {
            Name = "RES1-SLWB",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0, 0, 0.025, 0.031, 0.047, 0.072, 0.104, 0.142, 0.185, 0.232, 0.282, 0.334, 0.386, 0.438, 0.488, 0.535, 0.578, 0.616, 0.648, 0.672, 0.688, 0.693, 0.693, 0.693, 0.693 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new double[] { 0.006, 0.007, 0.014, 0.024, 0.038, 0.054, 0.073, 0.094, 0.116, 0.138, 0.161, 0.182, 0.202, 0.221, 0.236, 0.249, 0.258, 0.263, 0.263, 0.263, 0.263, 0.263, 0.263, 0.263, 0.263 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Manufactured housing (Residential)
        new OccupancyType
        {
            Name = "RES2",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.11, 0.44, 0.63, 0.73, 0.78, 0.79, 0.81, 0.82, 0.83, 0.84, 0.85, 0.86, 0.88, 0.89, 0.9, 0.91, 0.92, 0.94, 0.95, 0.96, 0.97, 0.98, 0.99, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.03, 0.27, 0.49, 0.64, 0.7, 0.76, 0.78, 0.79, 0.81, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83, 0.83 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Multi Family Residence - Duplex (Residential)
        new OccupancyType
        {
            Name = "RES3A",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.05, 0.28, 0.29, 0.31, 0.36, 0.37, 0.39, 0.4, 0.41, 0.42, 0.44, 0.46, 0.48, 0.52, 0.55, 0.58, 0.61, 0.64, 0.68, 0.69, 0.7, 0.71, 0.72, 0.73, 0.74 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.04, 0.24, 0.34, 0.4, 0.47, 0.53, 0.56, 0.58, 0.58, 0.58, 0.61, 0.66, 0.68, 0.76, 0.81, 0.86, 0.91, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = false,
            CollectivelyMobilize = false,
        },
        // Multi Family Residence - 3 to 4 units (Residential)
        new OccupancyType
        {
            Name = "RES3B",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.05, 0.28, 0.29, 0.31, 0.36, 0.37, 0.39, 0.4, 0.41, 0.42, 0.44, 0.46, 0.48, 0.52, 0.55, 0.58, 0.61, 0.64, 0.68, 0.69, 0.7, 0.71, 0.72, 0.73, 0.74 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.04, 0.24, 0.34, 0.4, 0.47, 0.53, 0.56, 0.58, 0.58, 0.58, 0.61, 0.66, 0.68, 0.76, 0.81, 0.86, 0.91, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = false,
            CollectivelyMobilize = false,
        },
        // Multi Family Residence – 5 to 9 Units (Residential)
        new OccupancyType
        {
            Name = "RES3C",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.05, 0.28, 0.29, 0.31, 0.36, 0.37, 0.39, 0.4, 0.41, 0.42, 0.44, 0.46, 0.48, 0.52, 0.55, 0.58, 0.61, 0.64, 0.68, 0.69, 0.7, 0.71, 0.72, 0.73, 0.74 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.04, 0.24, 0.34, 0.4, 0.47, 0.53, 0.56, 0.58, 0.58, 0.58, 0.61, 0.66, 0.68, 0.76, 0.81, 0.86, 0.91, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = false,
            CollectivelyMobilize = false,
        },
        // Multi Family Residence – 10 to 19 Units (Residential)
        new OccupancyType
        {
            Name = "RES3D",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.05, 0.28, 0.29, 0.31, 0.36, 0.37, 0.39, 0.4, 0.41, 0.42, 0.44, 0.46, 0.48, 0.52, 0.55, 0.58, 0.61, 0.64, 0.68, 0.69, 0.7, 0.71, 0.72, 0.73, 0.74 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.04, 0.24, 0.34, 0.4, 0.47, 0.53, 0.56, 0.58, 0.58, 0.58, 0.61, 0.66, 0.68, 0.76, 0.81, 0.86, 0.91, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = false,
            CollectivelyMobilize = false,
        },
        // Multi Family Residence – 20 to 49 Units (Residential)
        new OccupancyType
        {
            Name = "RES3E",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.05, 0.28, 0.29, 0.31, 0.36, 0.37, 0.39, 0.4, 0.41, 0.42, 0.44, 0.46, 0.48, 0.52, 0.55, 0.58, 0.61, 0.64, 0.68, 0.69, 0.7, 0.71, 0.72, 0.73, 0.74 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.04, 0.24, 0.34, 0.4, 0.47, 0.53, 0.56, 0.58, 0.58, 0.58, 0.61, 0.66, 0.68, 0.76, 0.81, 0.86, 0.91, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = false,
            CollectivelyMobilize = false,
        },
        // Multi Family Residence – more than 50 Units (Residential)
        new OccupancyType
        {
            Name = "RES3F",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.05, 0.28, 0.29, 0.31, 0.36, 0.37, 0.39, 0.4, 0.41, 0.42, 0.44, 0.46, 0.48, 0.52, 0.55, 0.58, 0.61, 0.64, 0.68, 0.69, 0.7, 0.71, 0.72, 0.73, 0.74 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0.04, 0.24, 0.34, 0.4, 0.47, 0.53, 0.56, 0.58, 0.58, 0.58, 0.61, 0.66, 0.68, 0.76, 0.81, 0.86, 0.91, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = false,
            CollectivelyMobilize = false,
        },
        // Temporary lodging (e.g. Hotels) (Residential)
        new OccupancyType
        {
            Name = "RES4",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.03, 0.05, 0.06, 0.07, 0.09, 0.12, 0.14, 0.18, 0.21, 0.26, 0.31, 0.36, 0.41, 0.46, 0.5, 0.54, 0.58, 0.62, 0.66, 0.7, 0.74, 0.78, 0.82, 0.86 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.11, 0.19, 0.25, 0.29, 0.34, 0.39, 0.44, 0.49, 0.56, 0.65, 0.74, 0.82, 0.88, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = false,
        },
        // Institutional dormitories (Residential)
        new OccupancyType
        {
            Name = "RES5",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.07, 0.1, 0.14, 0.15, 0.15, 0.16, 0.18, 0.2, 0.23, 0.26, 0.3, 0.34, 0.38, 0.42, 0.47, 0.52, 0.57, 0.62, 0.67, 0.72, 0.77, 0.82, 0.87, 0.92 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.38, 0.6, 0.73, 0.81, 0.88, 0.94, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
        // Nursing Homes (Residential)
        new OccupancyType
        {
            Name = "RES6",
            StructureDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.07, 0.1, 0.14, 0.15, 0.15, 0.16, 0.18, 0.2, 0.23, 0.26, 0.3, 0.34, 0.38, 0.42, 0.47, 0.52, 0.57, 0.62, 0.67, 0.72, 0.77, 0.82, 0.87, 0.92 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                new double[] { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },
                new double[] { 0, 0, 0, 0, 0, 0.38, 0.6, 0.73, 0.81, 0.88, 0.94, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                strictOnX: true, SortOrder.Ascending, strictOnY: false, SortOrder.Ascending),
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
        },
    ];
}
