using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinterRose.Web.Utils;

/// <summary>
/// Defines an interface for an event bus, which allows for publishing events and subscribing to them.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event with the specified name and payload. Subscribers to this event will receive the payload when the event is published.
    /// </summary>
    Task PublishAsync(string eventName, object payload);

    /// <summary>
    /// Subscribes to an event with the specified name. 
    /// </summary>
    void Subscribe(string eventName, Func<object, Task> handler);

    /// <summary>
    /// Publishes an event with the specified name and strongly-typed payload.
    /// </summary>
    Task PublishAsync<T>(string eventName, T payload);

    /// <summary>
    /// Subscribes to an event with the specified name and strongly-typed payload.
    /// </summary>
    void Subscribe<T>(string eventName, Func<T, Task> handler);

    /// <summary>
    /// Sends a request with the specified event name and payload, and waits for a response of type TResponse.
    /// </summary>
    Task<TResponse?> RequestAsync<TResponse>(string eventName, object payload);

    /// <summary>
    /// Registers a handler for requests with the specified event name. The handler will receive the
    /// request payload and an EventResponse object that can be used to send a response back to the requester.
    /// </summary>
    void Respond<TRequest, TResponse>(string eventName, Func<TRequest, EventResponse<TResponse>, Task> handler);
}

/// <summary>
/// Provides an implementation of <see cref="IEventBus"/>
/// </summary>
public class EventBus : IEventBus
{
    private readonly Dictionary<string, List<Func<object, Task>>> handlers = new();

    private readonly Dictionary<string, Func<object, Task<object?>>> responders = new();

    /// <summary>
    /// Subscribes to an event with the specified name. When the event is published, the provided handler will be invoked with the event payload.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="handler"></param>
    public void Subscribe(string eventName, Func<object, Task> handler)
    {
        if (!handlers.TryGetValue(eventName, out var list))
        {
            list = new List<Func<object, Task>>();
            handlers[eventName] = list;
        }

        list.Add(handler);
    }

    /// <summary>
    /// Publishes an event with the specified name and payload. All handlers subscribed to this event will be invoked with the payload.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    public async Task PublishAsync(string eventName, object payload)
    {
        if (!handlers.TryGetValue(eventName, out var list))
            return;

        foreach (var handler in list)
            await handler(payload);
    }

    public void Subscribe<T>(string eventName, Func<T, Task> handler)
    {
        Subscribe(eventName, payload => handler((T)payload));
    }

    public Task PublishAsync<T>(string eventName, T payload)
    {
        return PublishAsync(eventName, (object)payload!);
    }

    public async Task<TResponse?> RequestAsync<TResponse>(string eventName, object payload)
    {
        if (!responders.TryGetValue(eventName, out var responder))
            throw new InvalidOperationException($"No responder registered for event '{eventName}'.");

        var result = await responder(payload);
        return (TResponse?)result;
    }

    public void Respond<TRequest, TResponse>(
        string eventName,
        Func<TRequest, EventResponse<TResponse>, Task> handler)
    {
        responders[eventName] = async payload =>
        {
            var response = new EventResponse<TResponse>();
            await handler((TRequest)payload, response);
            return (object?)await response.Task;
        };
    }
}

/// <summary>
/// Represents a response to an event request, allowing the handler to set a result or an
/// exception that will be returned to the requester.
/// </summary>
public class EventResponse<T>
{
    internal TaskCompletionSource<T> Source { get; } = new();

    public void SetResult(T result)
        => Source.TrySetResult(result);

    public void SetException(Exception ex)
        => Source.TrySetException(ex);

    public Task<T> Task => Source.Task;
}