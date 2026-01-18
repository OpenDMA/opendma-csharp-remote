using System;
using System.Collections.Generic;
using System.Linq;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Utils;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Parses ObjectWireModel into IOdmaObject instances
    /// </summary>
    public static class ObjectDataParser
    {
        /// <summary>
        /// Parses wire model object data into a property dictionary
        /// </summary>
        public static Dictionary<OdmaQName, IOdmaProperty> ParseObjectData(
            ObjectWireModel wire,
            RemoteConnection connection,
            OdmaId repositoryId)
        {
            var properties = new Dictionary<OdmaQName, IOdmaProperty>();
            var objectId = new OdmaId(wire.Id);

            foreach (var wireProp in wire.Properties)
            {
                var propName = OdmaQName.FromString(wireProp.Name);
                var propType = OdmaTypeHelper.FromString(wireProp.Type);

                object? value = null;
                IOdmaLazyPropertyValueProvider? provider = null;

                if (wireProp.Resolved)
                {
                    value = JsonValueParser.ParsePropertyValue(wireProp, connection, repositoryId, objectId, propName);
                    
                    // Check if we got an OdmaId back instead of an object
                    // This happens when the reference is "resolved" but only contains an ID
                    if (value is OdmaId refId && propType == OdmaType.REFERENCE && !wireProp.MultiValue)
                    {
                        // Create lazy provider for this incomplete reference
                        provider = new LazyReferencePropertyValueProvider(connection, repositoryId, refId);
                        value = null;
                    }
                }
                else
                {
                    // Property is not resolved - always create a lazy provider
                    // Use LazyRemotePropertyValueProvider which re-fetches the object with this property
                    provider = new LazyRemotePropertyValueProvider(connection, repositoryId, objectId, propName);
                }

                var property = new OdmaProperty(
                    propName,
                    value,
                    provider,
                    propType,
                    wireProp.MultiValue,
                    wireProp.ReadOnly);

                properties[propName] = property;
            }

            return properties;
        }

        /// <summary>
        /// Creates an IOdmaObject from wire model
        /// </summary>
        public static IOdmaObject CreateObject(
            ObjectWireModel wire,
            RemoteConnection connection,
            OdmaId repositoryId)
        {
            var objectId = new OdmaId(wire.Id);
            var properties = ParseObjectData(wire, connection, repositoryId);
            var complete = wire.Complete ?? false;

            var coreObject = new RemoteCoreObject(
                connection,
                repositoryId,
                objectId,
                properties,
                complete);

            // Build list of class names for proxy creation
            var classNames = new List<OdmaQName>();
            
            if (wire.RootOdmaClassName != null)
            {
                classNames.Add(OdmaQName.FromString(wire.RootOdmaClassName));
            }
            
            foreach (var aspectName in wire.AspectRootOdmaNames)
            {
                classNames.Add(OdmaQName.FromString(aspectName));
            }

            // Use the API's proxy factory to create a properly typed proxy
            return OdmaProxyFactory.CreateProxy(coreObject, classNames);
        }
    }
}
