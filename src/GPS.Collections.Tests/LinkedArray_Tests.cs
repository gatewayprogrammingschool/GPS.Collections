using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;
using Xunit.Abstractions;

namespace GPS.Collections.Tests
{
    public class LinkedArray_Tests
    {
        ITestOutputHelper _log;

        public LinkedArray_Tests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void Constructor_Default()
        {
            var linkedArray = new LinkedArray<object>();

            Assert.NotNull(linkedArray);
        }

        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void Constructor_Size((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var list = new LinkedArray<int>(data.size);

            Assert.Equal(0, (int)list.Count);
        }

        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void Constructor_SetElement((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var list = new LinkedArray<int>(data.index, data.count);

            Assert.Equal(data.count, list[data.index]);
        }

        [Theory]
        [ClassData(typeof(IntDataSet2))]
        public void Constructor_Range((int[] set, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var list = new LinkedArray<int>(data.set);

            Assert.Equal(data.set.Length, list.Count);
            Assert.Equal(0, list.Lowest);
            Assert.Equal(data.set.Length - 1, list.Highest);

            for (int i = 0; i < data.set.Length; ++i)
            {
                Assert.Equal(data.set[i], list[i]);
            }
        }

        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void Add((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var list = new LinkedArray<int>();
            list.Add(data.index);

            Assert.Equal(1, (int)list.Count);
            Assert.Equal(0, list.Lowest);
            Assert.Equal(0, list.Highest);
            Assert.Equal(data.index, list[0]);
        }

        [Theory]
        [ClassData(typeof(IntDataSet2))]
        public void AddRange((int[] set, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var list = new LinkedArray<int>();
            list.AddRange(data.set);
            list.AddRange(data.set);

            Assert.Equal(data.set.Length * 2, (int)list.Count);
            Assert.Equal(0, list.Lowest);
            Assert.Equal(data.set.Length * 2 - 1, list.Highest);

            for (int i = 0; i < data.set.Length * 2; ++i)
            {
                Assert.Equal(data.set[i % data.set.Length], list[i]);
            }
        }

        [Theory]
        [ClassData(typeof(IntDataSet2))]
        public void AddRangeAt((int[] set, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var list = new LinkedArray<int>();
            list.AddRangeAt(-1, data.set);

            Assert.Equal(data.set.Length, (int)list.Count);
            Assert.Equal(-1, list.Lowest);
            Assert.Equal(data.set.Length - 2, list.Highest);

            for (int i = 0; i < data.set.Length; ++i)
            {
                Assert.Equal(data.set[i], list[i - 1]);
            }
        }

        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void Clear((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            var list = new LinkedArray<int>(data.index, data.count);
            list.Clear();

            Assert.Equal(0, (int)list.Count);
            Assert.Equal(0, list.Lowest);
            Assert.Equal(0, list.Highest);
            Assert.Throws(typeof(IndexOutOfRangeException), () => list[data.index]);
        }

        [Fact]
        public void Contains()
        {
            var list = new LinkedArray<string>(10, "Test");
            Assert.True(list.Contains("Test"));
            Assert.False(list.Contains(""));
        }

        [Fact]
        public void IndexOf()
        {
            var list = new LinkedArray<string>(10, "Test");
            Assert.Equal(10, list.IndexOf("Test"));
            Assert.Equal(list.Lowest - 1, list.IndexOf(""));
        }

        [Theory]
        [ClassData(typeof(LinkedArrayComparisonSet))]
        public void Benchmarks(int[] data)
        {
            var lsw = new GPS.SimpleHelpers.Stopwatch.LoggingStopwatch();

            LinkedArray<int> linkedArray = null;
            List<int> list = null;
            Dictionary<int, int> dictionary = null;
            SortedDictionary<int, int> sortedDictionary = null;

            var set = new List<int>();
            var mixedSet = new List<int>();

            for (int i = 0; i < 1; ++i)
            {
                var positiveData = new int[data.Length];
                for (int j = 0; j < data.Length; ++j)
                {
                    data[j] = (i * j + 10) ^ data[j] ;
                    positiveData[j] = Math.Abs(data[j]);
                }

                set.AddRange(positiveData);
                mixedSet.AddRange(data);
            }

            _log.WriteLine("");

            for (int i = 0; i < 20; ++i)
            {
                lsw.Mark("Loading LinkedArray", () => linkedArray = new LinkedArray<int>(set));
                lsw.Mark("Loading List", () => list = new List<int>(set));
                lsw.Mark("Loading Dictionary", () => { dictionary = new Dictionary<int, int>(); for(int j=0; j<set.Count; ++j) dictionary[set[j]] = set[j]; });
                lsw.Mark("Loading SortedDictionary", () => { sortedDictionary = new SortedDictionary<int, int>(); for(int j=0; j<set.Count; ++j) sortedDictionary[set[j]] = set[j]; });

                lsw.Mark("Find Last LinkedArray", () => linkedArray.Contains(data.Last()));
                lsw.Mark("Find Last List", () => list.Contains(data.Last()));
                lsw.Mark("Find Last Dictionary", () => dictionary.ContainsValue(data.Last()));
                lsw.Mark("Find Last SortedDictionary", () => sortedDictionary.ContainsValue(data.Last()));

                linkedArray.Clear();
                list.Clear();
                dictionary.Clear();
                sortedDictionary.Clear();

                lsw.Mark("Positive Random Set LinkedArray", () => { for (int j = 0; j < mixedSet.Count; ++j) linkedArray[set[j]] = set[j]; });
                lsw.Mark("Positive Random Set Dictionary", () => { for (int j = 0; j < mixedSet.Count; ++j) dictionary[set[j]] = set[j]; });
                lsw.Mark("Positive Random Set SortedDictionary", () => { for (int j = 0; j < mixedSet.Count; ++j) sortedDictionary[set[j]] = set[j]; });

                if (i == 0)
                {
                    lsw.Mark($"Size of LinkedArray: {ObjectSize(linkedArray)}");
                    lsw.Mark($"Size of List: {ObjectSize(list)}");
                    lsw.Mark($"Size of Dictionary: {ObjectSize(dictionary)}");
                    lsw.Mark($"Size of SortedDictionary: {ObjectSize(sortedDictionary)}");
                }

                linkedArray.Clear();
                list.Clear();
                dictionary.Clear();
                sortedDictionary.Clear();

                lsw.Mark("Open Ended Random Set LinkedArray", () => { for (int j = 0; j < mixedSet.Count; ++j) linkedArray[mixedSet[j]] = mixedSet[j]; });
                lsw.Mark("Open Ended Random Set Dictionary", () => { for (int j = 0; j < mixedSet.Count; ++j) dictionary[mixedSet[j]] = mixedSet[j]; });
                lsw.Mark("Open Ended Random Set SortedDictionary", () => { for (int j = 0; j < mixedSet.Count; ++j) sortedDictionary[mixedSet[j]] = mixedSet[j]; });

                lsw.Mark("Enumerate LinkedArray", () => { foreach (var value in linkedArray) ; });
                lsw.Mark("Enumerate List", () => { foreach (var value in list) ; });
                lsw.Mark("Enumerate dictionary", () => { foreach (var value in dictionary) ; });
                lsw.Mark("Enumerate sortedDictionary", () => { foreach (var value in sortedDictionary) ; });

                linkedArray.Dispose();
                linkedArray = null;
                list = null;
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

    public class IntComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return y - x;
        }
    }
}


/*
 * (C) 2019 Your Legal Entity's Name
 */
