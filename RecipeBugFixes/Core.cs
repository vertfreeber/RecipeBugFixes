using HarmonyLib;
using HarmonyLib.Tools;
using Il2CppArrabbiata;
using Il2CppArrabbiata.Common.Database;
using Il2CppArrabbiata.Common.NewDatabase;
using Il2CppCyanide;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using MelonLoader.Utils;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.Subsystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityPlugin;
using static Il2CppCyanide.TypeCollections;


[assembly: MelonInfo(typeof(RecipeBugFixes.Core), "RecipeBugFixes", "1.0.0", "Vubert", null)]
[assembly: MelonGame("Cyanide", "Chef Life : A Restaurant Simulator")]



namespace RecipeBugFixes
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
            var harmony = new HarmonyLib.Harmony("com.pepperdose.recipebugfixes");
        }
    }

    [HarmonyPatch(typeof(Il2CppArrabbiata.RecipeDatabase), nameof(RecipeDatabase.Load))]
    public static class PatchRecipeDatabase
    {

        private static void Prefix(ref Il2CppSystem.Collections.Generic.List<Il2CppArrabbiata.Common.Database.Recipe> recipes, Il2CppSystem.Collections.Generic.List<Il2CppArrabbiata.Common.Database.PhaseTemplate> phaseTemplates, Il2CppSystem.Collections.Generic.List<Il2CppArrabbiata.Common.NewDatabase.PreparationTableTemplate> preparationTableTemplates)
        {
            List<(string, int)> recipePotionstoIncrease = [("PureeVegan_Cooked_RA", 4), ("VealStock_Cooked_RA", 4), ("PrepLambChop_RA", 4)];
            List<string> ingredientsThicknessRemoved = ["LonzuWithOnionAndGarlic_RA"];

            for (int i = 0; i < recipes.Count; i++)
            {
                Il2CppArrabbiata.Common.Database.Recipe recipe = recipes[i];
                
                if(recipePotionstoIncrease.Any(t => t.Item1 == recipe.recipeAsync.ToString())) IncreasePotions(recipe, 4); // increase Potions of all recipes listed
                if(recipe.recipeAsync.ToString() == "LonzuWithOnionAndGarlic_RA") SetPropertyToEmptyString(recipe.ingredients[1], "_thickness_k__BackingField"); // that's the chopped Lonzu
                //if (recipe.recipeAsync.ToString() == "PeasMintSoup_RA") recipe._allowRecipeActions_k__BackingField = false;             
            }

        }

        private static void IncreasePotions(Il2CppArrabbiata.Common.Database.Recipe recipe, int targetAmount)
        {

            var baseCraft = recipe.crafts[^1];

            if (targetAmount < recipe.crafts.Count) throw new Exception("Target amount is less than currentAmount, reducing Potions not Implemented yet");


            for (int i = 0; i < (targetAmount - recipe.crafts.Count); i++)
            {
                var clone = new Il2CppArrabbiata.Common.Database.RecipeCraft();

                clone._recipe_k__BackingField = baseCraft._recipe_k__BackingField;
                clone._craft_k__BackingField = baseCraft._craft_k__BackingField;
                clone._afterPhase_k__BackingField = baseCraft._afterPhase_k__BackingField;

                clone._priority_k__BackingField = baseCraft.priority + i;

                clone._id_k__BackingField = baseCraft.id + 1000 + i;

                recipe.crafts.Add(clone);
            }
            Melon<Core>.Logger.Msg($"Increased amount of {recipe.recipeAsync.ToString()} to {recipe.crafts.Count}");
        }
        private static void SetPropertyToEmptyString(Il2CppArrabbiata.Common.Database.RecipeIngredient ingredient, string property) 
        {
            var emptyRef = new Il2CppArrabbiata.Common.Export.GuidAndFullName
            {
                guid = null,
                fullName = null
            };

            var prop = ingredient.GetType().GetProperty(property);
            if (prop == null) throw new Exception("Setting a property to empty string failed, because the property has not been found in func SetPropertyToEmptyString()");
            else prop.SetValue(ingredient, emptyRef);

            Melon<Core>.Logger.Msg($"Disabled {property} of  ingredient {ingredient.ingredient}");
        }
    }

    [HarmonyPatch(typeof(Il2CppArrabbiata.ItemDatabase), nameof(Il2CppArrabbiata.ItemDatabase.Load))]
    public static class PatchItemDatabase
    {
        private static void Prefix(Il2CppSystem.Collections.Generic.List<Il2CppArrabbiata.Common.Database.Item> items)
        {
            foreach (var item in items)
            {
                if (item.fullName == "DB_ITEM_ALMOND_BAKED") SetPropertyItem(item, "_useTemperature_k__BackingField", false);
                if (item.fullName == "DB_ITEM_POTATO-SALAD_VAR_LVL-02") SetPropertyItem(item, "_useTemperature_k__BackingField", false);
                if (item.fullName == "DB_ITEM_SMALL-POTATO_COOKED") SetPropertyItem(item, "_useTemperature_k__BackingField", false);
                if (item.id == 122) SetPropertyItem(item, "_useTemperature_k__BackingField", true); //tagliatelle
                if (item.id == 718) item.refAssetItemTags.Remove(item.refAssetItemTags[1]); // remove vegetarian tag from Arancini
                if (item.id == 197) SetPropertyItem(item, "_useTemperature_k__BackingField", true); // add temp to veal stock
                if (item.id == 1416) SetPropertyItem(item, "_maxStacks_k__BackingField", 5); // onion halves stackable
                    
            }

        }

        private static void SetFieldFalse(Il2CppArrabbiata.Common.Database.Item item, Expression<Func<object, bool>> fieldSelector) 
        {
            if (fieldSelector.Body is MemberExpression member) 
            {
                var field = member.Member as FieldInfo;
                if (field != null && field.FieldType == typeof(bool)) 
                {
                    field.SetValue(item, false);
                }
                Melon<Core>.Logger.Msg($"Sucessfully set {field.Name} of {item.fullName} to false");
            }

            
        }

        private static void SetPropertyItem(Il2CppArrabbiata.Common.Database.Item item, string property, object value) 
        {
            var prop = item.GetType().GetProperty(property);

            if (prop == null) throw new Exception($"Setting a property failed, because {prop} has not been found in func SetPropertyItem");
            if (prop.PropertyType != value.GetType()) {
                try {
                    value = Convert.ChangeType(value, prop.PropertyType);
                }
                catch (Exception ex) {
                    throw new Exception($"Setting a property failed, because {prop.PropertyType} and {value.GetType()} are not of the same type and cannot be converted in func SetPropertyItem", ex);
                }
            }
            prop.SetValue(item, value);
            Melon<Core>.Logger.Msg(txt: $"Successfully set '{property}' of '{item.fullName}' to '{value}'");
        }

        // [HarmonyPatch(typeof(Il2CppArrabbiata.RecipeData), nameof(RecipeData.FromCommonData))]
        public static class PatchCookDatabase
        {
            /*
            static void Prefix()
            {
                Melon<Core>.Logger.Msg(Environment.StackTrace);
            }
            */

            private static void Prefix(Il2CppArrabbiata.RecipeData __instance, Il2CppArrabbiata.Common.Database.Recipe recipe, Il2CppSystem.Collections.Generic.List<Il2CppArrabbiata.Common.Database.PhaseTemplate> phaseTemplates, Il2CppSystem.Collections.Generic.List<Il2CppArrabbiata.Common.NewDatabase.PreparationTableTemplate> preparationTableTemplates)
            {
                if (recipe.recipeAsync.ToString() == "PureeVegan_Cooked_RA") IncreasePotions(recipe, 4);

            }
            //DebugDump.DumpObject(recipe, __instance.fullName);
            /*
            for (int i = 0; i < phaseTemplates.Count; i++)
            {
                var phase = phaseTemplates[i];

                Melon<Core>.Logger.Msg($"Phase {i}: {phase}");
                DumpObject(phase);
            }
            for (int i = 0; i < preparationTableTemplates.Count; i++)
            {
                var preparation = preparationTableTemplates[i];

                Melon<Core>.Logger.Msg($"Preparation {i}: {preparation}");
                DumpObject(preparation);
            }
            */
            // example modification
            // phase.someField = value;

            private static void IncreasePotions(Il2CppArrabbiata.Common.Database.Recipe recipe, int targetAmount)
            {

                var baseCraft = recipe.crafts[0];

                if (targetAmount < 1) throw new Exception("Target amount is less than 1");

                for (int i = 1; i < targetAmount; i++)
                {
                    var clone = new Il2CppArrabbiata.Common.Database.RecipeCraft();

                    clone._recipe_k__BackingField = baseCraft._recipe_k__BackingField;
                    clone._craft_k__BackingField = baseCraft._craft_k__BackingField;
                    clone._afterPhase_k__BackingField = baseCraft._afterPhase_k__BackingField;

                    clone._priority_k__BackingField = baseCraft.priority + i;

                    clone._id_k__BackingField = baseCraft.id + 1000 + i;

                    recipe.crafts.Add(clone);
                }
                Melon<Core>.Logger.Msg($"Increased amount of {recipe.recipeAsync} to {recipe.crafts.Count}");
            }
        }

    }

    // [HarmonyPatch(typeof(Il2CppArrabbiata.Container), "UpdateUseTemperature")] // deprecated patch
    public static class PatchContainerTemp {

        
        private static void Prefix(ref Il2CppArrabbiata.Container __instance) {
            List<int> stopTempPropagation = [852, 1063]; // [Marinating Pea Soup, terragon-potato puree mix]
            if (stopTempPropagation.Contains(__instance.recipe?.id.id ?? -1)){ 
                foreach (var slot in __instance.inventoryRenderer?.m_slots)
                {
                    slot.item?.temperature.m_internalTemperature.m_useTemperature = false;
                }
                __instance.m_cachedTemperatureState = ETemperatureState.None;
                Melon<Core>.Logger.Msg($"Container {__instance.name} and recipe {__instance.recipe.recipeAsync.ToString()} has been set to no temp usage!");
                //return;
            }

            /*Traverse.IterateProperties(__instance, t =>
            {
                Melon<Core>.Logger.Msg(
                    $"{t.Property().} = {t}"
                );
            });*/
        }
    }

    // [HarmonyPatch(typeof(Il2CppArrabbiata.Workstation), "OnRecipeEvent")]
    public static class PatchBlenderTemp
    {
        static readonly HashSet<int> stopTempPropagation = [1080, 1316];  // [Marinating Pea Soup mixed, terragon-potato puree mix]
        private static void Postfix(ref Il2CppArrabbiata.Workstation __instance, ERecipeEvent recipeEvent)
        {
            if (__instance.fullname == "UI_BLENDER" && recipeEvent == ERecipeEvent.Cooked)
            {
                var lastItem = __instance?.inventoryRenderer?.m_inventories[1].m_backend.m_lastAdded;
                if (stopTempPropagation.Contains(lastItem?.itemId.id ?? -1))
                {
                    lastItem.m_temperature.m_useTemperature = false;
                    Melon<Core>.Logger.Msg($"Hi, disabled temperature for item {lastItem.itemData.fullName}");
                }
            }
        }
    }
    /*
    [HarmonyPatch(typeof(Il2CppArrabbiata.Item), "get_temperature")]
    public static class PatchItemTemp
    {
        static readonly HashSet<int> stopTempPropagation = [1080, 1316];  // [Marinating Pea Soup mixed, terragon-potato puree mix]
        static Il2CppArrabbiata.ReadOnlyTemperature Postfix(Il2CppArrabbiata.ReadOnlyTemperature __result, Il2CppArrabbiata.Item __instance)
        {
            if (!stopTempPropagation.Contains(__instance.id.id))
                return __result;

            var modified = __result; // copy struct

            modified.m_internalTemperature.m_useTemperature = false;

            return modified;
            Melon<Core>.Logger.Msg($"Hi, disabled temperature for item {__instance.name}");
        }
    }
    */
    
    [HarmonyPatch(typeof(Il2CppArrabbiata.Temperature.Backend), "InheritTemperatures")]
    public static class PatchTemperatureInheritance
    {
        static readonly HashSet<int> stopTempPropagation = 
            [1080, // pea mint soup mixed
            1053,  // pea watercress
            457,   // parmesan tuiles
            1316]; // tarragon puree balls

        static void Postfix(
            Il2CppArrabbiata.Temperature.Backend __instance,
            ref List<Il2CppArrabbiata.ReadOnlyTemperature> inheritedTemperatures)
        {
            int id = __instance?.m_item?.itemId?.id ?? -1;

            if (id == -1)
                return;

            if (stopTempPropagation.Contains(id))
            {
                __instance.m_useTemperature = false;

                Melon<Core>.Logger.Msg(
                    $"Disabled temp inheritance for {__instance.m_item.itemData.fullName}"
                );
            }
        }
    }

    public static class DebugDump
    {
        public static void DumpObject(object obj, string outputFilename)
        {
            if (obj == null)
            {
                Melon<Core>.Logger.Msg("NULL OBJECT");
                return;
            }

            // sanitize filename (important!)
            foreach (char c in Path.GetInvalidFileNameChars())
                outputFilename = outputFilename.Replace(c, '_');

            string dumpPath = Path.Combine(
                MelonEnvironment.UserDataDirectory,
                $"{outputFilename}.txt"
            );

            Type t = obj.GetType();

            File.AppendAllText(dumpPath, $"--- Dumping {t.FullName} ---\n");

            // properties
            foreach (var prop in t.GetProperties(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance))
            {
                try
                {
                    object value = prop.GetValue(obj);
                    File.AppendAllText(dumpPath, $"PROP {prop.Name} = {value}\n");
                }
                catch
                {
                    File.AppendAllText(dumpPath, $"PROP {prop.Name} = <unreadable>\n");
                }
            }

            // fields (IL2CPP pointers etc)
            foreach (var field in t.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance))
            {
                try
                {
                    object value = field.GetValue(obj);
                    File.AppendAllText(dumpPath, $"FIELD {field.Name} = {value}\n");
                }
                catch
                {
                    File.AppendAllText(dumpPath, $"FIELD {field.Name} = <unreadable>\n");
                }
            }

            File.AppendAllText(dumpPath, "\n");
        
        
        }
    }
    public static class DeepDump
    {
       

        public static void DumpObjectToFile(object obj, string outputFileName)
        {
            string dumpPath =
           Path.Combine(MelonEnvironment.UserDataDirectory, $"{outputFileName}.txt");
            using (var writer = new StreamWriter(dumpPath, true))
            {
                writer.WriteLine("=================================");
                writer.WriteLine($"Dump time: {DateTime.Now}");
                writer.WriteLine("=================================");

                DumpObject(obj, writer, "");
            }

            Melon<Core>.Logger.Msg($"Dump written to {dumpPath}");
        }

        static void DumpObject(object obj, StreamWriter writer, string indent)
        {
            if (obj == null)
            {
                writer.WriteLine(indent + "null");
                return;
            }

            var type = obj.GetType();

            writer.WriteLine($"{indent}{type.FullName}");

            // primitive
            if (type.IsPrimitive || obj is string || obj is decimal)
            {
                writer.WriteLine(indent + obj);
                return;
            }

            // Il2Cpp IEnumerable
            if (obj is Il2CppSystem.Collections.IEnumerable enumerable && !(obj is string))
            {
                int i = 0;

                foreach (var item in enumerable)
                {
                    writer.WriteLine($"{indent}[{i}]");

                    DumpObject(item, writer, indent + "  ");

                    i++;
                }

                return;
            }

            // properties
            foreach (var prop in type.GetProperties(
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.Instance))
            {
                try
                {
                    var value = prop.GetValue(obj);

                    writer.WriteLine($"{indent}{prop.Name}:");

                    DumpObject(value, writer, indent + "  ");
                }
                catch { }
            }

            // fields
            foreach (var field in type.GetFields(
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.Instance))
            {
                try
                {
                    var value = field.GetValue(obj);

                    writer.WriteLine($"{indent}{field.Name}:");

                    DumpObject(value, writer, indent + "  ");
                }
                catch { }
            }
        }
    }
}
