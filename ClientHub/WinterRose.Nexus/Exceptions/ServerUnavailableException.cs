using System;

namespace WinterRose.Nexus.Exceptions;

public class ServerUnavailableException : Exception
{
    public ServerUnavailableException(Exception inner)
        : base("The server is currently unavailable.", inner)
    {
    }
}