// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "RCS1170:Use read-only auto-implemented property.", Justification = "This field doesn't need to be static", Scope = "member", Target = "~P:ResetEnmityCommand.Plugin.TargetManager")]
[assembly: SuppressMessage("Style", "RCS1170:Use read-only auto-implemented property.", Justification = "This field doesn't need to be static", Scope = "member", Target = "~P:ResetEnmityCommand.Plugin.GameGui")]
[assembly: SuppressMessage("Style", "RCS1170:Use read-only auto-implemented property.", Justification = "This field doesn't need to be static", Scope = "member", Target = "~P:ResetEnmityCommand.Plugin.CommandManager")]
[assembly: SuppressMessage("Style", "RCS1170:Use read-only auto-implemented property.", Justification = "This field doesn't need to be static", Scope = "member", Target = "~P:ResetEnmityCommand.Plugin.SigScanner")]
#if DEBUG
[assembly: SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed", Justification = "<Pending>", Scope = "member", Target = "~M:ResetEnmityCommand.Plugin.ResetAll(System.String,System.String)")]
#endif
[assembly: SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "<Pending>", Scope = "member", Target = "~M:ResetEnmityCommand.Plugin.#ctor(Dalamud.Plugin.DalamudPluginInterface,Dalamud.Game.SigScanner)")]
