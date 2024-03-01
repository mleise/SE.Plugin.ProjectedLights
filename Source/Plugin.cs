using HarmonyLib;
using Sandbox.ModAPI;
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
		MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls.AddTerminalControls;
	}

	protected override void UnloadData()
	{
		MyAPIGateway.TerminalControls.CustomControlGetter -= TerminalControls.AddTerminalControls;
	}
}
