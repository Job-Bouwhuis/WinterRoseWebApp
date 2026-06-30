using System;
using System.Collections.Concurrent;

namespace WinterRose.Nexus.Interface;

public class MainThread
{
    private ConcurrentQueue<Action> actionQueue = [];

    public void Invoke(Action action) => actionQueue.Enqueue(action);
    
    public void ProcessActions(int maxProcessed = 100)
    {
        int processed = 0;

        while (processed < maxProcessed)
        {
            Action action;
            if (actionQueue.TryDequeue(out action))
            {
                action();
                processed++;
            }
            else
                break;
        }
    }
}