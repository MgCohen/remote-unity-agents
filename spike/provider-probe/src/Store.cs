namespace Probe;

// ============================================================================
// THE ARGS-PLAY — one uniform contract, provider-specific input, uniform output.
//
//   IStore<TArgs, TReturn> :  TReturn Get(TArgs)  /  Save(TArgs, TReturn)
//
// The store is a REAL component that SURVIVES into emitted code (like scope.Ask, not like
// Loop): the handler calls store.Get / store.Save uniformly. Each concrete provider FIXES its
// input type (TArgs) and hides its idiomatic surface (Load/Store, Download/Upload) INSIDE the
// adapter. Acquisition is a direct free function — `Repository<User>()` / `Bucket<User>()`,
// brought in with `using static` exactly like Mutate — so swapping the provider is one token.
//
//   Repository<User>() : IStore<string,    User>   -> Get(string)    (key = string)
//   Bucket<User>()     : IStore<BucketKey, User>   -> Get(BucketKey) (key = a path)
//
// Because Mutate takes `key: TArgs`, the key expression must have THIS provider's TArgs — swap
// the store and a stale key is a plain type error (string vs BucketKey).
// ============================================================================

public readonly record struct BucketKey(string Path);

public interface IStore<TArgs, TReturn> where TReturn : notnull
{
    TReturn Get(TArgs args);
    void Save(TArgs args, TReturn value);
}

// Concrete provider A — a keyed repository. Idiomatic surface: Load / Store by string key.
sealed class KeyedRepo<T> where T : notnull
{
    readonly Dictionary<string, T> _store = new();
    public T Load(string key) => _store.TryGetValue(key, out var v)
        ? v : throw new KeyNotFoundException($"No {typeof(T).Name} at key '{key}'.");
    public void Store(string key, T value) => _store[key] = value;
}

// Concrete provider B — a blob store. Idiomatic surface: Download / Upload by BucketKey.
sealed class Blob<T> where T : notnull
{
    readonly Dictionary<string, T> _store = new();
    public T Download(BucketKey key) => _store.TryGetValue(key.Path, out var v)
        ? v : throw new KeyNotFoundException($"No {typeof(T).Name} at bucket path '{key.Path}'.");
    public void Upload(BucketKey key, T value) => _store[key.Path] = value;
}

// The adapters map the uniform verbs (Get/Save) onto each provider's idiomatic methods.
// The emitted code calls Get/Save; the idiomatic call lives HERE, not in a string table.
sealed class RepoStore<T>(KeyedRepo<T> repo) : IStore<string, T> where T : notnull
{
    public T Get(string args) => repo.Load(args);
    public void Save(string args, T value) => repo.Store(args, value);
}

sealed class BlobStore<T>(Blob<T> blob) : IStore<BucketKey, T> where T : notnull
{
    public T Get(BucketKey args) => blob.Download(args);
    public void Save(BucketKey args, T value) => blob.Upload(args, value);
}

// STATIC stores as DIRECT free functions — `using static Probe.Stores` makes them unqualified
// (Repository<User>() / Bucket<User>()), like Mutate. One singleton per aggregate type, so the
// emitted handler reads/writes shared state with no injected parameter.
public static class Stores
{
    public static IStore<string, T> Repository<T>() where T : notnull => RepoHolder<T>.Instance;
    public static IStore<BucketKey, T> Bucket<T>() where T : notnull => BlobHolder<T>.Instance;

    static class RepoHolder<T> where T : notnull
    {
        public static readonly RepoStore<T> Instance = new(new KeyedRepo<T>());
    }

    static class BlobHolder<T> where T : notnull
    {
        public static readonly BlobStore<T> Instance = new(new Blob<T>());
    }
}
