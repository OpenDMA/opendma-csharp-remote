using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Implementations;

namespace OpenDMA.Remote.Utils
{
    /// <summary>
    /// Parses JSON wire model values to OpenDMA property values
    /// </summary>
    public static class JsonValueParser
    {
        /// <summary>
        /// Parses a wire model property value to the appropriate .NET type
        /// </summary>
        public static object? ParsePropertyValue(
            PropertyWireModel wireProperty, 
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId objectId,
            OdmaQName propertyName)
        {
            var type = OdmaTypeHelper.FromString(wireProperty.Type);
            var value = wireProperty.Value;

            if (value == null)
            {
                return null;
            }

            if (wireProperty.MultiValue)
            {
                return ParseMultiValue(type, value, wireProperty.Resolved, connection, repositoryId, objectId, propertyName);
            }
            else
            {
                return ParseSingleValue(type, value, wireProperty.Resolved, connection, repositoryId);
            }
        }

        private static object? ParseSingleValue(
            OdmaType type,
            object value,
            bool resolved,
            RemoteConnection connection,
            OdmaId repositoryId)
        {
            switch (type)
            {
                case OdmaType.STRING:
                    if (value is JsonElement je)
                        return je.GetString();
                    return value.ToString();

                case OdmaType.INTEGER:
                    if (value is JsonElement je2)
                    {
                        // Try to get as string first (REST API encodes as strings)
                        if (je2.ValueKind == JsonValueKind.String)
                        {
                            var str = je2.GetString();
                            return str != null ? int.Parse(str, CultureInfo.InvariantCulture) : 0;
                        }
                        return je2.GetInt32();
                    }
                    return Convert.ToInt32(value);

                case OdmaType.SHORT:
                    if (value is JsonElement je3)
                    {
                        if (je3.ValueKind == JsonValueKind.String)
                        {
                            var str = je3.GetString();
                            return str != null ? short.Parse(str, CultureInfo.InvariantCulture) : (short)0;
                        }
                        return je3.GetInt16();
                    }
                    return Convert.ToInt16(value);

                case OdmaType.LONG:
                    if (value is JsonElement je4)
                    {
                        if (je4.ValueKind == JsonValueKind.String)
                        {
                            var str = je4.GetString();
                            return str != null ? long.Parse(str, CultureInfo.InvariantCulture) : 0L;
                        }
                        return je4.GetInt64();
                    }
                    return Convert.ToInt64(value);

                case OdmaType.FLOAT:
                    if (value is JsonElement je5)
                    {
                        if (je5.ValueKind == JsonValueKind.String)
                        {
                            var str = je5.GetString();
                            return str != null ? float.Parse(str, CultureInfo.InvariantCulture) : 0f;
                        }
                        return je5.GetSingle();
                    }
                    return Convert.ToSingle(value);

                case OdmaType.DOUBLE:
                    if (value is JsonElement je6)
                    {
                        if (je6.ValueKind == JsonValueKind.String)
                        {
                            var str = je6.GetString();
                            return str != null ? double.Parse(str, CultureInfo.InvariantCulture) : 0.0;
                        }
                        return je6.GetDouble();
                    }
                    return Convert.ToDouble(value);

                case OdmaType.BOOLEAN:
                    if (value is JsonElement je7)
                    {
                        // REST API encodes booleans as strings "true" or "false"
                        if (je7.ValueKind == JsonValueKind.String)
                        {
                            var str = je7.GetString();
                            return str != null ? bool.Parse(str) : false;
                        }
                        return je7.GetBoolean();
                    }
                    return Convert.ToBoolean(value);

                case OdmaType.DATETIME:
                    if (value is JsonElement je8)
                    {
                        if (je8.ValueKind == JsonValueKind.String)
                        {
                            var str = je8.GetString();
                            return str != null ? DateTime.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : DateTime.MinValue;
                        }
                        return je8.GetDateTime();
                    }
                    if (value is string str2)
                        return DateTime.Parse(str2, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    return Convert.ToDateTime(value);

                case OdmaType.BINARY:
                    if (value is JsonElement je9)
                    {
                        if (je9.ValueKind == JsonValueKind.String)
                        {
                            var str = je9.GetString();
                            return str != null ? Convert.FromBase64String(str) : Array.Empty<byte>();
                        }
                        return je9.GetBytesFromBase64();
                    }
                    if (value is string str3)
                        return Convert.FromBase64String(str3);
                    return (byte[])value;

                case OdmaType.ID:
                    if (value is JsonElement je10)
                        return new OdmaId(je10.GetString() ?? "");
                    return new OdmaId(value.ToString() ?? "");

                case OdmaType.GUID:
                    return ParseGuidValue(value);

                case OdmaType.CONTENT:
                    return ParseContentValue(value, connection, repositoryId);

                case OdmaType.REFERENCE:
                    return ParseReferenceValue(value, resolved, connection, repositoryId);

                default:
                    throw new OdmaServiceException($"Unknown type: {type}");
            }
        }

        private static object ParseMultiValue(
            OdmaType type,
            object value,
            bool resolved,
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId objectId,
            OdmaQName propertyName)
        {
            if (type == OdmaType.REFERENCE)
            {
                return ParseReferenceEnumeration(value, connection, repositoryId, objectId, propertyName);
            }

            var jsonElement = value as JsonElement? ?? JsonSerializer.SerializeToElement(value);
            
            if (jsonElement.ValueKind != JsonValueKind.Array)
            {
                throw new OdmaServiceException("Multi-value property must be an array");
            }

            switch (type)
            {
                case OdmaType.STRING:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .ToList();

                case OdmaType.INTEGER:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? int.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture) : e.GetInt32())
                        .ToList();

                case OdmaType.SHORT:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? short.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture) : e.GetInt16())
                        .ToList();

