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


[BepInPlugin("com.yourdomain.InfiniteColors", "Infinite Colors", "1.0.0")]
public class Class1 : BaseUnityPlugin
{
    private const string modGUID = "x001.Infinite Colors";
    private const string modName = "InfiniteColors";
    private const string modVersion = "1.0.0";

    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        harmony.PatchAll();

        Logger.LogInfo($"Plugin {modGUID} is loaded!");
    }

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
            // Adjust these values as needed to make the sliders larger and more visible
            float preferredWidth = 40;
            float preferredHeight = 300;

            var redSlider = CreateColorSlider(colorPickerContainer.transform, Color.red, __instance, preferredWidth, preferredHeight);
            var greenSlider = CreateColorSlider(colorPickerContainer.transform, Color.green, __instance, preferredWidth, preferredHeight);
            var blueSlider = CreateColorSlider(colorPickerContainer.transform, Color.blue, __instance, preferredWidth, preferredHeight);

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




        private static Slider CreateColorSlider(Transform parent, Color sliderColor, PlayerCustomizer customizer, float preferredWidth, float preferredHeight)
        {
            // Slider GameObject
            GameObject sliderGO = new GameObject($"{sliderColor}Slider", typeof(RectTransform));
            sliderGO.transform.SetParent(parent, false);

            LayoutElement layoutElement = sliderGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.preferredHeight = preferredHeight;

            // Set up the RectTransform for the slider
            var sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(40, 300); // Adjust size as needed


            // Slider Component
            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = sliderColor == Color.red ? 1f : 0f;

            // Slider visuals - Assuming you have custom sprites for these
            var bgImage = new GameObject("Background").AddComponent<Image>();
            bgImage.transform.SetParent(sliderGO.transform, false);
            bgImage.color = new Color(0.2f, 0.2f, 0.2f); // Darker background for contrast
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
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white; // Consider using a custom sprite here

            // Set slider components
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.BottomToTop;

            // Determine the slider's initial value based on the headColor
            if (sliderColor == Color.red)
                slider.value = customizer.headColor.color.r;
            else if (sliderColor == Color.green)
                slider.value = customizer.headColor.color.g;
            else if (sliderColor == Color.blue)
                slider.value = customizer.headColor.color.b;

            // Additional Styling (Optional)
            // Here you can add shadows, borders, or gradients as needed using additional components.

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


            // Update the head color locally.
            if (customizer.headColor != null)
            {
                customizer.headColor.color = newColor;
            }

            // Log the new color for debugging.
            Debug.Log($"Updating color to: {newColor}, RGB({red.value}, {green.value}, {blue.value})");

            // Check if the PlayerVisor component exists and apply the color through it
            PlayerVisor playerVisor = customizer.playerInTerminal?.GetComponent<PlayerVisor>();
            if (playerVisor != null)
            {
                playerVisor.ApplyVisorColor(newColor);
            }
        }

    }
}