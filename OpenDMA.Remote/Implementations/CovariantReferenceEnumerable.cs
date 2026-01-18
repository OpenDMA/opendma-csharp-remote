using System;
using System.Collections;
using System.Collections.Generic;
using OpenDMA.Api;

namespace OpenDMA.Remote.Implementations
{
    /// <summary>
    /// A covariance-supporting wrapper for IEnumerable&lt;IOdmaObject&gt; 
    /// that can be cast to more specific types like IEnumerable&lt;IOdmaClass&gt;
    /// </summary>
    internal class CovariantReferenceEnumerable : IEnumerable<IOdmaObject>
    {
        private readonly IEnumerable<IOdmaObject> _source;

        public CovariantReferenceEnumerable(IEnumerable<IOdmaObject> source)
        {
            _source = source;
        }

        public IEnumerator<IOdmaObject> GetEnumerator()
        {
            foreach (var item in _source)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
