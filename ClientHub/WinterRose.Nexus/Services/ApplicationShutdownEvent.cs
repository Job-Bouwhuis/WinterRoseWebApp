using System.Diagnostics;

namespace WinterRose.Nexus.Services;

public record ApplicationShutdownEvent(string AppId, Process Process);