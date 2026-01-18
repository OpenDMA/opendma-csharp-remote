using System;
using System.Collections.Generic;
using OpenDMA.Api;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Implementation of IOdmaSearchResult for remote search results
    /// </summary>
    public class RemoteSearchResult : IOdmaSearchResult
    {
        private readonly List<IOdmaObject> _objects;

        /// <summary>
        /// Creates a new remote search result
        /// </summary>
        public RemoteSearchResult(List<IOdmaObject> objects)
        {
            _objects = objects;
        }

        public IEnumerable<IOdmaObject> GetObjects()
        {
            return _objects;
        }

        public int GetSize()
        {
            return _objects.Count;
        }
    }
}
