using WinterRose.DependancyInjection;

namespace WinterRose.Applications.ApplicationInstanceLocks;

public static class Extensions
{
    extension(IServiceBuilder services)
    {
        public IServiceBuilder AddApplicationMutex(string appId)
        {
            services.AddSingleton<MutexOptions>().Configure<MutexOptions>(x => x.AppId = appId);
            services.AddSingleton<IApplicationMutex, LinuxApplicationMutex>();
            services.AddSingleton<IApplicationMutex, WindowsApplicationMutex>();
            return services;
        }
    }
}