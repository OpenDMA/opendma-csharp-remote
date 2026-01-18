using System;
using System.Collections.Generic;
using System.Linq;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Utils;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Implementation of IOdmaSession for remote connections
    /// </summary>
    public class RemoteSession : IOdmaSession
    {
        private readonly RemoteConnection _connection;
        private readonly string _opendmaVersion;
        private readonly string _serviceVersion;
        private readonly List<OdmaId> _repositories;
        private readonly List<OdmaQName> _supportedQueryLanguages;
        private bool _disposed;

        /// <summary>
        /// Creates a new remote session
        /// </summary>
        public RemoteSession(
            RemoteConnection connection,
            string opendmaVersion,
            string serviceVersion,
            List<OdmaId> repositories,
            List<OdmaQName> supportedQueryLanguages)
        {
            _connection = connection;
            _opendmaVersion = opendmaVersion;
            _serviceVersion = serviceVersion;
            _repositories = repositories;
            _supportedQueryLanguages = supportedQueryLanguages;
        }

        public IList<OdmaId> GetRepositoryIds()
        {
            return _repositories;
        }

        public IOdmaRepository GetRepository(OdmaId repositoryId)
        {
            var task = _connection.GetRepositoryAsync(repositoryId, "default");
            var wire = task.GetAwaiter().GetResult();
            var obj = ObjectDataParser.CreateObject(wire, _connection, repositoryId);
            
            if (obj is IOdmaRepository repo)
            {
                return repo;
            }

            throw new OdmaServiceException("Server did not return a valid repository object");
        }

        public IOdmaObject GetObject(OdmaId repositoryId, OdmaId objectId, OdmaQName[] propertyNames)
        {
            string? include = null;
            if (propertyNames != null && propertyNames.Length > 0)
            {
                include = IncludeParameterBuilder.Build(propertyNames, true);
            }
            else
            {
                include = "default";
            }

            var task = _connection.GetObjectAsync(repositoryId, objectId, include);
            var wire = task.GetAwaiter().GetResult();
            return ObjectDataParser.CreateObject(wire, _connection, repositoryId);
        }

        public IOdmaSearchResult Search(OdmaId repositoryId, OdmaQName queryLanguage, string query)
        {
            var task = _connection.SearchAsync(repositoryId, queryLanguage, query);
            var wire = task.GetAwaiter().GetResult();

            var objects = new List<IOdmaObject>();
            foreach (var itemWire in wire.Items)
            {
                var obj = ObjectDataParser.CreateObject(itemWire, _connection, repositoryId);
                objects.Add(obj);
            }

            return new RemoteSearchResult(objects);
        }

        public IList<OdmaQName> GetSupportedQueryLanguages()
        {
            return _supportedQueryLanguages;
        }

        public void Close()
        {
            if (!_disposed)
            {
                _connection.Dispose();
                _disposed = true;
            }
        }
    }
}
