using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace GPS.Collections.Tests
{
    [Serializable]
    public class ObservableDispatchedCollectionTests
    {
        static ITestOutputHelper _log;
        private TestDispatcher _testDispatcher;

        [Serializable]
        public class TestDispatcher : DispatcherProxy
        {
            public override Task<bool> TryDispatchAsync(Action action)
            {
                _log.WriteLine(action.ToString());

                return Task.FromResult(true);
            }
        }

        public ObservableDispatchedCollectionTests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void Constructor_Default()
        {
            var collection = new ObservableDispatchedCollection<TestDispatcher, object>(GetDispatcher());

            Assert.NotNull(collection);
        }

        private TestDispatcher GetDispatcher()
        {
            return _testDispatcher ??= new TestDispatcher();
        }

        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void Constructor_SetElement((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            if (data.size > data.index)
            {
                var dispatchedCollection = new ObservableDispatchedCollection<TestDispatcher, int>(new int[data.size], GetDispatcher());
                dispatchedCollection[data.index] = data.count;

                Assert.Equal(data.count, dispatchedCollection[data.index]);
            }
        }

        [Theory]
        [ClassData(typeof(IntDataSet2))]
        public void Constructor_Range((int[] set, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var dispatchedCollection = new ObservableDispatchedCollection<TestDispatcher, int>(data.set, GetDispatcher());

            Assert.Equal(data.set.Length, dispatchedCollection.Count);

            for (int i = 0; i < data.set.Length; ++i)
            {
                Assert.Equal(data.set[i], dispatchedCollection[i]);
            }
        }

        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void Add((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var dispatchedCollection = new ObservableDispatchedCollection<TestDispatcher, int>(GetDispatcher());
            dispatchedCollection.Add(data.index);

            Assert.Equal(1, (int)dispatchedCollection.Count);
            Assert.Equal(data.index, dispatchedCollection[0]);
        }

        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void Clear((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var dispatchedCollection = new ObservableDispatchedCollection<TestDispatcher, int>(new int[data.size], GetDispatcher());
            dispatchedCollection.Clear();

            Assert.Equal(0, (int)dispatchedCollection.Count);
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => dispatchedCollection[data.index]);
        }

        [Fact]
        public void Contains()
        {
            var dispatchedCollection = new ObservableDispatchedCollection<TestDispatcher, string>(new [] {"Test"}, GetDispatcher());
            Assert.True(dispatchedCollection.Contains("Test"));
            Assert.False(dispatchedCollection.Contains(""));
        }

        [Fact]
        public void IndexOf()
        {
            var dispatchedCollection = new ObservableDispatchedCollection<TestDispatcher, string>(new[] { "Test" }, GetDispatcher());
            Assert.Equal(0, dispatchedCollection.IndexOf("Test"));
            Assert.Equal(-1, dispatchedCollection.IndexOf(""));
        }

        [Theory]
        [ClassData(typeof(LinkedArrayComparisonSet))]
        public void Benchmarks(int[] data)
        {
            var lsw = new GPS.SimpleHelpers.Stopwatch.LoggingStopwatch();

            ObservableDispatchedCollection<TestDispatcher, int> collection = null;
            List<int> dispatchedCollection = null;
            Dictionary<int, int> dictionary = null;
            SortedDictionary<int, int> sortedDictionary = null;

            var set = new List<int>();

            for (int i = 0; i < 1; ++i)
            {
                var positiveData = new int[data.Length];
                for (int j = 0; j < data.Length; ++j)
                {
                    data[j] = (i * j + 10) ^ data[j] ;
                    positiveData[j] = Math.Abs(data[j]);
                }

                set.AddRange(positiveData);
            }

            _log.WriteLine("");

            for (int i = 0; i < 20; ++i)
            {
                lsw.Mark("Loading ObservableDispatchedCollection", () => collection = new ObservableDispatchedCollection<TestDispatcher, int>(set, GetDispatcher()));
                lsw.Mark("Loading List", () => dispatchedCollection = new List<int>(set));
                lsw.Mark("Loading Dictionary", () => { dictionary = new Dictionary<int, int>(); for(int j=0; j<set.Count; ++j) dictionary[set[j]] = set[j]; });
                lsw.Mark("Loading SortedDictionary", () => { sortedDictionary = new SortedDictionary<int, int>(); for(int j=0; j<set.Count; ++j) sortedDictionary[set[j]] = set[j]; });

                lsw.Mark("Find Last ObservableDispatchedCollection", () => collection.Contains(data.Last()));
                lsw.Mark("Find Last List", () => dispatchedCollection.Contains(data.Last()));
                lsw.Mark("Find Last Dictionary", () => dictionary.ContainsValue(data.Last()));
                lsw.Mark("Find Last SortedDictionary", () => sortedDictionary.ContainsValue(data.Last()));

                lsw.Mark("Enumerate ObservableDispatchedCollection", () => { foreach (var value in collection) ; });
                lsw.Mark("Enumerate List", () => { foreach (var value in dispatchedCollection) ; });
                lsw.Mark("Enumerate dictionary", () => { foreach (var value in dictionary) ; });
                lsw.Mark("Enumerate sortedDictionary", () => { foreach (var value in sortedDictionary) ; });

                collection.Dispose();
                collection = null;
                dispatchedCollection = null;
                dictionary = null;
                sortedDictionary = null;

                GC.Collect();
            }


            lsw.Stop();

            foreach (var mark in lsw.ExecutionMarks)
            {
                _log.WriteLine($"\"{mark.Mark}\",{mark.ExecutionMilliseconds}");
            }

            foreach(var mark in lsw.ElapsedMarks)
            {
                if(mark.Mark.StartsWith("Size")) _log.WriteLine($"{mark.Mark}");
            }
        }

        public long ObjectSize(object obj)
        {
            using (Stream stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();

                formatter.Serialize(stream, obj);

                return stream.Length;
            }
        }

    }
}

/*
 * (C) 2019 Your Legal Entity's Name
 */
