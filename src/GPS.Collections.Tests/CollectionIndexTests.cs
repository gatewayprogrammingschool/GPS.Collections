using Xunit;
using GPS.Collections.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xunit.Abstractions;

namespace GPS.Collections.Tests.Tests
{
    public class CollectionIndexTests
    {
        static ITestOutputHelper _log;
        private TestDispatcher _dispatcher;
        private TestDispatcher Dispatcher => _dispatcher ??= new TestDispatcher();

        [Serializable]
        public class TestDispatcher : DispatcherProxy
        {
            public override Task<bool> TryDispatchAsync(Action action)
            {
                action?.Invoke();

                return Task.FromResult(true);
            }
        }

        public CollectionIndexTests(ITestOutputHelper log)
        {
            _log = log;
        }

        private (ObservableDispatchedCollection<TestDispatcher, TestEntity> collection,
            CollectionIndex<TestEntity, string, TestDispatcher> index)
            CreateIndex(Func<TestEntity, (string, TestEntity)> mapper,
                IEnumerable<TestEntity> data = null)
        {
            TestCollection = new ObservableDispatchedCollection<TestDispatcher, TestEntity>(Dispatcher);

            TestCollection.LogThis += s => _log.WriteLine(s);

            TestIndex = new CollectionIndex<TestEntity, string, TestDispatcher>(
                TestCollection,
                mapper,
                Dispatcher);

            Assert.NotNull(TestIndex);
            Assert.NotNull(TestIndex.SourceCollection);

            var result = (TestCollection, TestIndex);

            if (data is null || !data.Any()) return result;

            var sw = new Stopwatch();
            _log.WriteLine("Adding Data");
            TestCollection.HoldEvents = true;

            sw.Start();
            TestCollection.AddRange(data);
            var elapse = sw.Elapsed;
            _log.WriteLine($"Added {TestCollection.Count} Rows in {elapse:G}");

            sw.Reset();
            sw.Start();
            var timings = TestCollection.ConsolidateQueues();
            elapse = sw.Elapsed;
            timings.ForEach(_log.WriteLine);
            _log.WriteLine($"Consolidated Events Took {elapse:G}");

            sw.Reset();
            sw.Start();
            TestCollection.HoldEvents = false;
            elapse = sw.Elapsed;
            _log.WriteLine($"Dispatching Events Took {elapse:G}");

            return result;
        }

        public CollectionIndex<TestEntity, string, TestDispatcher> TestIndex { get; set; }

        public ObservableDispatchedCollection<TestDispatcher, TestEntity> TestCollection { get; set; }

        [Fact]
        public void CollectionIndexTest()
        {
            CreateIndex(s => (s.Name, s));
        }

        [Fact()]
        public void AddEntityTest()
        {
            NotifyCollectionChangedAction expectedAction = default;
            TestEntity expectedEntity = default;

            var (collection, index) = CreateIndex(s => (s.Name, s));

            expectedAction = NotifyCollectionChangedAction.Add;
            expectedEntity = new TestEntity {Name = nameof(AddEntityTest)};

            var action = expectedAction;
            var entity = expectedEntity;
            var mre = new ManualResetEvent(false);

            index.CollectionChanged += CollectionChanged;

            collection.Add(expectedEntity);

            if (!mre.WaitOne(TimeSpan.FromMilliseconds(500)))
            {
                Assert.True(false, "Timeout waiting for event.");
            }

            Assert.Contains(expectedEntity, index.SourceCollection);

            _log.WriteLine("All Tests passed.");

            var retrieved = index[nameof(AddEntityTest)];

            _log.WriteLine(retrieved.FirstOrDefault()?.ToString());

            _log.WriteLine("");

            void CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
            {
                _log.WriteLine("In CollectionChanged.");
                Assert.Equal(action, args.Action);

                Assert.Contains(entity,
                    args.Action != NotifyCollectionChangedAction.Remove
                        ? args.NewItems.Cast<TestEntity>()
                        : args.OldItems.Cast<TestEntity>());

                _log.WriteLine("Leaving CollectionChanged.");
                mre.Set();
            }
        }

