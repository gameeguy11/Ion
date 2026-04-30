using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace IonRodMod
{
    [BepInPlugin("com.gamerguy11.ionrod", "Ion Reactor Rod", "1.0.0")]
    public class IonRodPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static TechType IonRodTech;

        // Optional: keep your own table for clarity
        public static readonly Dictionary<TechType, float> IonRodCharge =
            new Dictionary<TechType, float>(TechTypeExtensions.sTechTypeComparer);

        private void Awake()
        {
            Log = Logger;
            RegisterIonRod();
            RegisterIonRodAsReactorFuel();

            // Harmony is no longer strictly needed, but harmless to keep
            Harmony.CreateAndPatchAll(typeof(IonRodPlugin).Assembly, "IonRodMod");
            Log.LogInfo("[IonRod] Loaded successfully.");
        }

        // ----------------- REGISTER ION ROD ITEM -----------------
        private void RegisterIonRod()
        {
            var info = PrefabInfo.WithTechType(
                "IonReactorRod",
                "Ion Reactor Rod",
                "High-capacity ion-powered reactor fuel rod."
            ).WithIcon(LoadIcon("IonReactorRod", TechType.ReactorRod));

            IonRodTech = info.TechType;

            var prefab = new CustomPrefab(info);

            prefab.SetGameObject(new CloneTemplate(info, TechType.ReactorRod));
            prefab.SetEquipment(EquipmentType.NuclearReactor);

            prefab.SetRecipe(new RecipeData
            {
                craftAmount = 1,
                Ingredients =
                {
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.Glass, 1),
                    new Ingredient(TechType.PrecursorIonCrystal, 2)
                }
            });

            prefab.Register();

            CraftTreeHandler.AddCraftingNode(
                CraftTree.Type.Fabricator,
                IonRodTech,
                "Resources",
                "Electronics"
            );

            KnownTechHandler.UnlockOnStart(IonRodTech);

            // Your own logical energy value
            IonRodCharge[IonRodTech] = 25000f;

            Log.LogInfo("[IonRod] Registered Ion Reactor Rod successfully.");
        }

        // ----------------- REGISTER AS REACTOR FUEL -----------------
        private void RegisterIonRodAsReactorFuel()
        {
            try
            {
                // BaseNuclearReactor has: private static Dictionary<TechType, float> charge;
                var chargeField = typeof(BaseNuclearReactor).GetField(
                    "charge",
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (chargeField == null)
                {
                    Log.LogError("[IonRod] Could not find BaseNuclearReactor.charge field.");
                    return;
                }

                var chargeDict = chargeField.GetValue(null) as Dictionary<TechType, float>;
                if (chargeDict == null)
                {
                    Log.LogError("[IonRod] BaseNuclearReactor.charge is null.");
                    return;
                }

                float energy = 25000f; // tweak this to balance
                chargeDict[IonRodTech] = energy;

                Log.LogInfo($"[IonRod] Registered {IonRodTech} as reactor fuel with energy = {energy}.");
            }
            catch (Exception e)
            {
                Log.LogError("[IonRod] Exception while registering reactor fuel: " + e);
            }
        }

        // ----------------- ICON LOADER -----------------
        private Sprite LoadIcon(string baseName, TechType fallback)
        {
            string modFolder = Path.GetDirectoryName(Info.Location);
            string assetsDir = Path.Combine(modFolder, "Assets");

            string[] exts = { ".png", ".jpg", ".jpeg", ".webp" };

            foreach (var ext in exts)
            {
                string path = Path.Combine(assetsDir, baseName + ext);
                if (!File.Exists(path))
                    continue;

                Texture2D tex = ImageUtils.LoadTextureFromFile(path);
                if (tex != null)
                {
                    return Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }
            }

            return SpriteManager.Get(fallback);
        }
    }
}
