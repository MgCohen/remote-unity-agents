using System.Reflection;
using ABox.Features.Threads.Add;

namespace ABox.Features.Threads.Module;

public static class ThreadsModule
{
    public static Assembly EndpointsAssembly => typeof(AddThreadEndpoint).Assembly;
}
