using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ABox.Features.Threads.Add;

namespace ABox.Features.Threads.Module;

public static class ThreadsModule
{
    public static Assembly EndpointsAssembly => typeof(AddThreadEndpoint).Assembly;

    public static IServiceCollection AddThreads(this IServiceCollection services)
    {
        services.AddSingleton<IThreadFiles, ThreadFiles>();
        return services;
    }
}
