namespace WinterRose.Nexus.Shared;

public interface IAsyncEventQueue<T>
{
    IDisposable Subscribe(
        Func<T, Task> handler,
        CancellationToken ct);

    IDisposable Subscribe(
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct);

    Task SubscribeAsync(
        Func<T, Task> handler,
        CancellationToken ct);

    Task SubscribeAsync(
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct);

    void Publish(T evt);
}