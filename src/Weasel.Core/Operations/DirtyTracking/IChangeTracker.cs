using System.Diagnostics.CodeAnalysis;

namespace Weasel.Core.Operations.DirtyTracking;

public interface IChangeTracker
{
    object Document { get; }
    bool DetectChanges(IStorageSession session, [NotNullWhen(true)]out IStorageOperation? operation);
    void Reset(IStorageSession session);
}
