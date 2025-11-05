using SPTarkov.Server.Core.Models.Enums;

namespace _enableLabyrinth.Models;

public record ModConfig
{
    public bool ChangePmcExfilTimers { get; set; }
    public required ExfilTimers PrimaryPmcExfilTimer { get; init; }
    public bool GuaranteeSecretExfilKey { get; set; }
    
}

public record ExfilTimers
{
    public double ExfiltrationTime { get; set; }
    public required string ExfiltrationType { get; set; }
    public double ElapsedSecondsBeforeAvailable { get; set; }
}