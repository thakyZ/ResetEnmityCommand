using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;

using BattleChara = Dalamud.Game.ClientState.Objects.Types.BattleChara;
using Character = Dalamud.Game.ClientState.Objects.Types.Character;

namespace ResetEnmityCommand
{
  public class Plugin : IDalamudPlugin
  {
    /*
    /// <summary>
    /// Interface to the Dalamud plugin library.
    /// </summary>
    [PluginService] public static DalamudPluginInterface DalamudPluginInterface { get; private set; }
    */
    
    /// <summary>
    /// The Dalamud service's manager to get what the player's target is.
    /// </summary>
    [PluginService] public static TargetManager TargetManager { get; private set; }
    
    /*
    /// <summary>
    /// The Dalamud service's data table to get what objects are in the local vicinity.
    /// </summary>
    [PluginService] public static ObjectTable ObjectTable { get; private set; }
    */

    /// <summary>
    /// The Dalamud service's interface class to get the game's GUI interface.
    /// </summary>
    [PluginService] public static GameGui GameGui { get; private set; }

    /// <summary>
    /// The Dalamud service's command manager interface.
    /// Used to add the commands to the plugin interface.
    /// </summary>
    [PluginService] public static CommandManager CommandManager { get; private set; }

    /// <summary>
    /// The Dalamud service interface to scan the data in the running process to clear enmity.
    /// </summary>
    [PluginService] public static SigScanner SigScanner { get; private set; }

    /// <summary>
    /// The Delegate function to execute the target subcommand to reset enmity of the striking dummy.
    /// </summary>
    /// <param name="id">ID of the object.</param>
    /// <param name="a1">Unknown</param>
    /// <param name="a2">Unknown</param>
    /// <param name="a3">Unknown</param>
    /// <param name="a4">Unknown</param>
    /// <returns>Unknown</returns>
    private delegate long ExecuteCommandDele(int id, int a1, int a2, int a3, int a4);

    /// <summary>
    /// The variable that implements the <see cref="ExecuteCommandDele"/> function.
    /// </summary>
    private ExecuteCommandDele ExecuteCommand;

    /// <summary>
    /// The name of the plugin.
    /// </summary>
    public string Name => "Reset Striking Dummy Enmity";

    /// <summary>
    /// Safe disposing measures.
    /// </summary>
    private bool _IsDisposed = false;

    /// <summary>
    /// Dispose plugin safely.
    /// </summary>
    /// <param name="disposing"><see cref="Boolean"/> = True if your intent is to dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
      /// Checks if the plugin has been disposed of yet in an extreme circumstance.
      if (!_IsDisposed)
      {
        /// Dispose of the targeted command.
        _ = CommandManager.RemoveHandler("/resetenmity");
        /// Dispose of the global command.
        _ = CommandManager.RemoveHandler("/resetenmityall");
        /// Marks the plugin as disposed.
        _IsDisposed = true;
      }
    }

    /// <summary>
    /// Dispose the plugin's handler's on unload.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The plugin constructor class.
    /// </summary>
    public unsafe Plugin()
    {
      /// The signature for the reset enmity at target.
      /// Ask the Dalamud Discord  or use a Decompiler like IDA if it is out of date.
      var scanText = SigScanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A");
      /// Adds the memory delegate to execute the function at the pointer.
      ExecuteCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDele>(scanText);
      /// Logs the execution of the reset striking dummy enmity command to the debug log.
      PluginLog.Debug($"{nameof(ExecuteCommand)} +{(long)scanText - (long)Process.GetCurrentProcess().MainModule.BaseAddress:X}");

      /// Add the reset enmity command for the targeted striking dummy.
      _ = CommandManager.AddHandler("/resetenmity", new CommandInfo(ResetTarget) { HelpMessage = "Reset target dummy's enmity." });
      /// Add the reset enmity command for all striking dummies.
      _ =CommandManager.AddHandler("/resetenmityall", new CommandInfo(ResetAll) { HelpMessage = "Reset the enmity of all dummies." });

      /// <summary>
      /// The main reset enmity for object function.
      /// Resets the enmity of the provided object ID and logs it to information.
      /// </summary>
      /// <param name="objectId">The object to reset the enmity of.</param>
      void ResetEnmity(int objectId)
      {
        PluginLog.Information($"Resetting enmity {objectId:X}");
        _ = ExecuteCommand(0x140, objectId, 0, 0, 0);
      }

      /// <summary>
      /// The reset enmity of the targeted striking dummy.
      /// </summary>
      /// <param name="s">The command executed. (Unused)</param>
      /// <param name="s1">The parameters. (Unused)</param>
      void ResetTarget(string s, string s1)
      {
        /// Gets the current target object.
        var target = TargetManager.Target;
        /// Checks if the current target is a character with the name ID of:
        /// <seealso href="https://xivapi.com/BNpcName/541"/>
        if (target is Character { NameId: 541 })
        {
          /// Reset the enmity of the target via it's objectId.
          ResetEnmity((int)target.ObjectId);
        }
      }
      
      /// <summary>
      /// The reset enmity of all aggroed striking dummies.
      /// </summary>
      /// <param name="s">The command executed. (Unused)</param>
      /// <param name="s1">The parameters. (Unused)</param>
      void ResetAll(string s, string s1)
      {
        /// Get game GUI add-on, EnemyList by name
        var addonByName = GameGui.GetAddonByName("_EnemyList", 1);
        /// If the EnemyList add-on was found and isn't <see cref="IntPtr.Zero"/> then continue.
        if (addonByName != IntPtr.Zero)
        {
          /// Parse the addonByName to the <see cref="AddonEnemyList"/> class/pointer.
          var addon = (AddonEnemyList*)addonByName;
          /// Get the array of enemies to an <see cref="NumberArrayData"/> pointer.
          var numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.NumberArrays[19];

          /// Loop through the array of enemies by the count of enemies from the <see cref="AddonEnemyList"/>.
          for (var i = 0; i < addon->EnemyCount; i++)
          {
            /// Get the enemy object ID from the <see cref="NumArrayData"/> as an <see cref="int"/> array with index at <see cref="i"/> multiplied by 6, then offset by plus 8.
            var enemyObjectId = numArray->IntArray[8 + (i * 6)];
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
}
