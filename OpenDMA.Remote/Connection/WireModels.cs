using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OpenDMA.Api;

namespace OpenDMA.Remote.Connection
{
    /// <summary>
    /// Wire model for service descriptor from GET /opendma
    /// </summary>
    public class ServiceDescriptorWireModel
    {
        [JsonPropertyName("opendmaVersion")]
        public string OpendmaVersion { get; set; } = "";

        [JsonPropertyName("serviceVersion")]
        public string ServiceVersion { get; set; } = "";

        [JsonPropertyName("repositories")]
        public List<string> Repositories { get; set; } = new List<string>();

        [JsonPropertyName("supportedQueryLanguages")]
        public List<string> SupportedQueryLanguages { get; set; } = new List<string>();
    }

    /// <summary>
    /// Wire model for property representation
    /// </summary>
    public class PropertyWireModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("multiValue")]
        public bool MultiValue { get; set; }

        [JsonPropertyName("readOnly")]
        public bool ReadOnly { get; set; }

        [JsonPropertyName("resolved")]
        public bool Resolved { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    /// <summary>
    /// Wire model for object representation
    /// </summary>
    public class ObjectWireModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("rootOdmaClassName")]
        public string? RootOdmaClassName { get; set; }

        [JsonPropertyName("aspectRootOdmaNames")]
        public List<string> AspectRootOdmaNames { get; set; } = new List<string>();

        [JsonPropertyName("properties")]
        public List<PropertyWireModel> Properties { get; set; } = new List<PropertyWireModel>();

        [JsonPropertyName("complete")]
        public bool? Complete { get; set; }
    }

    /// <summary>
    /// Wire model for reference enumeration with pagination
    /// </summary>
    public class ReferenceEnumerationWireModel
    {
        [JsonPropertyName("items")]
        public List<ObjectWireModel> Items { get; set; } = new List<ObjectWireModel>();

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    /// <summary>
    /// Wire model for content value
    /// </summary>
    public class ContentValueWireModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// Wire model for GUID value
    /// </summary>
    public class GuidValueWireModel
    {
        [JsonPropertyName("objectId")]
        public string ObjectId { get; set; } = "";

        [JsonPropertyName("repositoryId")]
        public string RepositoryId { get; set; } = "";
    }

    /// <summary>
    /// Wire model for search request
    /// </summary>
    public class SearchRequestWireModel
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "";

        [JsonPropertyName("query")]
        public string Query { get; set; } = "";
    }

    /// <summary>
    /// Wire model for search response
    /// </summary>
    public class SearchResponseWireModel
    {
        [JsonPropertyName("items")]
        public List<ObjectWireModel> Items { get; set; } = new List<ObjectWireModel>();
    }
}
