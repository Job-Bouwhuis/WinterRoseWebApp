namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public interface IAsyncQueue<T>
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