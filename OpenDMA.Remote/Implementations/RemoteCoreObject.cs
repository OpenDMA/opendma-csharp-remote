using System;
using System.Collections.Generic;
using System.Linq;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Utils;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Implementation of IOdmaCoreObject for remote objects
    /// </summary>
    public class RemoteCoreObject : IOdmaCoreObject
    {
        private readonly RemoteConnection _connection;
        private readonly OdmaId _repositoryId;
        private readonly OdmaId _objectId;
        private readonly Dictionary<OdmaQName, IOdmaProperty> _properties;
        private bool _complete;

        /// <summary>
        /// Creates a new remote core object
        /// </summary>
        public RemoteCoreObject(
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId objectId,
            Dictionary<OdmaQName, IOdmaProperty> properties,
            bool complete)
        {
            _connection = connection;
            _repositoryId = repositoryId;
            _objectId = objectId;
            _properties = properties;
            _complete = complete;
        }

        public IOdmaProperty GetProperty(OdmaQName propertyName)
        {
            // Check if property is already loaded
            if (_properties.TryGetValue(propertyName, out var property))
            {
                return property;
            }

            // If not complete, try to load this specific property
            if (!_complete)
            {
                PrepareProperties(new[] { propertyName }, false);
                
                if (_properties.TryGetValue(propertyName, out property))
                {
                    return property;
                }
            }

            throw new OdmaPropertyNotFoundException(propertyName);
        }

        public void PrepareProperties(OdmaQName[] propertyNames, bool refresh)
        {
            // Determine which properties to fetch
            List<OdmaQName> propsToFetch = new List<OdmaQName>();

            if (propertyNames == null || propertyNames.Length == 0)
            {
                // Fetch all properties
                if (refresh || !_complete)
                {
                    propsToFetch = null!; // null means fetch all
                }
                else
                {
                    return; // Already have everything
                }
            }
            else
            {
                foreach (var propName in propertyNames)
                {
                    if (refresh || !_properties.ContainsKey(propName))
                    {
                        propsToFetch.Add(propName);
                    }
                }

                if (propsToFetch.Count == 0)
                {
                    return; // Nothing to fetch
                }
            }

            // Build include parameter
            string? include = propsToFetch == null 
                ? "*:*" 
                : IncludeParameterBuilder.Build(propsToFetch.ToArray(), false);

            // Fetch from server
            var task = _connection.GetObjectAsync(_repositoryId, _objectId, include);
            var wire = task.GetAwaiter().GetResult();

            // Parse and merge properties
            var newProperties = ObjectDataParser.ParseObjectData(wire, _connection, _repositoryId);
            
            foreach (var kvp in newProperties)
            {
                _properties[kvp.Key] = kvp.Value;
            }

            if (wire.Complete == true)
            {
                _complete = true;
            }
        }

        public void SetProperty(OdmaQName propertyName, object? newValue)
        {
            var property = GetProperty(propertyName);
            property.Value = newValue;
        }

        public bool IsDirty => _properties.Values.Any(p => p.IsDirty);

        public void Save()
        {
            // TODO: Implement save operation when REST API supports it
            throw new NotSupportedException("Save operation is not yet supported by the remote client");
        }

        public bool InstanceOf(OdmaQName classOrAspectName)
        {
            // Get the class property
            var classProperty = _properties.TryGetValue(OdmaCommonNames.PROPERTY_CLASS, out var prop) 
                ? prop 
                : null;

            if (classProperty != null)
            {
                var odmaClass = classProperty.GetReference() as IOdmaClass;
                if (odmaClass != null && CheckClassHierarchy(odmaClass, classOrAspectName))
                {
                    return true;
                }
            }

            // Check aspects
            var aspectsProperty = _properties.TryGetValue(OdmaCommonNames.PROPERTY_ASPECTS, out var aspectsProp) 
                ? aspectsProp 
                : null;

            if (aspectsProperty != null && aspectsProperty.IsMultiValue)
            {
                var aspects = aspectsProperty.GetReferenceEnumerable();
                foreach (var aspect in aspects)
                {
                    if (aspect is IOdmaClass aspectClass && CheckClassHierarchy(aspectClass, classOrAspectName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CheckClassHierarchy(IOdmaClass clazz, OdmaQName targetName)
        {
            var current = clazz;
            while (current != null)
            {
                if (current.QName == targetName)
                {
                    return true;
                }

                // Check included aspects
                try
                {
                    var includedAspects = current.GetProperty(OdmaCommonNames.PROPERTY_INCLUDEDASPECTS);
                    if (includedAspects.IsMultiValue)
                    {
                        foreach (var aspect in includedAspects.GetReferenceEnumerable())
                        {
                            if (aspect is IOdmaClass aspectClass && CheckClassHierarchy(aspectClass, targetName))
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (OdmaPropertyNotFoundException)
                {
                    // No included aspects
                }

                // Move to super class
                try
                {
                    var superClassProp = current.GetProperty(OdmaCommonNames.PROPERTY_SUPERCLASS);
                    current = superClassProp.GetReference() as IOdmaClass;
                }
                catch (OdmaPropertyNotFoundException)
                {
                    break;
                }
            }

            return false;
        }
    }
}