                case OdmaType.LONG:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? long.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture) : e.GetInt64())
                        .ToList();

                case OdmaType.FLOAT:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? float.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture) : e.GetSingle())
                        .ToList();

                case OdmaType.DOUBLE:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? double.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture) : e.GetDouble())
                        .ToList();

                case OdmaType.BOOLEAN:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? bool.Parse(e.GetString() ?? "false") : e.GetBoolean())
                        .ToList();

                case OdmaType.DATETIME:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? DateTime.Parse(e.GetString() ?? "", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : e.GetDateTime())
                        .ToList();

                case OdmaType.BINARY:
                    return jsonElement.EnumerateArray()
                        .Select(e => e.ValueKind == JsonValueKind.String ? Convert.FromBase64String(e.GetString() ?? "") : e.GetBytesFromBase64())
                        .ToList();

                case OdmaType.ID:
                    return jsonElement.EnumerateArray()
                        .Select(e => new OdmaId(e.GetString() ?? ""))
                        .ToList();

                case OdmaType.GUID:
                    return jsonElement.EnumerateArray()
                        .Select(e => ParseGuidValue(e))
                        .ToList();

                case OdmaType.CONTENT:
                    return jsonElement.EnumerateArray()
                        .Select(e => ParseContentValue(e, connection, repositoryId))
                        .ToList();

                default:
                    throw new OdmaServiceException($"Unknown type: {type}");
            }
        }

        private static OdmaGuid ParseGuidValue(object value)
        {
            if (value is JsonElement je)
            {
                var guidWire = JsonSerializer.Deserialize<GuidValueWireModel>(je.GetRawText());
                if (guidWire == null)
                    throw new OdmaServiceException("Failed to parse GUID value");
                return new OdmaGuid(new OdmaId(guidWire.RepositoryId), new OdmaId(guidWire.ObjectId));
            }
            
            var guidWire2 = value as GuidValueWireModel;
            if (guidWire2 == null)
                throw new OdmaServiceException("Invalid GUID value format");
            
            return new OdmaGuid(new OdmaId(guidWire2.RepositoryId), new OdmaId(guidWire2.ObjectId));
        }

        private static IOdmaContent ParseContentValue(object value, RemoteConnection connection, OdmaId repositoryId)
        {
            if (value is JsonElement je)
            {
                var contentWire = JsonSerializer.Deserialize<ContentValueWireModel>(je.GetRawText());
                if (contentWire == null)
                    throw new OdmaServiceException("Failed to parse content value");
                return new RemoteContent(connection, repositoryId, contentWire.Id, contentWire.Size);
            }
            
            var contentWire2 = value as ContentValueWireModel;
            if (contentWire2 == null)
                throw new OdmaServiceException("Invalid content value format");
            
            return new RemoteContent(connection, repositoryId, contentWire2.Id, contentWire2.Size);
        }

        private static object? ParseReferenceValue(
            object value,
            bool resolved,
            RemoteConnection connection,
            OdmaId repositoryId)
        {
            if (!resolved)
            {
                // Create lazy provider for unresolved reference
                var refId = new OdmaId(value.ToString() ?? "");
                return null; // Will use lazy provider in OdmaProperty
            }

            // Parse inline object
            if (value is JsonElement je)
            {
                var objWire = JsonSerializer.Deserialize<ObjectWireModel>(je.GetRawText());
                if (objWire == null)
                    throw new OdmaServiceException("Failed to parse reference object");
                
                // Check if this is an incomplete object (only has ID, no class information)
                // In this case, return the ID instead of trying to create an incomplete object
                if (string.IsNullOrEmpty(objWire.RootOdmaClassName) && 
                    (objWire.Properties == null || objWire.Properties.Count == 0))
                {
                    // This is just an ID reference - return the ID so a lazy provider can be created
                    return new OdmaId(objWire.Id);
                }
                
                return ObjectDataParser.CreateObject(objWire, connection, repositoryId);
            }

            return value;
        }

        private static IEnumerable<IOdmaObject> ParseReferenceEnumeration(
            object value,
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId objectId,
            OdmaQName propertyName)
        {
            if (value is JsonElement je)
            {
                var enumWire = JsonSerializer.Deserialize<ReferenceEnumerationWireModel>(je.GetRawText());
                if (enumWire == null)
                    throw new OdmaServiceException("Failed to parse reference enumeration");
                
                return new PagingReferenceEnumerable(enumWire, connection, repositoryId, objectId, propertyName);
            }

            var enumWire2 = value as ReferenceEnumerationWireModel;
            if (enumWire2 == null)
                throw new OdmaServiceException("Invalid reference enumeration format");
            
            return new PagingReferenceEnumerable(enumWire2, connection, repositoryId, objectId, propertyName);
        }
    }
}
