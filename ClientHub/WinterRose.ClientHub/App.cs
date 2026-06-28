using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinterRose.Applications;
using WinterRose.ClientHub.Feature.Interface;
using WinterRose.ClientHub.Feature.Interface.Windows;

namespace WinterRose.ClientHub;

internal class App : Application
{
    public static ApplicationBuilder CreateBuilder()
    {
        ApplicationBuilder builder = new ApplicationBuilder();
        builder.UseApplication<App>();
        return builder;
    }

    private int delay = 1000;
    
    private readonly UiManager uiManager;
    private Task ipcServerTask;

    public App(UiManager uiManager)
    {
        this.uiManager = uiManager;
    }
    
    /// <summary>
    /// Method should only be used when the app is started using a URI invocation, and this instance is not the MUTEX singleton process
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    public bool TrySendIntentToRunningInstance(string uri)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.PostAsync(
                "http://127.0.0.1:41472/intent",
                new StringContent(uri)
            ).Wait();

            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public Task StartIpcServer(CancellationToken token)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:41472/");
        listener.Start();

        return Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext ctx = listener.GetContext();
                string uri = new StreamReader(ctx.Request.InputStream).ReadToEnd();

                //HandleIntent(uri);

                byte[] response = Encoding.UTF8.GetBytes("ok");
                ctx.Response.OutputStream.Write(response);
                ctx.Response.Close();
            }
        });
    }

    public void Start()
    {

    }

    public void Stop()
    {
    }

    protected override async Task Execute(CancellationToken token)
    {
        ipcServerTask = StartIpcServer(token);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, token);

            }
            catch (TaskCanceledException)
            {
                // this is okey, the task was cancelled
                // we break immeidately
                break;
            }
        }
    }
}