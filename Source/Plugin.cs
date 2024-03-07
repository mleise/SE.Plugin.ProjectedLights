using avaness.PluginLoader;
using HarmonyLib;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game.Components;
using VRage.Plugins;
using VRage.Utils;

namespace mleise.ProjectedLightsPlugin
{
	sealed class Main : IPlugin
	{
		/// <summary>Called when the plugins are instantiated.</summary>
		public void Init(object gameInstance)
		{
			// Ensure that we aren't loading twice.
			var allPlugins = new List<KeyValuePair<IPlugin, bool>>();

			foreach (var plugin in MyPlugins.Plugins)
			{
				if (plugin is avaness.PluginLoader.Main)
				{
					var pluginField = AccessTools.Field(typeof(PluginInstance), "plugin");
					foreach (var pluginInstance in (List<PluginInstance>)AccessTools.Field(typeof(avaness.PluginLoader.Main), "plugins").GetValue(plugin))
					{
						allPlugins.Add(new KeyValuePair<IPlugin, bool>((IPlugin)pluginField.GetValue(pluginInstance), false));
					}
				}
				else
				{
					allPlugins.Add(new KeyValuePair<IPlugin, bool>(plugin, true));
				}
			}

			foreach (var plugin in allPlugins)
			{
				if (ReferenceEquals(plugin.Key, this))
				{
					break;
				}
				else if (plugin.Key.GetType().Namespace == typeof(Main).Namespace)
				{
					var assembly = plugin.Key.GetType().Assembly;
					var location = assembly.Location;
					var msg = "A version of \"" + assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title + "\" has already been instantiated via the ";
					msg += (plugin.Value ? "command-line" : "Plugin Loader") + ":" + (location == "" ? " Compiled from development folder." : "\n" + location);
					MyMessageBox.Show("Plugin patching aborted", msg);
					throw new Exception(msg);
				}
			}

			// Patch Space Engineers.
			Harmony harmony = new Harmony(typeof(Main).Namespace);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			// Space Engineers compiles a couple of shaders before plugins have a chance to intercept them.
			// By triggering a recompile, we can go back and patch them. They will eventually all be cached in
			// "%APPDATA%\SpaceEngineers\ShaderCache2".
			AccessTools.Method("VRageRender.MyPixelShaders:Recompile").Invoke(null, null);
		}

		/// <summary>Called when the game is closed.</summary>
		public void Dispose() {}

		/// <summary>Called at the end of each game update at 60 Hz, with only audio trailing it.</summary>
		public void Update() {}
	}

	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	sealed class AdditionalLightOptions : MySessionComponentBase
	{
		public override void LoadData()
		{
			MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls.AddTerminalControls;
		}

		protected override void UnloadData()
		{
			MyAPIGateway.TerminalControls.CustomControlGetter -= TerminalControls.AddTerminalControls;
		}
	}
}
