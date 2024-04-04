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
using Photon.Pun;


[BepInPlugin("com.yourdomain.FaceCustomization", "Face Customization", "1.0.1")]
public class Class1 : BaseUnityPlugin
{
    private const string modGUID = "x002.Face Customization";
    private const string modName = "FaceCustomization";
    private const string modVersion = "1.0.1";

    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        harmony.PatchAll();

        Logger.LogInfo($"Plugin {modGUID} is loaded!");
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "Awake")]
    public class PlayerCustomizer_Awake_Patch
    {
        static void Prefix(PlayerCustomizer __instance)
        {
            float terminalTextScale = Mathf.Lerp(__instance.visorFaceSizeMinMax.x, __instance.visorFaceSizeMinMax.y, 0.5f);
            float headTextScale = Mathf.Lerp(__instance.faceSizeMinMax.x, __instance.faceSizeMinMax.y, 0.5f);

            __instance.faceText.transform.localScale = new Vector3(terminalTextScale, terminalTextScale, 1f);

            if (__instance.playerInTerminal != null)
            {
                __instance.playerInTerminal.refs.visor.visorFaceText.transform.localScale = new Vector3(headTextScale, headTextScale, 1f);
            }

        }
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

    [HarmonyPatch(typeof(PlayerCustomizer))]
    public class PlayerCustomizerPatches
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(PlayerCustomizer __instance, TextMeshProUGUI ___faceText, PhotonView ___view_g)
        {
            if (Input.GetKey(KeyCode.Delete))
            {
                ___view_g.RPC("SetFaceText", RpcTarget.All, "");
            }
            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            {
                string clipboardText = GUIUtility.systemCopyBuffer;
                ___view_g.RPC("SetFaceText", RpcTarget.All, clipboardText);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                string currentText = ___faceText.text;
                string newText = currentText + "\n";
                ___view_g.RPC("SetFaceText", RpcTarget.All, newText);
            }
        }
    }


    [HarmonyPatch("SetFaceText", MethodType.Normal)]
    [HarmonyPrefix]
    static bool PrefixSetFaceText(ref string text, PlayerCustomizer __instance)
    {
        __instance.faceText.text = text;
        if (__instance.playerInTerminal != null)
        {
            __instance.playerInTerminal.refs.visor.visorFaceText.text = text;
        }
        return false;
    }

    [HarmonyPatch(typeof(PlayerVisor), "RPCA_SetVisorText")]
    public static class Patch_PlayerVisor
    {
        [HarmonyPrefix]
        public static bool PrefixRPCA_SetVisorText(PlayerVisor __instance, ref string text)
        {
            __instance.visorFaceText.text = text;
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "RPCA_ChangeFaceSize")]
    public class SyncFaceSizePatch
    {
        static bool Prefix(PlayerCustomizer __instance, bool smaller)
        {
            if (__instance.playerInTerminal == null) return false;

            float terminalScaleStep = 0.05f;
            float faceScaleStep = 0.002f;

            float currentTerminalSize = __instance.faceText.transform.localScale.x;
            float changeAmountTerminal = smaller ? -terminalScaleStep : terminalScaleStep;
            float newTerminalSize = currentTerminalSize + changeAmountTerminal;

            __instance.faceText.transform.localScale = new Vector3(newTerminalSize, newTerminalSize, 1f);

            if (__instance.playerInTerminal != null && __instance.playerInTerminal.refs != null && __instance.playerInTerminal.refs.visor != null)
            {
                float currentFaceSize = __instance.playerInTerminal.refs.visor.visorFaceText.transform.localScale.x;
                float changeAmountFace = smaller ? -faceScaleStep : faceScaleStep;
                float newFaceSize = currentFaceSize + changeAmountFace;

                __instance.playerInTerminal.refs.visor.visorFaceText.transform.localScale = new Vector3(newFaceSize, newFaceSize, 1f);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerVisor), "Awake")]
    public class PlayerVisorAwakePatch
    {
        static void Postfix(PlayerVisor __instance)
        {
            if (__instance.visorFaceText is TextMeshPro textMesh)
            {
                RectTransform rectTransform = textMesh.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(13, 13);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "RPCA_SyncEverything")]
    public class Patch_RPCA_SyncEverything
    {
        [HarmonyPrefix]
        public static void Prefix(int playerId, int colorIndex, string faceText, float faceRotation, float faceSize, PlayerCustomizer __instance)
        {
            // Ensuring the TextMeshPro component gets the updated size settings
            if (__instance.playerInTerminal != null)
            {
                // Convert UI size to the correct visor size if necessary
                float adjustedFaceSize = __instance.playerInTerminal.refs.visor.visorFaceText.transform.localScale.x;
                __instance.playerInTerminal.refs.visor.visorFaceText.fontSize = adjustedFaceSize;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "FaceSizeVisorToUi")]
    public class PlayerCustomizer_FaceSizeVisorToUi_Patch
    {
        static bool Prefix(ref float visorSize, ref float __result, PlayerCustomizer __instance)
        {
            float t = Mathf.InverseLerp(__instance.visorFaceSizeMinMax.x, __instance.visorFaceSizeMinMax.y, visorSize);

            __result = Mathf.LerpUnclamped(__instance.faceSizeMinMax.x, __instance.faceSizeMinMax.y, t);

            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizer), "FaceSizeUiToVisor")]
    public class PlayerCustomizer_FaceSizeUiToVisor_Patch
    {
        static bool Prefix(ref float uiSize, ref float __result, PlayerCustomizer __instance)
        {
            float t = Mathf.InverseLerp(__instance.faceSizeMinMax.x, __instance.faceSizeMinMax.y, uiSize);

            __result = Mathf.LerpUnclamped(__instance.visorFaceSizeMinMax.x, __instance.visorFaceSizeMinMax.y, t);

            return false;
        }
    }

    // Static variable to track the last applied face size
    private static float lastAppliedFaceSize = -1f;

    [HarmonyPatch(typeof(PlayerCustomizer), "RPCA_PlayerLeftTerminal")]
    public static class PlayerCustomizer_RPCA_PlayerLeftTerminal_Patch
    {
        static bool Prefix(PlayerCustomizer __instance, bool apply)
        {
            if (__instance.playerInTerminal == null)
            {
                return false; // Skip original method if there's no player in terminal.
            }

            // Accessing the custom color. Assuming headColor holds the color selected via your custom UI.
            Color customColor = __instance.headColor.color;

            if (apply)
            {
                // Apply the custom color to the visor.
                __instance.playerInTerminal.refs.visor.ApplyVisorColor(customColor);
                // Assuming you have a mechanism to save other customizations and sync across clients.
                __instance.playerInTerminal.refs.visor.visorFaceText.text = __instance.faceText.text;

                lastAppliedFaceSize = __instance.FaceSize;

            }
            else
            {
                // Revert to initial color if not applying.
                __instance.playerInTerminal.refs.visor.ApplyVisorColor(__instance.headColor.color);
            }

            // This line is moved outside the if (apply) check, so it executes regardless of the apply value.
            __instance.playerInTerminal.data.isInCostomizeTerminal = false;

            __instance.playerInTerminal = null;

            // Prevent the original method from executing since we've handled the logic.
            return false;
        }
    }


    // Patch for the RPCA_EnterTerminal method to use lastAppliedFaceSize if available
    [HarmonyPatch(typeof(PlayerCustomizer), "RPCA_EnterTerminal")]
    public class RPCA_EnterTerminal_Patch
    {
        static void Postfix(int playerId, PlayerCustomizer __instance)
        {
            if (lastAppliedFaceSize > 0f)
            {
                // Use lastAppliedFaceSize instead of startFaceSize
                __instance.FaceSize = lastAppliedFaceSize;
            }
            else
            {
                // Logic to calculate startFaceSize if lastAppliedFaceSize is not available
                // This might be the first entry or reset conditions
                float num = __instance.FaceSizeVisorToUi(__instance.playerInTerminal.refs.visor.visorFaceText.transform.localScale.x);
                __instance.FaceSize = num;
            }
        }
    }

}
