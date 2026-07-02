using System.Runtime.InteropServices.JavaScript;

namespace WinterRose.Nexus.SDK;

internal class NexusNotInstalledException(Exception inner) : Exception("Launching the Nexus client failed. Do you have Nexus installed?", inner);