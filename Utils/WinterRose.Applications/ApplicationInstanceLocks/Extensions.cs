using WinterRose.DependancyInjection;

namespace WinterRose.Applications.ApplicationInstanceLocks;

public static class Extensions
{
    extension(ServiceBuilder services)
    {
        public ServiceBuilder AddApplicationMutex()
        {
            services.AddSingleton<IApplicationMutex, LinuxApplicationMutex>();
            services.AddSingleton<IApplicationMutex, WindowsApplicationMutex>();
            return services;
        }
    }
}