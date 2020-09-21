using System;
using System.Linq;
using GPS.Collections;
using Xunit;
using Xunit.Abstractions;

namespace GPS.Collections.Tests
{
    public class ArrayLink_Tests
    {
        readonly ITestOutputHelper _log;

        public ArrayLink_Tests(ITestOutputHelper log)
        {
            _log = log;
        }
        
        [Theory]
        [ClassData(typeof(IntDataSet))]
        public void InitializeArrayLink((int index, int size, int count, bool higher, bool lower) data)
        {
            _log.WriteLine($"{data}");

            using var link = new ArrayLink<int>(0, data.size);
            link[data.index] = (set: true, value: data.index);
                        
            var count = (link.Lower?.ArraySize ?? 0) + 
                link.ArraySize + 
                (link.Higher?.ArraySize ?? 0);
            
            Assert.Equal(data.size, link.Highest - link.Lowest + 1);
            Assert.Equal(data.index, link[data.index].value);
            Assert.Equal(data.index, link.Lowest);
            Assert.Equal(data.lower, link.Lower != null);
            Assert.Equal(data.higher, link.Higher != null);
            Assert.Equal(data.count, count);
        }
        
        [Theory]
        [ClassData(typeof(IntDataSet2))]
        public void AddSet((int[] set, int size, int count, bool higher, bool lower) data)
        {
             _log.WriteLine($"{data}");

            using var link = new ArrayLink<int>(0, ArrayLink<int>.InitialSize);
            foreach(var item in data.set) link[item] = (set: true, value: item);
                        
            var count = (link.Lower?.ArraySize ?? 0) + 
                link.ArraySize + 
                (link.Higher?.ArraySize ?? 0);

            Assert.Equal(data.size, link.Size);
            Assert.Equal(data.set.Min(i => i), link[link.Lowest].value);
            Assert.Equal(data.set.Max(i => i), link[link.Highest].value);
            Assert.Equal(data.set.Min(i => i), link.Lowest);
            Assert.Equal(data.set.Max(i => i), link.Highest);
            Assert.Equal(data.lower, link.Lower != null);
            Assert.Equal(data.higher, link.Higher != null);
            Assert.Equal(data.count, count);
        }
    }
}


/*
 * (C) 2019 Gateway Programming School, Inc.
 */
