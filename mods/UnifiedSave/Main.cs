using Godot;
using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves; 

namespace UnifiedSave;

[ModInitializer(nameof(Init))]
public static class MainFile
{
    public const string ModId = "Sothis.UnifiedSave";
    public static readonly MegaCrit.Sts2.Core.Logging.Logger Logger = new(ModId, LogType.Generic);

    public static void Init()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        // Force the backing field to false immediately at initialization
        try 
        {
            UserDataPathProvider.IsRunningModded = false;
            Logger.Info("Unified Save Path initialized. Forced global Modded flag to FALSE.");
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to set static flag directly: {e.Message}");
        }
    }
}

/// <summary>
/// Intercepts the property getter for IsRunningModded and forces it to return false.
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), "get_IsRunningModded")]
public static class PatchGetIsRunningModded
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false; // Skip original property getter execution
    }
}

/// <summary>
/// Intercepts the property setter for IsRunningModded and kills any attempts to flip it to true.
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), "set_IsRunningModded")]
public static class PatchSetIsRunningModded
{
    public static bool Prefix(ref bool value)
    {
        value = false; 
        return true; // Execute original setter using our sanitized 'false' value
    }
}

/// <summary>
/// Complete fallback patch. If any core engine method inline-caches the profile name,
/// this intercepts it and guarantees a clean, unmodded folder path is returned.
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), "GetProfileDir")]
public static class PatchGetProfileDir
{
    public static bool Prefix(int profileId, ref string __result)
    {
        __result = $"profile{profileId}";
        
        return false; // Skip original method logic entirely
    }
}