        [Fact()]
        public void TryRemoveEntityTest()
        {
            NotifyCollectionChangedAction expectedAction = default;
            TestEntity expectedEntity = default;

            var (collection, index) = CreateIndex(s => (s.Name, s));

            expectedAction = NotifyCollectionChangedAction.Remove;
            expectedEntity = new TestEntity {Name = nameof(TryRemoveEntityTest)};

            var action = expectedAction;
            var entity = expectedEntity;
            var mre = new ManualResetEvent(false);

            collection.Add(expectedEntity);

            var retrieved = index[nameof(TryRemoveEntityTest)];

            index.CollectionChanged += CollectionChanged;

            Assert.True(index.TryRemove(expectedEntity, out var results));

            if (!mre.WaitOne(TimeSpan.FromMilliseconds(500)))
            {
                Assert.True(false, "Timeout waiting for event.");
            }

            Assert.NotNull(results);

            _log.WriteLine(results.ToString());

            retrieved = null;

            retrieved = index[nameof(TryRemoveEntityTest)];

            Assert.Null(retrieved);

            var indexContents = index.AsEnumerable().SelectMany(pair => pair.Item2);

            Assert.DoesNotContain(expectedEntity, indexContents);

            _log.WriteLine("All Tests passed.");

            void CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
            {
                _log.WriteLine("In CollectionChanged.");
                Assert.Equal(action, args.Action);

                Assert.Contains(entity,
                    args.Action != NotifyCollectionChangedAction.Remove
                        ? args.NewItems.Cast<TestEntity>()
                        : args.OldItems.Cast<TestEntity>());

                _log.WriteLine("Leaving CollectionChanged.");
                mre.Set();
            }
        }


        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        [InlineData(5000)]
        [InlineData(50000)]
        //[InlineData(500000)]
        public void TryAddMultipleItemsTest(int itemCount)
        {
            NotifyCollectionChangedAction expectedAction = default;
            TestEntity expectedEntity = default;
            var testData = new TestEntityDataSet(itemCount).DataSet;

            var (collection, index) = CreateIndex(s => (s.Surname, s));

            index.LogThis += s => _log.WriteLine(s);

            collection.AddRange(testData);

            Assert.Equal(testData.Count(), index.SourceCollection.Count);

            var sw = new Stopwatch();
            _log.WriteLine("Indices:");
            _log.WriteLine("====================================");
            sw.Start();

            var indexSearchTime = TimeSpan.Zero;
            var collectionSearchTime = TimeSpan.Zero;

            foreach ((string, IEnumerable<TestEntity>) keySet in index)
            {
                var (key, values) = keySet;

                //_log.WriteLine($"\tKey: {key}");
                sw.Reset();
                sw.Start();
                var entities = index[key];
                var indexedElapsed = sw.Elapsed;
                //_log.WriteLine($"\tRetrieve from Index: {indexedElapsed:G} ticks");
                sw.Reset();
                sw.Start();
                entities = collection.Where(entity => entity.Surname == key);
                var collectionElapsed = sw.Elapsed;
                sw.Stop();
                //_log.WriteLine($"\tRetrieve from Collection: {collectionElapsed:G} ticks");
                //_log.WriteLine("\t===============================");
                //foreach (var testEntity in values)
                //{
                //    _log.WriteLine($"\t{testEntity}");
                //}
                //_log.WriteLine("");
                indexSearchTime += indexedElapsed;
                collectionSearchTime += collectionElapsed;
            }

            var diff = collectionSearchTime - indexSearchTime;
            _log.WriteLine($"Indexed Search Time   : {indexSearchTime:G}");
            _log.WriteLine($"Collection Search Time: {collectionSearchTime:G}");
            _log.WriteLine($"Diff: {diff.TotalMilliseconds:N4}");
        }

        [Fact]
        public void UniquenessTest()
        {
            var (collection, index) = CreateIndex(s => (s.Surname, s));

            var uniqueIndex = new CollectionIndex<TestEntity, Guid, TestDispatcher>(
                collection,
                entity => (entity.KeyField, entity),
                _dispatcher,
                true
            );

            var testEntity = new TestEntity("John Doe");

            collection.Add(testEntity);

            Assert.Contains(testEntity, collection);
            Assert.Contains(testEntity, uniqueIndex.SelectMany(tuple => tuple.Item2));

            Assert.ThrowsAny<UniquenessViolationExceptionBase>(
                () => collection.Add(testEntity));
        }
    }
}