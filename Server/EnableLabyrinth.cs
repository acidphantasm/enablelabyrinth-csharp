using System.Reflection;
using System.Runtime.CompilerServices;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _enableLabyrinth;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.acidphantasm.enablelabyrinth";
    public override string Name { get; init; } = "Enable Labyrinth";
    public override string Author { get; init; } = "acidphantasm";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 90000)]
public class DynamicMapsServer(
    DatabaseService databaseService,
    ModHelper modHelper,
    CustomItemService customItemService,
    ConfigServer configServer,
    ICloner cloner)
    : IOnLoad
{
    private LocationConfig _locationConfig = configServer.GetConfig<LocationConfig>();
    
    public Task OnLoad()
    { 
        AdjustLabyrinth();
        return Task.CompletedTask;
    }

    private void AdjustLabyrinth()
    {
        var labyrinth = databaseService.GetLocations().Labyrinth;
        var labyrinthBase = databaseService.GetLocations().Labyrinth.Base;
        labyrinthBase.AccessKeys = [];
        labyrinthBase.AccessKeysPvE = [];
        labyrinthBase.Enabled = true;
        labyrinthBase.DisabledForScav = false;
        labyrinthBase.ForceOnlineRaidInPVE = false;

        _locationConfig.ScavRaidTimeSettings.Maps["labyrinth"] =
            cloner.Clone(_locationConfig.ScavRaidTimeSettings.Maps["factory4_day"]);

        var extractList = labyrinth.AllExtracts.ToList();
        
        foreach (var exit in extractList.ToList())
        {
            var newExit = cloner.Clone(exit);
            newExit.Side = "Scav";
            newExit.Name = "custom_labyrinth_scav_exfil";
            newExit.MinTime = 0;
            newExit.MinTimePVE = 0;
            newExit.MaxTime = 0;
            newExit.MaxTimePVE = 0;
            newExit.ExfiltrationType = ExfiltrationType.Individual;
            
            extractList.Add(newExit);
        }
        
        labyrinth.AllExtracts = extractList;
    }
}