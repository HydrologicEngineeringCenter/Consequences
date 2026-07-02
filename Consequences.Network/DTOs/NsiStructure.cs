using System.Text.Json.Serialization;

namespace Consequences.Network.DTOs;

public sealed record NsiStructure
{
    [JsonPropertyName("bid")]        public string Bid        { get; init; } = "";
    [JsonPropertyName("bldgtype")]   public string Bldgtype   { get; init; } = "";
    [JsonPropertyName("bldheight")]  public float  BldHeight  { get; init; }
    [JsonPropertyName("cbfips")]     public string Cbfips     { get; init; } = "";
    [JsonPropertyName("creprcnt")]   public float  CreProcnt  { get; init; }
    [JsonPropertyName("crerank")]    public float  CreRank    { get; init; }
    [JsonPropertyName("depindex")]   public float  DepIndex   { get; init; }
    [JsonPropertyName("fd_id")]      public long   FdId       { get; init; }
    [JsonPropertyName("firmzone")]   public string Firmzone   { get; init; } = "";
    [JsonPropertyName("found_ht")]   public float  FoundHt    { get; init; }
    [JsonPropertyName("found_type")] public string FoundType  { get; init; } = "";
    [JsonPropertyName("ftprntid")]   public string FtprntId   { get; init; } = "";
    [JsonPropertyName("ftprntsqft")] public float  FtprntSqft { get; init; }
    [JsonPropertyName("ftprntsrc")]  public string FtprntSrc  { get; init; } = "";
    [JsonPropertyName("fullrep")]    public double FullRep    { get; init; }
    [JsonPropertyName("grnd_elv_m")] public double GrndElvM   { get; init; }
    [JsonPropertyName("ground_elv")] public double GroundElv  { get; init; }
    [JsonPropertyName("med_yr_blt")] public int    MedYrBlt   { get; init; }
    [JsonPropertyName("novehprob")]  public float  NoVehProb  { get; init; }
    [JsonPropertyName("num_story")]  public int    NumStory   { get; init; }
    [JsonPropertyName("o65disable")] public float  O65disable { get; init; }
    [JsonPropertyName("occtype")]    public string Occtype    { get; init; } = "";
    [JsonPropertyName("pctlowclr")]  public float  PctLowClr  { get; init; }
    [JsonPropertyName("pop2amo65")]  public float  Pop2amo65  { get; init; }
    [JsonPropertyName("pop2amu65")]  public float  Pop2amu65  { get; init; }
    [JsonPropertyName("pop2pmo65")]  public float  Pop2pmo65  { get; init; }
    [JsonPropertyName("pop2pmu65")]  public float  Pop2pmu65  { get; init; }
    [JsonPropertyName("resunits")]   public int    ResUnits   { get; init; }
    [JsonPropertyName("source")]     public string Source     { get; init; } = "";
    [JsonPropertyName("sqft")]       public float  Sqft       { get; init; }
    [JsonPropertyName("st_damcat")]  public string StDamcat   { get; init; } = "";
    [JsonPropertyName("static_bfe")] public float  StaticBfe  { get; init; }
    [JsonPropertyName("students")]   public float  Students   { get; init; }
    [JsonPropertyName("u65disable")] public float  U65disable { get; init; }
    [JsonPropertyName("usastrucid")] public string UsaStrucId { get; init; } = "";
    [JsonPropertyName("val_cont")]   public double ValCont    { get; init; }
    [JsonPropertyName("val_struct")] public double ValStruct  { get; init; }
    [JsonPropertyName("val_vehic")]  public double ValVehic   { get; init; }
    [JsonPropertyName("vehperunit")] public float  VehPerUnit { get; init; }
    [JsonPropertyName("x")]          public double X          { get; init; }
    [JsonPropertyName("y")]          public double Y          { get; init; }
    [JsonPropertyName("zone_sub")]   public string ZoneSub    { get; init; } = "";
}
