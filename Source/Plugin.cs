using HarmonyLib;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game.Components;
using VRage.Plugins;

public class Main : IPlugin
{
	public void Startup() { }

	public void Dispose() { }

	public void Init(object gameInstance)
	{
		Harmony harmony = new Harmony("ProjectedLights");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}

	public void Update() { }
}

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
sealed public class AdditionalLightOptions : MySessionComponentBase
{
	public override void LoadData()
	{
		MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls.AddCustomControls;
	}

	protected override void UnloadData()
	{
		MyAPIGateway.TerminalControls.CustomControlGetter -= TerminalControls.AddCustomControls;
	}
}
