using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
#if DEBUG
using Dalamud.Hooking;
#endif
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;

using Character = Dalamud.Game.ClientState.Objects.Types.Character;

namespace ResetEnmityCommand
{
  public class Plugin : IDalamudPlugin
  {
    /*
    /// <summary>
    /// Interface to the Dalamud plugin library.
    /// </summary>
    [PluginService][NotNull, AllowNull] public static DalamudPluginInterface DalamudPluginInterface { get; private set; }
    */

    /// <summary>
    /// The Dalamud service's manager to get what the player's target is.
    /// </summary>
    [PluginService][NotNull, AllowNull] public static ITargetManager TargetManager { get; private set; }

    /*
    /// <summary>
    /// The Dalamud service's data table to get what objects are in the local vicinity.
    /// </summary>
    [PluginService][NotNull, AllowNull] public static ObjectTable ObjectTable { get; private set; }
    */

    /// <summary>
    /// The Dalamud service's interface class to get the game's GUI interface.
    /// </summary>
    [PluginService][NotNull, AllowNull] public static IGameGui GameGui { get; private set; }

    /// <summary>
    /// The Dalamud service's command manager interface.
    /// Used to add the commands to the plugin interface.
    /// </summary>
    [PluginService][NotNull, AllowNull] public static ICommandManager CommandManager { get; private set; }

    /// <summary>
    /// The Dalamud service interface to scan the data in the running process to clear enmity.
    /// </summary>
    [PluginService][NotNull, AllowNull] public static IGameInteropProvider GameInteropProvider { get; private set; }

    /// <summary>
    /// The Dalamud service interface to scan the data in the running process to clear enmity.
    /// </summary>
    [PluginService][NotNull, AllowNull] public static IPluginLog Log { get; private set; }

    /// <summary>
    /// The Delegate function to execute the target subcommand to reset enmity of the striking dummy.
    /// </summary>
    /// <param name="id">ID of the object.</param>
    /// <param name="a1">Unknown</param>
    /// <param name="a2">Unknown</param>
    /// <param name="a3">Unknown</param>
    /// <param name="a4">Unknown</param>
    /// <returns>Unknown</returns>
    private delegate long ExecuteCommandDelegate(uint id, uint a1, int a2, int a3, int a4);

    /// <summary>
    /// The variable that implements the <see cref="ExecuteCommandDelegate"/> function.
    /// </summary>
    private readonly ExecuteCommandDelegate ExecuteCommand;

    /// <summary>
    /// The name of the plugin.
    /// </summary>
    public static string Name => "Reset Striking Dummy Enmity";

    /// <summary>
    /// Dispose the plugin's handler's on unload.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Safe disposing measures.
    /// </summary>
    private bool _IsDisposed;

    /// <summary>
    /// Dispose plugin safely.
    /// </summary>
    /// <param name="disposing"><see cref="Boolean"/> = True if your intent is to dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
      /// Checks if the plugin has been disposed of yet in an extreme circumstance.
      if (!_IsDisposed)
      {
#if DEBUG
        ExecuteCommandHook?.Disable();
#endif
        /// Dispose of the targeted command.
        _ = CommandManager.RemoveHandler("/resetenmity");
        /// Dispose of the global command.
        _ = CommandManager.RemoveHandler("/resetenmityall");
        /// Marks the plugin as disposed.
        _IsDisposed = true;
      }
    }

#if DEBUG
    private static Hook<ExecuteCommandDelegate>? ExecuteCommandHook { get; set; }
    private static long ExecuteCommandDetour(uint trigger, uint a1, int a2, int a3, int a4)
    {
      Log.Debug($"trigger: {trigger}, a1: {a1:X}, a2: {a2:X}, a3: {a3:X}, a4: {a4:X}");
      return ExecuteCommandHook!.Original(trigger, a1, a2, a3, a4);
    }
#endif

