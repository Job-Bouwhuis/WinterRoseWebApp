using System;

namespace WinterRose.Applications.ApplicationInstanceLocks;

public interface IApplicationMutex : IDisposable
{
    bool IsFirstInstance { get; }
}