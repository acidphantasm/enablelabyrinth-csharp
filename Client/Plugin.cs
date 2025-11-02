using acidphantasm_enablelabyrinth.Patches;
using BepInEx;

namespace acidphantasm_enablelabyrinth
{
    [BepInPlugin("com.acidphantasm.enablelabyrinth", "acidphantasm-enablelabyrinth", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new LocationSceneAwakePatch().Enable();
        }
    }
}
