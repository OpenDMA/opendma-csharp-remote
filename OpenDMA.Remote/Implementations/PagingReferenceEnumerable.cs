using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenDMA.Api;
using OpenDMA.Remote.Connection;
using OpenDMA.Remote.Utils;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// Enumerable for paginated reference collections that supports covariance
    /// </summary>
    internal class PagingReferenceEnumerable : IEnumerable<IOdmaObject>
    {
        private readonly ReferenceEnumerationWireModel _initialPage;
        private readonly RemoteConnection _connection;
        private readonly OdmaId _repositoryId;
        private readonly OdmaId _objectId;
        private readonly OdmaQName _propertyName;

        public PagingReferenceEnumerable(
            ReferenceEnumerationWireModel initialPage,
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId objectId,
            OdmaQName propertyName)
        {
            _initialPage = initialPage;
            _connection = connection;
            _repositoryId = repositoryId;
            _objectId = objectId;
            _propertyName = propertyName;
        }

        public IEnumerator<IOdmaObject> GetEnumerator()
        {
            return new PagingReferenceEnumerator(_initialPage, _connection, _repositoryId, _objectId, _propertyName);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Enumerator for paginated reference collections
    /// </summary>
    internal class PagingReferenceEnumerator : IEnumerator<IOdmaObject>
    {
        private readonly RemoteConnection _connection;
        private readonly OdmaId _repositoryId;
        private readonly OdmaId _objectId;
        private readonly OdmaQName _propertyName;
        private ReferenceEnumerationWireModel? _currentPage;
        private int _currentIndex;
        private IOdmaObject? _current;

        public PagingReferenceEnumerator(
            ReferenceEnumerationWireModel initialPage,
            RemoteConnection connection,
            OdmaId repositoryId,
            OdmaId objectId,
            OdmaQName propertyName)
        {
            _currentPage = initialPage;
            _connection = connection;
            _repositoryId = repositoryId;
            _objectId = objectId;
            _propertyName = propertyName;
            _currentIndex = -1;
        }

        public IOdmaObject Current => _current ?? throw new InvalidOperationException();

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_currentPage == null)
            {
                return false;
            }

            _currentIndex++;

            if (_currentIndex < _currentPage.Items.Count)
            {
                var wireItem = _currentPage.Items[_currentIndex];
                
                // Check if this is an ID-only object (no class information)
                // If so, fetch the full object from the server
                if (string.IsNullOrEmpty(wireItem.RootOdmaClassName))
                {
                    // Fetch the full object using its ID
                    var objectId = new OdmaId(wireItem.Id);
                    var task = _connection.GetObjectAsync(_repositoryId, objectId, "default");
                    var fullWireItem = task.GetAwaiter().GetResult();
                    _current = ObjectDataParser.CreateObject(fullWireItem, _connection, _repositoryId);
                }
                else
                {
                    // We have class information, create object directly
                    _current = ObjectDataParser.CreateObject(wireItem, _connection, _repositoryId);
                }
                
                return true;
            }

            // Check if there's a next page
            if (!string.IsNullOrEmpty(_currentPage.Next))
            {
                // Fetch the next page
                FetchNextPage();
                return MoveNext(); // Recurse to process first item of new page
            }

            return false;
        }

        private void FetchNextPage()
        {
            if (_currentPage == null || string.IsNullOrEmpty(_currentPage.Next))
            {
                return;
            }

            // Build include parameter using the next token
            var include = IncludeParameterBuilder.BuildWithNextToken(_propertyName, _currentPage.Next!);
            
            // Fetch the object with the next page parameter
            var task = _connection.GetObjectAsync(_repositoryId, _objectId, include);
            var wire = task.GetAwaiter().GetResult();
            
            // Extract the property from the wire model
            var propWire = wire.Properties.FirstOrDefault(p => 
                OdmaQName.FromString(p.Name).Equals(_propertyName));
            
            if (propWire == null)
            {
                throw new OdmaServiceException($"Property {_propertyName} not found in paginated response");
            }

            // Parse the reference enumeration from the property value
            var enumWire = System.Text.Json.JsonSerializer.Deserialize<ReferenceEnumerationWireModel>(
                propWire.Value?.ToString() ?? "{}");
            
            if (enumWire == null)
            {
                throw new OdmaServiceException("Failed to parse next page of reference enumeration");
            }

            _currentPage = enumWire;
            _currentIndex = -1; // Reset index for the new page
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
