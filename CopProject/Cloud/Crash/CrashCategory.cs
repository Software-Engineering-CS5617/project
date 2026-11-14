namespace Cloud.Crash;

/// <summary>
/// Broad kind of failure, derived from the root-cause exception type.
/// </summary>
public enum CrashCategory
{
    /// <summary>Anything that matches no other category.</summary>
    Other,

    /// <summary>A null reference was dereferenced.</summary>
    NullReference,

    /// <summary>A method received an invalid argument.</summary>
    InvalidArgument,

    /// <summary>An operation was invalid for the current state of an object.</summary>
    InvalidState,

    /// <summary>File or directory access failed.</summary>
    FileSystem,

    /// <summary>A network call failed.</summary>
    Network,

    /// <summary>A storage (database) operation failed.</summary>
    Storage,

    /// <summary>A task or thread was cancelled, aborted or deadlocked.</summary>
    Concurrency,

    /// <summary>Data could not be parsed, converted or serialized.</summary>
    DataFormat,

    /// <summary>The process ran out of a resource such as memory or stack.</summary>
    Resources,

    /// <summary>The OS, COM or native interop failed.</summary>
    Platform,

    /// <summary>A logic error such as an index, key or arithmetic failure.</summary>
    Logic
}
