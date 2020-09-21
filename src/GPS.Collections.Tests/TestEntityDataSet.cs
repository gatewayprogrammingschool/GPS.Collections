using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GPS.RandomDataGenerator;
using GPS.RandomDataGenerator.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace GPS.Collections.Tests.Tests
{
    public class TestEntityDataSet : IEnumerable<object[]>
    {
        private readonly int _size;

        static TestEntityDataSet()
        {
            var collection = new ServiceCollection();
            collection.AddGenerators();
            _services = collection.BuildServiceProvider();
        }

        public TestEntityDataSet(int size = 500)
        {
            _size = size;
        }

        private NameGenerator _generator = _services.GetService<NameGenerator>();
        
        private object[] _data => _generator.Generate(new Random(0), _size)
            .Select(s => new TestEntity(s))
            .ToArray();

        public IEnumerable<TestEntity> DataSet => _data.Cast<TestEntity>();

        private static ServiceProvider _services;

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var item in _data) yield return new object[] { item };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

    }
}