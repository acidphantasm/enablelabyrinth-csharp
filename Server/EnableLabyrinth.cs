using System.Reflection;
using System.Runtime.CompilerServices;
using _enableLabyrinth.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _enableLabyrinth;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.acidphantasm.enablelabyrinth";
    public override string Name { get; init; } = "Enable Labyrinth";
    public override string Author { get; init; } = "acidphantasm";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.3");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 90000)]
public class EnableLabyrinth(
    DatabaseService databaseService,
    ModHelper modHelper,
    JsonUtil jsonUtil,
    ConfigServer configServer,
    ICloner cloner,
    ISptLogger<EnableLabyrinth> logger)
    : IOnLoad
{
    private readonly LocationConfig _locationConfig = configServer.GetConfig<LocationConfig>();
    private readonly string _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    private Location _labyrinthData = null!;
    private LocationBase _labyrinthBase = null!;
    private Dictionary<string, BotType?> _databaseBots = null!;
    private ModConfig _config = null!;
    
    public Task OnLoad()
    { 
        _labyrinthData = databaseService.GetLocations().Labyrinth;
        _labyrinthBase = databaseService.GetLocations().Labyrinth.Base;
        _databaseBots = databaseService.GetBots().Types;
        
        _config = jsonUtil.DeserializeFromFile<ModConfig>(_modPath + "/config.json") ?? throw new ArgumentNullException();
        
        AdjustLabyrinthBase();
        AdjustLabyrinthScavRaidTimeSettings();
        AdjustExfils();
        AddSecretKeyGuarantee();

        return Task.CompletedTask;
    }
    
    private void AdjustLabyrinthBase()
    {
        _labyrinthBase.AccessKeys = [];
        _labyrinthBase.AccessKeysPvE = [];
        _labyrinthBase.IconY = 350f;
        _labyrinthBase.Enabled = true;
        _labyrinthBase.DisabledForScav = false;
        _labyrinthBase.ForceOnlineRaidInPVE = false;
    }

    private void AdjustLabyrinthScavRaidTimeSettings()
    {
        _locationConfig.ScavRaidTimeSettings.Maps["labyrinth"] =
            cloner.Clone(_locationConfig.ScavRaidTimeSettings.Maps["factory4_day"]);
    }

    private void AdjustExfils()
    {
        // Add Scav Exfil to AllExtracts
        AddScavExfilToAllExtracts();
        AddScavExfilToBaseExtracts();
        
        // Adjust the normal Pmc Exfil if enabled
        if (!_config.ChangePmcExfilTimers) return;
        
        var extractList = _labyrinthBase.Exits.ToList();
        var pmcExit = extractList.FirstOrDefault(e => e.Name == "labir_exit");
        if (pmcExit != null)
        {
            var exfiltrationTypeValueFromConfig = _config.PrimaryPmcExfilTimer.ExfiltrationType;

            if (!Enum.TryParse<ExfiltrationType>(exfiltrationTypeValueFromConfig, ignoreCase: true, out var parsedExfiltrationType))
            {
                logger.Warning($"Invalid ExfiltrationType Config Value: '{exfiltrationTypeValueFromConfig}', defaulting to SharedTimer.");
                parsedExfiltrationType = ExfiltrationType.SharedTimer;
            }
            
            pmcExit.ExfiltrationTime = _config.PrimaryPmcExfilTimer.ExfiltrationTime;
            pmcExit.ExfiltrationTimePVE = _config.PrimaryPmcExfilTimer.ExfiltrationTime;
            pmcExit.ExfiltrationType = parsedExfiltrationType;
                
            // Timer requirements do Random.Range(MinTime (inclusive), MaxTime (exclusive)) on the client side
            pmcExit.MinTime = _config.PrimaryPmcExfilTimer.ElapsedSecondsBeforeAvailable;
            pmcExit.MinTimePVE = _config.PrimaryPmcExfilTimer.ElapsedSecondsBeforeAvailable;
            pmcExit.MaxTime = _config.PrimaryPmcExfilTimer.ElapsedSecondsBeforeAvailable;
            pmcExit.MaxTimePVE = _config.PrimaryPmcExfilTimer.ElapsedSecondsBeforeAvailable;
        }
            
        // Reassign original Exfils
        _labyrinthData.Base.Exits = extractList;

    }

    private void AddScavExfilToAllExtracts()
    {
        var allExtractList = _labyrinthData.AllExtracts.ToList();
        
        allExtractList.Add(new AllExtractsExit
        {
            Chance = 100,
            ChancePVE = 100,
            Count = 0,
            CountPVE = 0,
            EntryPoints = "",
            EventAvailable = false,
            ExfiltrationTime = 30,
            ExfiltrationTimePVE = 30,
            ExfiltrationType = ExfiltrationType.Individual,
            Id = "",
            MaxTime = 0,
            MaxTimePVE = 0,
            MinTime = 0,
            MinTimePVE = 0,
            Name = "The Way Up (scav)",
            PassageRequirement = RequirementState.None,
            PlayersCount = 0,
            PlayersCountPVE = 0,
            RequiredSlot = EquipmentSlots.FirstPrimaryWeapon,
            RequirementTip = "",
            Side = "Scav"
        });
        
        _labyrinthData.AllExtracts = allExtractList;
    }

    private void AddScavExfilToBaseExtracts()
    {
        var labyrinthBaseExtracts = _labyrinthBase.Exits.ToList();
        
        labyrinthBaseExtracts.Add(new Exit
        {
            Chance = 100,
            ChancePVE = 100,
            Count = 0,
            CountPVE = 0,
            EntryPoints = "",
            EventAvailable = false,
            ExfiltrationTime = 30,
            ExfiltrationTimePVE = 30,
            ExfiltrationType = ExfiltrationType.Individual,
            Id = "",
            MaxTime = 0,
            MaxTimePVE = 0,
            MinTime = 0,
            MinTimePVE = 0,
            Name = "The Way Up (scav)",
            PassageRequirement = RequirementState.None,
            PlayersCount = 0,
            PlayersCountPVE = 0,
            RequirementTip = ""
        });
        
        _labyrinthBase.Exits = labyrinthBaseExtracts;
    }

    private void AddSecretKeyGuarantee()
    {
        if (!_config.GuaranteeSecretExfilKey) return;
        if (!_databaseBots.TryGetValue("bosstagillaagro", out var shadowOfTagilla)) return;
        
        shadowOfTagilla.BotInventory.Items.SpecialLoot["67e183377c6c2011970f3149"] = 1;
        shadowOfTagilla.BotGeneration.Items.SpecialItems.Weights = new Dictionary<double, double>
        {
            { 0, 0 },
            { 1, 100}
        };
    }
}