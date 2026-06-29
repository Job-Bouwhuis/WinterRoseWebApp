using System;

namespace WinterRose.ClientHub.Exceptions;

public class ServerUnavailableException : Exception
{
    public ServerUnavailableException(Exception inner)
        : base("The server is currently unavailable.", inner)
    {
    }
}