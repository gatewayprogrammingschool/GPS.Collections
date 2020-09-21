using System.Collections;
using System.Collections.Generic;

namespace GPS.Collections.Tests
{
    public class OrderedConcurrentDictionaryComparisonSet : IEnumerable<object[]>
    {
        readonly Dictionary<int, string>[] _data = { 
            new Dictionary<int, string> { { 1, "One" }, { 2, "Two" }, { 3, "Three" }, },
            new Dictionary<int, string> { { 2, "Two" }, { 3, "Three" }, { 1, "One" }, },
            new Dictionary<int, string> { { 3, "Three" }, { 1, "One" }, { 2, "Two" }, }
        };
        
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var item in _data) yield return new [] {item};
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}


/*
 * (C) 2019 Gateway Programming School , Inc.
 */
