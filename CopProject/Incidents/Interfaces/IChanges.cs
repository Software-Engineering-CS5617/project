namespace Incidents;

// Generics <T> provides compile-time safety over object.
public interface IChange<T>
{
    T OldValue { get; }
    T NewValue { get; }
    DateTime UploadTime { get; }
    Guid UploadedBy { get; }
}
