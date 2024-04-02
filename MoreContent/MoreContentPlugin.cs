using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit; // Required for working with OpCodes
using UnityEngine; // Necessary for Debug.Log
using System.Linq; // Add this line here

[BepInPlugin("com.yourdomain.MoreContent", "More Content", "1.0.0")]
public class MoreContentPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Initialize Harmony
        var harmony = new Harmony("com.yourdomain.MoreContent");
        harmony.PatchAll();
        Debug.Log("MoreContent mod has been loaded and patches applied.");
    }
}

[HarmonyPatch(typeof(MainMenuHandler))]
[HarmonyPatch("Start")]
public static class MainMenuHandlerStartPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        bool found = false;

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldc_I4_4)
            {
                codes[i].opcode = OpCodes.Ldc_I4_8;
                found = true;
                Debug.Log("Transpiler found and changed max player count to 8.");
            }
        }

        if (!found)
        {
            Debug.LogWarning("Transpiler did not find the max player count value to change.");
        }

        // AsEnumerable() is recognized with the System.Linq using directive
        return codes.AsEnumerable();
    }
}
