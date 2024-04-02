using BepInEx;
using HarmonyLib;
using System;

using UnityEngine;
using DefaultNamespace;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;


[BepInPlugin("com.yourdomain.MoreCustomization", "More Customization", "1.0.0")]
public class MoreContentPlugin : BaseUnityPlugin
{
    private const string modGUID = "x001.MoreCustomization";
    private const string modName = "MoreCustomization";
    private const string modVersion = "1.0.0";

    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        harmony.PatchAll();

        Logger.LogInfo($"Plugin {modGUID} is loaded!");
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "RunTerminal")]
    public static class PlayerCustomizer_RunTerminal_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_3)
                {
                    codes[i].opcode = OpCodes.Ldc_I4;
                    codes[i].operand = 128;
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "RunTerminal")]
    public static class PlayerCustomizer_RunTerminal_Paste
    {
        [HarmonyPostfix]
        public static void PostfixRunTerminal(PlayerCustomizer __instance)
        {
            // Check for Ctrl+V (paste) input
            if (UnityInput.Current.GetKey(KeyCode.LeftControl) || UnityInput.Current.GetKey(KeyCode.RightControl) && UnityInput.Current.GetKey(KeyCode.V))
            {
                // Access clipboard text
                string clipboardText = UnityEngine.GUIUtility.systemCopyBuffer; // This is now recognized

                // Apply clipboard text
                __instance.faceText.text = clipboardText; // Set the pasted text
                Debug.Log($"Pasted text from clipboard: {clipboardText}");
            }

            if (__instance.faceText != null)
            {
                __instance.faceText.enableAutoSizing = true;
                __instance.faceText.fontSizeMin = 10;
                __instance.faceText.fontSizeMax = 40;

                Debug.Log($"[PlayerCustomizer] Auto-sizing enabled. Min Size: {__instance.faceText.fontSizeMin}, Max Size: {__instance.faceText.fontSizeMax}");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizer))]
    public static class Patch_PlayerCustomizer
    {
        [HarmonyPatch("SetFaceText"), HarmonyPostfix]
        public static void PostfixSetFaceText(ref PlayerCustomizer __instance, string text)
        {
            if (__instance.playerInTerminal)
            {
                Debug.Log($"Patching SetFaceText with full text: {text}");

                __instance.faceText.text = text;
                __instance.playerInTerminal.refs.visor.visorFaceText.text = text;

                if (__instance.faceText != null)
                {
                    __instance.faceText.enableAutoSizing = true;
                    __instance.faceText.fontSizeMin = 10;
                    __instance.faceText.fontSizeMax = 40;

                    Debug.Log($"[SetFaceText] Text auto-sized. Min: {__instance.faceText.fontSizeMin}, Max: {__instance.faceText.fontSizeMax}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerVisor), "RPCA_SetVisorText")]
    public static class Patch_PlayerVisor
    {
        [HarmonyPrefix]
        public static bool PrefixRPCA_SetVisorText(PlayerVisor __instance, ref string text)
        {
            // Assuming __instance has a public or accessible TextMeshProUGUI component named visorText
            if (__instance.visorFaceText != null)
            {
                __instance.visorFaceText.text = text;
                __instance.visorFaceText.enableAutoSizing = true;
                __instance.visorFaceText.fontSizeMin = 10;
                __instance.visorFaceText.fontSizeMax = 40;

                Debug.Log($"[PlayerVisor] Adjusted text size for '{text}'. Min Font Size: {__instance.visorFaceText.fontSizeMin}, Max Font Size: {__instance.visorFaceText.fontSizeMax}");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "OnChangeFaceSize")]
    public class PlayerCustomizer_OnChangeFaceSize_Patch
    {
        static bool Prefix(PlayerCustomizer __instance, bool smaller)
        {
            // Assuming there's a way to get the current size directly. You might need to adjust this.
            float currentSize = __instance.faceText.transform.localScale.x;
            float changeAmount = 0.05f; // Example step size for each button press

            // Adjust the size up or down based on the button pressed
            float newSize = smaller ? currentSize - changeAmount : currentSize + changeAmount;

            // Directly set the new size
            __instance.faceText.transform.localScale = new Vector3(newSize, newSize, 1f);

            // Log for debugging
            Debug.Log($"Adjusted face size to: {newSize}");

            // Since we've directly set the size, skip the original method to prevent it from clamping the value
            return false;
        }
    }
}
