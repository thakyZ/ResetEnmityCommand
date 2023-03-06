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
#if DEBUG
using Dalamud.Hooking;
#endif
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
		//[PluginService] public static DalamudPluginInterface DalamudPluginInterface { get; private set; }
		[PluginService] public static TargetManager TargetManager { get; private set; }
		//[PluginService] public static ObjectTable ObjectTable { get; private set; }
		[PluginService] public static GameGui GameGui { get; private set; }
		[PluginService] public static CommandManager CommandManager { get; private set; }
		[PluginService] public static SigScanner SigScanner { get; private set; }

		private delegate long ExecuteCommandDele(uint id, int a1, int a2, int a3, int a4);
		private ExecuteCommandDele ExecuteCommand;

		public void Dispose()
		{
			CommandManager.RemoveHandler("/resetenmity");
			CommandManager.RemoveHandler("/resetenmityall");
#if DEBUG
			ExecuteCommandHook.Disable();
#endif
		}


#if DEBUG
        private static Hook<ExecuteCommandDele> ExecuteCommandHook;
        private static long ExecuteCommandDetour(uint trigger, int a1, int a2, int a3, int a4)
        {
			PluginLog.Debug($"trigger: {trigger}, a1: {a1:X}, a2: {a2:X}, a3: {a3:X}, a4: {a4:X}");
            return ExecuteCommandHook.Original(trigger, a1, a2, a3, a4);
        }
#endif

        public unsafe Plugin()
		{
			var scanText = SigScanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A");

			ExecuteCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDele>(scanText);
			PluginLog.Debug($"{nameof(ExecuteCommand)} +{(long)scanText - (long)Process.GetCurrentProcess().MainModule.BaseAddress:X} ");
            CommandManager.AddHandler("/resetenmity", new CommandInfo(ResetTarget) { HelpMessage = "Reset target dummy's enmity." });
			CommandManager.AddHandler("/resetenmityall", new CommandInfo(ResetAll) { HelpMessage = "Reset the enmity of all dummies." });
#if DEBUG
            ExecuteCommandHook = Hook<ExecuteCommandDele>.FromAddress(SigScanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A"), ExecuteCommandDetour);
            ExecuteCommandHook.Enable();
#endif
            
            void ResetEnmity(int objectId)
			{
				PluginLog.Information($"resetting enmity {objectId:X}");
                ExecuteCommand(0x13F, objectId, 0, 0, 0);
            }

            void ResetTarget(string s, string s1)
			{
                var target = TargetManager.Target;
                if (target is Character { NameId: 541 }) ResetEnmity((int)target.ObjectId);
			}

			void ResetAll(string s, string s1)
			{
				var addonByName = GameGui.GetAddonByName("_EnemyList", 1);
				if (addonByName != IntPtr.Zero)
				{
					var addon = (AddonEnemyList*)addonByName;
					var numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.NumberArrays[21];
#if DEBUG
                    numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureAtkModule()
						->AtkModule.AtkArrayDataHolder.NumberArrays[int.Parse(s1.Split()[0])];
#endif

                    for (var i = 0; i < addon->EnemyCount; i++)
					{
						var enemyObjectId = numArray->IntArray[8 + i * 6];
						var enemyChara = CharacterManager.Instance()->LookupBattleCharaByObjectId(enemyObjectId);
						if (enemyChara is null) continue;
						if (enemyChara->Character.NameID == 541) ResetEnmity(enemyObjectId);
					}
				}
			}
		}
		public string Name => "Reset enmity command";
	}
}
