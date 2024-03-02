using System.Reflection;
using System.Runtime.InteropServices;

// KEEP THESE UP-TO-DATE

// Version that shows in the plugin list in Space Engineers as well as Windows Explorer unless overridden by an AssemblyFileVersion.
[assembly: AssemblyVersion("1.0.0.0")]
// Version of Space Engineers this plugin was written for. Shows as "Product" related info in Windows Explorer.
[assembly: AssemblyInformationalVersion("1.203.630 b0 default")]
// Yearly updates to the copyright notice.
[assembly: AssemblyCopyright("Copyright © Marco Leise 2024")]

// INFORMATION SPECIFIC TO THIS PLUGIN (KEEP IN SYNC WITH PLUGIN HUB XML)

[assembly: AssemblyTitle("Projected Lights")]
[assembly: AssemblyDescription("Turns point lights into projected lights.")]

// INFORMATION THAT IS THE SAME FOR ALL PLUGINS

[assembly: AssemblyProduct("Space Engineers")]
[assembly: ComVisible(false)]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
