using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpenDMA.Api;

namespace OpenDMA.Remote.Connection
{
    /// <summary>
    /// Manages HTTP communication with the OpenDMA REST service
    /// </summary>
    public class RemoteConnection : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly int _traceLevel;
        private bool _disposed;

        /// <summary>
        /// Creates a new remote connection
        /// </summary>
        /// <param name="endpoint">Base URL of the OpenDMA service (e.g., "http://localhost:8080/opendma")</param>
        /// <param name="username">Optional username for Basic authentication</param>
        /// <param name="password">Optional password for Basic authentication</param>
        /// <param name="traceLevel">Trace level: 0=none, 1=urls, 2=timing, 3=responses</param>
        public RemoteConnection(string endpoint, string? username = null, string? password = null, int traceLevel = 0)
        {
            _endpoint = endpoint.TrimEnd('/');
            _traceLevel = traceLevel;
            _httpClient = new HttpClient();

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var authBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
                var authHeader = Convert.ToBase64String(authBytes);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            }
        }

        /// <summary>
        /// Gets the service descriptor
        /// </summary>
        public async Task<ServiceDescriptorWireModel> GetServiceDescriptorAsync()
        {
            var url = $"{_endpoint}/";
            return await GetAsync<ServiceDescriptorWireModel>(url, null, null);
        }

        /// <summary>
        /// Gets repository information
        /// </summary>
        public async Task<ObjectWireModel> GetRepositoryAsync(OdmaId repositoryId, string? include = null)
        {
            var url = $"{_endpoint}/obj/{Uri.EscapeDataString(repositoryId.ToString())}";
            if (!string.IsNullOrEmpty(include))
            {
                url += $"?include={Uri.EscapeDataString(include)}";
            }
            return await GetAsync<ObjectWireModel>(url, repositoryId, null);
        }

        /// <summary>
        /// Gets an object
        /// </summary>
        public async Task<ObjectWireModel> GetObjectAsync(OdmaId repositoryId, OdmaId objectId, string? include = null)
        {
            var url = $"{_endpoint}/obj/{Uri.EscapeDataString(repositoryId.ToString())}/{Uri.EscapeDataString(objectId.ToString())}";
            if (!string.IsNullOrEmpty(include))
            {
                url += $"?include={Uri.EscapeDataString(include)}";
            }
            return await GetAsync<ObjectWireModel>(url, repositoryId, objectId);
        }

        /// <summary>
        /// Executes a search
        /// </summary>
        public async Task<SearchResponseWireModel> SearchAsync(OdmaId repositoryId, OdmaQName language, string query)
        {
            var url = $"{_endpoint}/search/{Uri.EscapeDataString(repositoryId.ToString())}";
            var request = new SearchRequestWireModel
            {
                Language = language.ToString(),
                Query = query
            };
            return await PostAsync<SearchRequestWireModel, SearchResponseWireModel>(url, request, repositoryId, true);
        }

        /// <summary>
        /// Gets binary content stream
        /// </summary>
        public async Task<Stream> GetContentStreamAsync(OdmaId repositoryId, string contentId)
        {
            var url = $"{_endpoint}/bin/{Uri.EscapeDataString(repositoryId.ToString())}/{Uri.EscapeDataString(contentId)}";
            
            if (_traceLevel > 0)
            {
                Console.WriteLine($">>>> {url}");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            if (_traceLevel > 1)
            {
                Console.WriteLine($"<<<< Duration: {stopwatch.ElapsedMilliseconds}ms");
            }

            await HandleErrorStatusCode(response, repositoryId, null);
            return await response.Content.ReadAsStreamAsync();
        }

        private async Task<T> GetAsync<T>(string url, OdmaId? repositoryId = null, OdmaId? objectId = null)
        {
            if (_traceLevel > 0)
            {
                Console.WriteLine($">>>> {url}");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            if (_traceLevel > 1)
            {
                Console.WriteLine($"<<<< Duration: {stopwatch.ElapsedMilliseconds}ms");
            }

            await HandleErrorStatusCode(response, repositoryId, objectId);

            var content = await response.Content.ReadAsStringAsync();
            
            if (_traceLevel > 2)
            {
                Console.WriteLine($"<<<< {content}");
            }

            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new OdmaServiceException("Failed to deserialize response");
            }

            return result;
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, OdmaId? repositoryId = null, bool isSearchRequest = false)
        {
            if (_traceLevel > 0)
            {
                Console.WriteLine($">>>> POST {url}");
            }

            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsync(url, httpContent);
            stopwatch.Stop();

            if (_traceLevel > 1)
            {
                Console.WriteLine($"<<<< Duration: {stopwatch.ElapsedMilliseconds}ms");
            }

            await HandleErrorStatusCode(response, repositoryId, null, isSearchRequest);

            var content = await response.Content.ReadAsStringAsync();
            
            if (_traceLevel > 2)
            {
                Console.WriteLine($"<<<< {content}");
            }

            var result = JsonSerializer.Deserialize<TResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new OdmaServiceException("Failed to deserialize response");
            }

            return result;
        }

        private static async Task HandleErrorStatusCode(HttpResponseMessage response, OdmaId? repositoryId = null, OdmaId? objectId = null, bool isSearchRequest = false)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new OdmaAuthenticationException("Authentication failed");
                
                case HttpStatusCode.Forbidden:
                    throw new OdmaAccessDeniedException($"Access denied: {content}");
                
                case HttpStatusCode.NotFound:
                    if (repositoryId != null && objectId != null)
                    {
                        throw new OdmaObjectNotFoundException(repositoryId, objectId);
                    }
                    else if (repositoryId != null)
                    {
                        throw new OdmaObjectNotFoundException(repositoryId);
                    }
                    else
                    {
                        throw new OdmaException($"Resource not found: {content}");
                    }
                
                case HttpStatusCode.BadRequest:
                    if (isSearchRequest)
                    {
                        throw new OdmaQuerySyntaxException($"Invalid query syntax: {content}");
                    }
                    else
                    {
                        throw new OdmaServiceException($"Bad request: {content}");
                    }
                
                default:
                    throw new OdmaServiceException($"HTTP {(int)response.StatusCode}: {content}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
