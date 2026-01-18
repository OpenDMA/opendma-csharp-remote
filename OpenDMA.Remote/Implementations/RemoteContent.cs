using System;
using System.IO;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Implementation of IOdmaContent for remote content streams
    /// </summary>
    public class RemoteContent : IOdmaContent
    {
        private readonly RemoteConnection _connection;
        private readonly OdmaId _repositoryId;
        private readonly string _contentId;
        private readonly long _size;

        public RemoteContent(
            RemoteConnection connection,
            OdmaId repositoryId,
            string contentId,
            long size)
        {
            _connection = connection;
            _repositoryId = repositoryId;
            _contentId = contentId;
            _size = size;
        }

        public long GetSize() => _size;

        public Stream GetStream()
        {
            var task = _connection.GetContentStreamAsync(_repositoryId, _contentId);
            return task.GetAwaiter().GetResult();
        }
    }
}
