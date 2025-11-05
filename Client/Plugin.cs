using System;
using acidphantasm_enablelabyrinth.Patches;
using acidphantasm_enablelabyrinth.VersionCheck;
using BepInEx;

namespace acidphantasm_enablelabyrinth
{
    [BepInPlugin("com.acidphantasm.enablelabyrinth", "acidphantasm-enablelabyrinth", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }
            
            new LocationSceneAwakePatch().Enable();
        }
    }
}