    /// <summary>
    /// The plugin constructor class.
    /// </summary>
    public unsafe Plugin([RequiredVersion("1.0")] ISigScanner sigScanner)
    {
      #region Sig Documentation
      /// As of 6.28 and 6.3 the file offset is: ffxiv_dx11.exe+742D70
      //FUN_14073a2f0
      /// void UndefinedFunction_140b105e0 (undefined8 param_1,ushort param_2,undefined param_3,undefined param_4,ushort param_5 )
      /// {
      ///   longlong lVar1;
      ///   undefined auStack_f88 [32];
      ///   undefined4 auStack_f68 [2];
      ///   undefined8 uStack_f60;
      ///   undefined4 uStack_f48;
      ///   uint uStack_f44;
      ///   uint uStack_f40;
      ///   uint uStack_f3c;
      ///   undefined4 uStack_f38;
      ///   undefined8 uStack_f30;
      ///   ulonglong uStack_18;
      ///
      ///   uStack_18 = _DAT_14207e8f0 ^ (ulonglong)auStack_f88;
      ///   lVar1 = func_0x000140093540(_g_Client::System::Framework::Framework_InstancePointer2);
      ///   if (lVar1 != 0) {
      ///     uStack_f38 = 0;
      ///     auStack_f68[0] = 0xfc;
      ///     uStack_f60 = 0x30;
      ///     uStack_f48 = 0x466;
      ///     uStack_f30 = 0;
      ///     uStack_f44 = (uint)param_2;
      ///     uStack_f40 = (uint)CONCAT11(param_3,param_4);
      ///     uStack_f3c = (uint)param_5;
      ///     func_0x0001402097f0(lVar1,auStack_f68,0,0);
      ///   }
      ///   FUN_1415aff90(uStack_18 ^ (ulonglong)auStack_f88);
      ///   return;
      /// }
      #endregion
      /// The signature for the reset enmity at target.
      /// Ask the Dalamud Discord or use a Decompiler like IDA if it is out of date.
      nint scanText = sigScanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A");
      /// Adds the memory delegate to execute the function at the pointer.
      ExecuteCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(scanText);
      /// Logs the execution of the reset striking dummy enmity command to the debug log.
      Log.Debug($"{nameof(ExecuteCommand)} +{scanText - Process.GetCurrentProcess().MainModule!.BaseAddress:X}");
      /// Add the reset enmity command for the targeted striking dummy.
      _ = CommandManager.AddHandler("/resetenmity", new CommandInfo(ResetTarget) { HelpMessage = "Reset target dummy's enmity." });
      /// Add the reset enmity command for all striking dummies.
      _ = CommandManager.AddHandler("/resetenmityall", new CommandInfo(ResetAll) { HelpMessage = "Reset the enmity of all dummies." });
#if DEBUG
      ExecuteCommandHook = GameInteropProvider.HookFromAddress<ExecuteCommandDelegate>(sigScanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A"), ExecuteCommandDetour);
      ExecuteCommandHook.Enable();
#endif
    }

    /// <summary>
    /// The main reset enmity for object function.
    /// Resets the enmity of the provided object ID and logs it to information.
    /// </summary>
    /// <param name="objectId">The object to reset the enmity of.</param>
    private void ResetEnmity(uint objectId)
    {
      Log.Information($"Resetting enmity {objectId:X}");
      long success = ExecuteCommand(0x13f, objectId, 0, 0, 0);
      Log.Debug($"Reset enmity of {objectId:X} returned: {success}");
    }

    /// <summary>
    /// The reset enmity of the targeted striking dummy.
    /// </summary>
    /// <param name="s">The command executed. (Unused)</param>
    /// <param name="s1">The parameters. (Unused)</param>
    private void ResetTarget(string s, string s1)
    {
      /// Gets the current target object.
      var target = TargetManager.Target;
      /// Checks if the current target is a character with the name ID of:
      /// <seealso href="https://xivapi.com/BNpcName/541"/>
      if (target is Character { NameId: 541 })
      {
        /// Reset the enmity of the target via it's objectId.
        ResetEnmity(target.ObjectId);
      }
    }

    /// <summary>
    /// The reset enmity of all aggroed striking dummies.
    /// </summary>
    /// <param name="s">The command executed. (Unused)</param>
    /// <param name="s1">The parameters. (Unused)</param>
    private unsafe void ResetAll(string s, string s1)
    {
      /// Get game GUI add-on, EnemyList by name
      var addonByName = GameGui.GetAddonByName("_EnemyList", 1);
      /// If the EnemyList add-on was found and isn't <see cref="IntPtr.Zero"/> then continue.
      if (addonByName != IntPtr.Zero)
      {
        /// Parse the addonByName to the <see cref="AddonEnemyList"/> class/pointer.
        var addon = (AddonEnemyList*)addonByName;
        /// Get the array of enemies to an <see cref="NumberArrayData"/> pointer.
        var numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.NumberArrays[21];
#if DEBUG
        numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.NumberArrays[int.Parse(s1.Split()[0])];
#endif

        /// Loop through the array of enemies by the count of enemies from the <see cref="AddonEnemyList"/>.
        for (var i = 0; i < addon->EnemyCount; i++)
        {
          /// Get the enemy object ID from the <see cref="NumArrayData"/> as an <see cref="int"/> array with index at <see cref="i"/> multiplied by 6, then offset by plus 8.
          var enemyObjectId = (uint)numArray->IntArray[8 + (i * 6)];

          /// Get the <see cref="BattleChara"/> from the <see cref="enemyObjectId"/> in the instance.
          var enemyChara = CharacterManager.Instance()->LookupBattleCharaByObjectId(enemyObjectId);
          /// If the <see cref="enemyChara"/> is not found or is null, then ignore and continue through the for loop.
          if (enemyChara is null)
          {
            continue;
          }
          /// If the <see cref="enemyChara"/> is found and is not null, check if it has the name ID of:
          /// <seealso href="https://xivapi.com/BNpcName/541"/>
          /// and then reset the enmity of that object.
          if (enemyChara->Character.NameID == 541)
          {
            ResetEnmity(enemyObjectId);
          }
        }
      }
    }
  }
}
