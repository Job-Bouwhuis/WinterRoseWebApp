using System;
using System.Threading;

namespace WinterRose.Nexus.Services;

public static class ApplicationInstallerContext
{
    private static readonly AsyncLocal<string?> OVERRIDDEN_APP_ROOT = new();

    public static string? CurrentAppRoot
    {
        get => OVERRIDDEN_APP_ROOT.Value;
        set => OVERRIDDEN_APP_ROOT.Value = value;
    }

    public static IDisposable PushAppRoot(string appRoot)
    {
        string? previous = OVERRIDDEN_APP_ROOT.Value;
        OVERRIDDEN_APP_ROOT.Value = appRoot;

        return new PopOnDispose(previous);
    }

    private sealed class PopOnDispose : IDisposable
    {
        private readonly string? previous;

        public PopOnDispose(string? previous)
        {
            this.previous = previous;
        }

        public void Dispose()
        {
            OVERRIDDEN_APP_ROOT.Value = previous;
        }
    }
}