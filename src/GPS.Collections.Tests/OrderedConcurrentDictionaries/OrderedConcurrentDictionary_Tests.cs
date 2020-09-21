using System;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

namespace GPS.Collections.Tests
{
    public class OrderedConcurrentDictionary_Tests
    {
        readonly ITestOutputHelper _log;

        public OrderedConcurrentDictionary_Tests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void TryAddNotPresent()
        {
            var ocd = new OrderedConcurrentDictionary<int, string>();

            Assert.True(ocd.TryAdd(1, "one"));
        }

        [Fact]
        public void TryAddPresent()
        {
            var ocd = new OrderedConcurrentDictionary<int, string>();

            ocd.TryAdd(1, "one");
            Assert.False(ocd.TryAdd(1, "not one"));
            Assert.Equal("one", ocd[1]);
        }

        [Fact]
        public void TryAddOrUpdate()
        {
            static string Updater(int _, string v) => "not " + v;
            var ocd = new OrderedConcurrentDictionary<int, string>();

            ocd.AddOrUpdate(1, "one", Updater);
            Assert.Equal("one", ocd[1]);
            ocd.AddOrUpdate(1, "one", Updater);
            Assert.Equal("not one", ocd[1]);
        }

        [Theory]
        [ClassData(typeof(OrderedConcurrentDictionaryComparisonSet))]
        public void IsOrderPreserved(Dictionary<int, string> values)
        {
            var ocd = new OrderedConcurrentDictionary<int, string>(values);

            var original = values.ToArray();
            var data = ocd.ToArray();

            Assert.Equal(original.Length, data.Length);

            for (int i = 0; i < original.Length; ++i)
            {
                _log.WriteLine($"[{original[i].Key}, {original[i].Value}] : [{data[i].Key}, {data[i].Value}]");
                Assert.Equal(original[i].Key, data[i].Key);
                Assert.Equal(original[i].Value, data[i].Value);
            }
        }

        [Theory]
        [ClassData(typeof(OrderedConcurrentDictionaryComparisonSet))]
        public void IsSorted(Dictionary<int, string> values)
        {
            var ocd = new OrderedConcurrentDictionary<int, string>(values);
            ocd.Reorder();

            var original = values.OrderBy(p => p.Key).ToArray();
            var data = ocd.ToArray();

            Assert.Equal(original.Length, data.Length);

            for (int i = 0; i < original.Length; ++i)
            {
                _log.WriteLine($"[{original[i].Key}, {original[i].Value}] : [{data[i].Key}, {data[i].Value}]");
                Assert.Equal(original[i].Key, data[i].Key);
                Assert.Equal(original[i].Value, data[i].Value);
            }
        }

        [Theory]
        [ClassData(typeof(OrderedConcurrentDictionaryComparisonSet))]
        public void IsSortedDescending(Dictionary<int, string> values)
        {
            var ocd = new OrderedConcurrentDictionary<int, string>(values);
            ocd.Reorder(ReorderDirection.Descending);

            var original = values.OrderByDescending(p => p.Key).ToArray();
            var data = ocd.ToArray();

            Assert.Equal(original.Length, data.Length);

            for (int i = 0; i < original.Length; ++i)
            {
                _log.WriteLine($"[{original[i].Key}, {original[i].Value}] : [{data[i].Key}, {data[i].Value}]");
                Assert.Equal(original[i].Key, data[i].Key);
                Assert.Equal(original[i].Value, data[i].Value);
            }
        }

        [Theory]
        [ClassData(typeof(OrderedConcurrentDictionaryComparisonSet))]
        public void CompareEnumerators(Dictionary<int, string> values)
        {
            var ocd = new OrderedConcurrentDictionary<int, string>(values);

            var enumerator = ocd.GetEnumerator();
            var dictionaryEnumerator = ((IDictionary)ocd).GetEnumerator();

            var eState = enumerator.MoveNext();
            var dState = dictionaryEnumerator.MoveNext();

            while (eState && dState)
            {
                _log.WriteLine($"{enumerator.Current.Key} ??? {dictionaryEnumerator.Key}");
                Assert.Equal(enumerator.Current.Key, dictionaryEnumerator.Key);
                _log.WriteLine($"{enumerator.Current.Value} ??? {dictionaryEnumerator.Value}");
                Assert.Equal(enumerator.Current.Value, dictionaryEnumerator.Value);

                eState = enumerator.MoveNext();
                dState = dictionaryEnumerator.MoveNext();
            }

            Assert.Equal(eState, dState);
        }
    }
}


/*
 * (C) 2019 Gateway Programming School , Inc.
 */
