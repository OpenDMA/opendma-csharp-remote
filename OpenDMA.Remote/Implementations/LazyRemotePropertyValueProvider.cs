using System;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Utils;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Lazy property value provider that fetches the property value from the server
    /// by re-fetching the object with the specific property included
    /// </summary>
    public class LazyRemotePropertyValueProvider : IOdmaLazyPropertyValueProvider
    {
        private readonly RemoteConnection _connection;
        private readonly OdmaId _repositoryId;
        private readonly OdmaId _objectId;
        private readonly OdmaQName _propertyName;

        public LazyRemotePropertyValueProvider(
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId objectId,
            OdmaQName propertyName)
        {
            _connection = connection;
            _repositoryId = repositoryId;
            _objectId = objectId;
            _propertyName = propertyName;
        }

        public bool HasReferenceId() => false;

        public OdmaId GetReferenceId()
        {
            throw new NotSupportedException("This provider fetches property values, not object references");
        }

        public object? ResolvePropertyValue()
        {
            // Fetch the object with this specific property included
            var include = IncludeParameterBuilder.Build(new[] { _propertyName }, false);
            var task = _connection.GetObjectAsync(_repositoryId, _objectId, include);
            var wire = task.GetAwaiter().GetResult();
            
            // Parse the object data and get the property value
            var properties = ObjectDataParser.ParseObjectData(wire, _connection, _repositoryId);
            
            if (properties.TryGetValue(_propertyName, out var property))
            {
                return property.Value;
            }
            
            throw new OdmaPropertyNotFoundException(_propertyName);
        }
    }
}
