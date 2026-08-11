using Consequences.Scratch.EntryPoints;

// Scratch paper for the Consequences solution.
//
// Each developer owns one file under EntryPoints/ and one folder for their working files.
// Nothing here ships, nothing here is packed, and nothing in the product references it —
// so you are free to leave half-finished experiments lying around. Just make sure the solution compiles.
// CI Will complain if it does not. 
//
// Get your computer name quickly using powershell. enter the following:  '$env:COMPUTERNAME'
const string BrennanWork = "EIWRW4EFAANB455";
const string BrennanVM = "BEAM-VM00";
const string BrennanHome = "BRENNANDESKTOP";
const string JackWork = "IWR-HEC-JS-01";

string pcName = GetPCName();

if (pcName.Equals(BrennanWork) || pcName.Equals(BrennanVM) || pcName.Equals(BrennanHome))
{
    Beam.EntryPoint();
}
if (pcName.Equals(JackWork))
{
    Schonherr.EntryPoint();
}

static string GetPCName() => Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "";
