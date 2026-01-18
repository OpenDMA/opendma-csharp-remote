using System;
using System.Collections.Generic;
using System.Linq;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Implementations;

namespace OpenDMA.Remote
{
    /// <summary>
    /// Factory for creating remote OpenDMA sessions
    /// </summary>
    public static class RemoteSessionFactory
    {
        /// <summary>
        /// Connects to an OpenDMA REST service and returns a session
        /// </summary>
        /// <param name="endpoint">Base URL of the OpenDMA service (e.g., "http://localhost:8080/opendma")</param>
        /// <param name="username">Optional username for Basic authentication</param>
        /// <param name="password">Optional password for Basic authentication</param>
        /// <param name="requestTraceLevel">Trace level: 0=none, 1=urls, 2=timing, 3=responses</param>
        /// <returns>An IOdmaSession connected to the remote service</returns>
        public static IOdmaSession Connect(
            string endpoint,
            string? username = null,
            string? password = null,
            int requestTraceLevel = 0)
        {
            var connection = new RemoteConnection(endpoint, username, password, requestTraceLevel);

            // Fetch service descriptor
            var task = connection.GetServiceDescriptorAsync();
            var descriptor = task.GetAwaiter().GetResult();

            // Parse repository IDs
            var repositories = descriptor.Repositories
                .Select(r => new OdmaId(r))
                .ToList();

            // Parse supported query languages
            var queryLanguages = descriptor.SupportedQueryLanguages
                .Select(ql => OdmaQName.FromString(ql))
                .ToList();

            // Create and return session
            return new RemoteSession(
                connection,
                descriptor.OpendmaVersion,
                descriptor.ServiceVersion,
                repositories,
                queryLanguages);
        }
    }
}
