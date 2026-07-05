namespace ABox.Host.Tests.Support;

// Booting more than one WebApplicationFactory<Program> at once trips a FastEndpoints race in UseFastEndpoints
// (it copies the shared JsonSerializerOptions converter list while another host mutates it). Production has a
// single host, so this is a test-only hazard — every Wire class that boots a Host joins this collection so
// they run sequentially rather than in parallel with each other. An xUnit collection cannot span assemblies,
// so co-located Wire suites (ABox.Threads.Tests) instead rely on `dotnet test dirs.proj` running test
// assemblies sequentially — revisit both if that ever goes parallel.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WireHostCollection
{
    public const string Name = "WireHost";
}
