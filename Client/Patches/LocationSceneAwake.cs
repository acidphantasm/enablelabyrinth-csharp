using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace acidphantasm_enablelabyrinth.Patches;

internal class LocationSceneAwakePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LocationScene), nameof(LocationScene.Awake));
    }

    [PatchPrefix]
    static void Prefix(LocationScene __instance)
    {
        var localCopy = new List<ExfiltrationPoint>();
        ExfiltrationPoint exfilToCopy = null;
        var thisIsActuallyLabyrinth = false;
        
        foreach (var exfil in __instance.ExfiltrationPoints)
        {
            if (exfil.Settings.Name == "labir_exit")
            {
                exfilToCopy = exfil;
                localCopy.Add(exfil);
            }

            if (exfil.Settings.Name == "labyrinth_secret_tagilla_key")
            {
                thisIsActuallyLabyrinth = true;
                localCopy.Add(exfil);
            }
        }

        if (localCopy.Count == 2 && exfilToCopy != null && thisIsActuallyLabyrinth)
        {
            var newExfil = LocationScene.Instantiate(exfilToCopy.gameObject);
            newExfil.transform.SetParent(exfilToCopy.transform.parent, true);
                
            var exfilPointComponent = newExfil.GetComponent<ExfiltrationPoint>();
            if (exfilPointComponent != null)
            {
                GameObject.DestroyImmediate(exfilPointComponent);
            }
            
            newExfil.SetActive(true);
            
            var scavComponent = newExfil.AddComponent<ScavExfiltrationPoint>();
            newExfil.name = "The Way Up (scav)";
            scavComponent.name = "The Way Up (scav)";
            scavComponent.Id = "690704dcfdb5f8595e3cb815";
            scavComponent.EligibleIds = new List<string>();
            scavComponent.Requirements = new ExfiltrationRequirement[] {};
            scavComponent.CharismaLevel = 0;
            scavComponent.FenceRep = 0;
            scavComponent.enabled = true;
            scavComponent.Settings.ExfiltrationType = EExfiltrationType.Individual;
            scavComponent.Settings.Chance = 100;
            scavComponent.Settings.EntryPoints = "";
            scavComponent.Settings.ExfiltrationTime = 15;
            scavComponent.Settings.Id = "";
            scavComponent.Settings.MinTime = 0;
            scavComponent.Settings.MaxTime = 0;
            scavComponent.Settings.PlayersCount = 0;
            scavComponent.Settings.Name = "The Way Up (scav)";
            scavComponent.Settings.StartTime = 0;

            localCopy.Add(scavComponent);
            
            __instance.ExfiltrationPoints = localCopy.ToArray();
            
            Logger.LogInfo("Enabled Labyrinth with Scav Exfil.");
        }
    }
}