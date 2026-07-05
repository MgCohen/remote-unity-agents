namespace ABox.Threads.Tests.Support;

// Mirrors WireHostCollection (src/Host/Tests/Support) inside this assembly: xUnit collections cannot span
// assemblies, so any second host-booting Wire class added here must join this one or the FastEndpoints
// serializer race that collection documents fires in-process.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ThreadsWireCollection
{
    public const string Name = "ThreadsWire";
}
