using System;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Utils;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Lazy property value provider for remote references
    /// </summary>
    public class LazyReferencePropertyValueProvider : IOdmaLazyPropertyValueProvider
    {
        private readonly RemoteConnection _connection;
        private readonly OdmaId _repositoryId;
        private readonly OdmaId _referenceId;

        public LazyReferencePropertyValueProvider(
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId referenceId)
        {
            _connection = connection;
            _repositoryId = repositoryId;
            _referenceId = referenceId;
        }

        public bool HasReferenceId() => true;

        public OdmaId GetReferenceId() => _referenceId;

        public object? ResolvePropertyValue()
        {
            // Fetch the referenced object
            var task = _connection.GetObjectAsync(_repositoryId, _referenceId, "default");
            var wire = task.GetAwaiter().GetResult();
            return ObjectDataParser.CreateObject(wire, _connection, _repositoryId);
        }
    }
}
