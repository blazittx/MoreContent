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

    //NEW COLORS

    [HarmonyPatch(typeof(PlayerCustomizer))]
    public static class PlayerCustomizer_AddColorPicker_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void PostfixAwake(PlayerCustomizer __instance)
        {
            // Create Color Picker Container
            GameObject colorPickerContainer = new GameObject("ColorPicker");
            colorPickerContainer.transform.SetParent(__instance.colorsRoot.transform, false);
            var containerLayoutGroup = colorPickerContainer.AddComponent<HorizontalLayoutGroup>();
            containerLayoutGroup.childControlWidth = true;
            containerLayoutGroup.childControlHeight = true;
            containerLayoutGroup.childForceExpandWidth = true;
            containerLayoutGroup.childForceExpandHeight = false;
            containerLayoutGroup.spacing = 10; // Add some spacing between sliders

            // Create Sliders for R, G, B
            var redSlider = CreateColorSlider(colorPickerContainer.transform, Color.red);
            var greenSlider = CreateColorSlider(colorPickerContainer.transform, Color.green);
            var blueSlider = CreateColorSlider(colorPickerContainer.transform, Color.blue);

            // Set sliders as vertical
            redSlider.direction = Slider.Direction.BottomToTop;
            greenSlider.direction = Slider.Direction.BottomToTop;
            blueSlider.direction = Slider.Direction.BottomToTop;

            // Add listeners to update color in real-time
            redSlider.onValueChanged.AddListener(newValue => UpdateColor(__instance, redSlider, greenSlider, blueSlider));
            greenSlider.onValueChanged.AddListener(newValue => UpdateColor(__instance, redSlider, greenSlider, blueSlider));
            blueSlider.onValueChanged.AddListener(newValue => UpdateColor(__instance, redSlider, greenSlider, blueSlider));

            Debug.Log("Color pickers have been created and listeners are set.");
        }

        private static Slider CreateColorSlider(Transform parent, Color sliderColor)
        {
            // Slider GameObject
            GameObject sliderGO = new GameObject($"{sliderColor}Slider", typeof(RectTransform));
            sliderGO.transform.SetParent(parent, false);

            // Set up the RectTransform for the slider to size correctly
            var sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(20, 150); // Set the width and height of the slider

            // Slider Component
            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = sliderColor == Color.red ? 1f : 0f; // Default value based on color channel

            // Slider visuals
            var bgImage = new GameObject("Background").AddComponent<Image>();
            bgImage.transform.SetParent(sliderGO.transform, false);
            bgImage.color = new Color(0.8f, 0.8f, 0.8f); // Light grey background
            bgImage.rectTransform.sizeDelta = new Vector2(20, 150);

            var fillArea = new GameObject("Fill Area").AddComponent<RectTransform>();
            fillArea.SetParent(sliderGO.transform, false);
            fillArea.sizeDelta = new Vector2(10, 150);

            var fill = new GameObject("Fill").AddComponent<Image>();
            fill.transform.SetParent(fillArea, false);
            fill.color = sliderColor;
            fill.rectTransform.sizeDelta = new Vector2(10, 150);

            // Handle Area
            var handleArea = new GameObject("HandleSlideArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGO.transform, false);
            handleArea.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 150);

            // Handle
            GameObject handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20); // Set the size of the handle
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white; // Set the handle color to white

            // Set slider components
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.BottomToTop; // Set the direction to BottomToTop for vertical slider

            return slider;
        }

        [HarmonyPatch(typeof(PlayerCustomizer), "RPCA_PlayerLeftTerminal")]
        public static class PlayerCustomizer_RPCA_PlayerLeftTerminal_Patch
        {
            static bool Prefix(PlayerCustomizer __instance, bool apply)
            {
                if (__instance.playerInTerminal == null)
                {
                    return false; // Skip original method if there's no player in terminal.
                }

                // Here you apply the custom color. Assuming headColor holds the color selected via your custom UI.
                Color customColor = __instance.headColor.color;

                if (apply)
                {
                    // Apply the custom color to the visor.
                    __instance.playerInTerminal.refs.visor.ApplyVisorColor(customColor);

                    // Save custom color (optional, depends on your implementation).
                    SaveCustomColorToPlayerPrefs(customColor);

                    // Assuming there's a mechanism to save other customizations and sync across clients.
                    // Adjust as necessary.
                    __instance.playerInTerminal.refs.visor.visorFaceText.text = __instance.faceText.text;
                    __instance.playerInTerminal.data.isInCostomizeTerminal = false;
                    // Update any necessary state here...
                }
                else
                {
                    // Revert to initial color if not applying.
                    __instance.playerInTerminal.refs.visor.ApplyVisorColor(__instance.headColor.color);
                }

                __instance.playerInTerminal = null;

                // Prevent the original method from executing since we've handled the logic.
                return false;
            }

            private static void SaveCustomColorToPlayerPrefs(Color color)
            {
                // Convert color to a savable format (e.g., a string) and save it.
                PlayerPrefs.SetString("CustomColor", ColorUtility.ToHtmlStringRGBA(color));
                PlayerPrefs.Save();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("SpawnColors")]
        public static void PostfixSpawnColors(PlayerCustomizer __instance)
        {
            // Assuming colorsRoot is the parent GameObject where the color selectors are instantiated
            Transform colorsRootTransform = __instance.colorsRoot.transform;

            // Iterate through the child objects of colorsRoot
            for (int i = 0; i < colorsRootTransform.childCount; i++)
            {
                Transform child = colorsRootTransform.GetChild(i);

                // Check if the child has a ColorSelector component
                ColorSelector colorSelector = child.GetComponent<ColorSelector>();
                if (colorSelector != null)
                {
                    // Disable the GameObject and its components
                    child.gameObject.SetActive(false);
                    colorSelector.enabled = false;
                }
            }
        }

        private static void UpdateColor(PlayerCustomizer customizer, Slider red, Slider green, Slider blue)
        {
            // Create the new color from the slider values.
            Color newColor = new Color(red.value, green.value, blue.value);

            // Log the new color for debugging.
            Debug.Log($"Updating color to: {newColor}, RGB({red.value}, {green.value}, {blue.value})");

            // Update the head color locally.
            if (customizer.headColor != null)
            {
                customizer.headColor.color = newColor;
                Debug.Log("Head color updated locally.");
            }
            else
            {
                Debug.LogError("Head color component not found.");
                return; // Exit if the head color component isn't found.
            }

            // Assuming your game's logic directly applies the RGB color to the visor.
            if (customizer.playerInTerminal != null)
            {
                customizer.playerInTerminal.refs.visor.ApplyVisorColor(newColor);
                Debug.Log("Visor color updated locally.");
            }
            else
            {
                Debug.LogError("Player in terminal not found.");
            }
        }

    }
}
