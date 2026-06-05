using Godot;
using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace QuickRestart;

[ModInitializer(nameof(Init))]
public static class MainFile
{
    public const string ModId = "sothis.QuickRestart";
    public static readonly MegaCrit.Sts2.Core.Logging.Logger Logger = new(ModId, LogType.Generic);
    
    public static void Init()
    {
        Harmony harmony = new(ModId);
        try 
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.Info("Quick Restart Mod Armed! Press 'R' to reload the room.");
        }
        catch (System.Exception e)
        {
            Logger.Error($"Harmony Patch Failed down the line: {e}");
        }
    }
}


[HarmonyPatch]
public static class QuickRestartHandler
{
    // ==========================================
    // ⌨️ KEY EVENT INTERCEPTOR (R KEY)
    // ==========================================

    /// <summary>
    /// Patches the native C# NInputManager script.
    /// Intercepts physical hardware keystrokes before standard shortcut/debug processing.
    /// </summary>
    [HarmonyPatch(typeof(NInputManager), nameof(NInputManager._UnhandledKeyInput))]
    public static class InputManagerHotkeyPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(InputEvent inputEvent)
        {
            // Filter for physical press of 'R' and ignore echo repeats
            if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                if (keyEvent.Keycode == Key.R)
                {
                    // Ensure core run singletons are initialized and set to Singleplayer mode
                    if (RunManager.Instance != null && 
                        RunManager.Instance.NetService.Type == NetGameType.Singleplayer && 
                        NGame.Instance != null)
                    {
                        MainFile.Logger.Info("Hotkey 'R' intercepted via NInputManager. Executing room reset...");
                        RestartRoom();
                        return false; // Skip original method execution (swallow the keypress)
                    }
                }
            }
            return true; // Let all other keys flow through to shortcuts/debug processing
        }
    }

    // ==========================================
    // 🔄 STATE ENGINE ROLLBACK EXECUTOR
    // ==========================================

    public static void RestartRoom()
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            MainFile.Logger.Error("Restart aborted: Multiplayer session detected.");
            return;
        }

        if (!SaveManager.Instance.HasRunSave)
        {
            MainFile.Logger.Error("Restart aborted: Active save state database not found on disk.");
            return;
        }

        // 1. Flush volatile active room components and audio channels
        RunManager.Instance.ActionQueueSet.Reset();
        NRunMusicController.Instance.StopMusic();
        RunManager.Instance.CleanUp();

        MainFile.Logger.Info("Flushed active room assets. Loading local auto-save data...");

        // 2. Load and deserialize the active run database snapshot
        ReadSaveResult<SerializableRun> runSave = SaveManager.Instance.LoadRunSave();
        SerializableRun serializableRun = runSave.SaveData;
        RunState runState = RunState.FromSerializable(serializableRun);

        MainFile.Logger.Info("State data deserialized successfully.");

        // 3. Re-initialize underlying run structures using Mega Crit's async Task wrapper
        TaskHelper.RunSafely(RunManager.Instance.SetUpSavedSingleplayer(runState, serializableRun));
        MainFile.Logger.Info($"Continuing run with character: {serializableRun.Players[0].CharacterId}");
        
        // 4. Fire the transition SFX trigger
        if (runState.Players.Count > 0 && runState.Players[0].Character != null)
        {
            SfxCmd.Play(runState.Players[0].Character.CharacterTransitionSfx);
        }
        
        // 5. Force-bind the singleplayer network container interface wrapper
        NGame.Instance.ReactionContainer.InitializeNetworking(new NetSingleplayerGameService());
        
        // 6. Push the scene load instruction safely directly into MegaDot's scheduler pipeline
        TaskHelper.RunSafely(NGame.Instance.LoadRun(runState, serializableRun.PreFinishedRoom));
        
        MainFile.Logger.Info("Room reload completely achieved.");
    }
}