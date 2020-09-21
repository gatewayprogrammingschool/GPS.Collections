using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GPS.Collections
{
    public sealed class CollectionIndex<TEntity, TKeyValue, TDispatcher> :
        INotifyCollectionChanged,
        //IEnumerable<TEntity>,
        IEnumerable<(TKeyValue, IEnumerable<TEntity>)>
        where TDispatcher : DispatcherProxy
    {
        public event Action<string> LogThis;
        public ICollection<TEntity> SourceCollection { get; }
        public IEnumerable<TKeyValue> Keys => _index.Keys;

        private readonly DispatcherProxy _dispatcherProxy;
        private readonly Func<TEntity, (TKeyValue, TEntity)> _mapper;
        private readonly bool _unique;

        private readonly ConcurrentDictionary<TKeyValue, IndexBucket> _index =
            new ConcurrentDictionary<TKeyValue, IndexBucket>();

        private readonly ChangeNotifier _notifier;

        private CollectionIndex(DispatcherProxy dispatcherProxy)
        {
            _dispatcherProxy = dispatcherProxy ?? new DefaultDispatcher();
            _notifier = new ChangeNotifier(this, dispatcherProxy);
        }

        public CollectionIndex(
            ICollection<TEntity> sourceCollection,
            Func<TEntity, (TKeyValue, TEntity)> mapper,
            TDispatcher dispatcher = null,
            bool unique = false)
            : this(dispatcher)
        {
            SourceCollection = sourceCollection;
            _mapper = mapper;
            _unique = unique;

            var mapped = sourceCollection.Select(mapper).ToList();

            mapped.ForEach(item => TryAddInternal(item));

            switch (SourceCollection)
            {
                case ObservableCollection<TEntity> observable:
                    observable.CollectionChanged += ObservableOnCollectionChanged;
                    break;
                case ObservableDispatchedCollection<TDispatcher, TEntity> dispatched:
                    dispatched.CollectionChanged += ObservableOnCollectionChanged;
                    break;
            }
        }

        public IEnumerable<TEntity> this[TKeyValue key] =>
            _index.TryGetValue(key, out var value)
                ? value.AsEnumerable()
                : null;

        private void ObservableOnCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.Cast<TEntity>())
                    {
                        if (item is null) continue;

                        TryAddOrUpdateEntity(item, out _);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.Cast<TEntity>())
                    {
                        if (item is null) continue;

                        TryRemove(item, out var removed);
                    }

                    break;
            }
        }

        private bool TryAddInternal((TKeyValue key, TEntity entity) tuple)
        {
            if (_unique && ContainsEntity(tuple.entity))
                throw new UniquenessViolationException<TEntity, TKeyValue, TDispatcher>(
                    this, tuple.key, tuple.entity);

            _index.TryGetValue(tuple.key, out var bucket);

            if (bucket is null)
            {
                bucket = new IndexBucket(tuple.key, tuple.entity);

                bucket.LogThis += s => LogThis?.Invoke(s);

                if (!_index.TryAdd(tuple.key, bucket)) return false;

                _notifier.NotifyAdd(tuple);

                return true;
            }
            else
            {
                bucket.Add(tuple.entity);

                _notifier.NotifyAdd(tuple);

                return true;
            }

            return false;
        }


        private class ChangeNotifier
        {
            private readonly CollectionIndex<TEntity, TKeyValue, TDispatcher> _parent;
            private readonly DispatcherProxy _dispatcherProxy;

            public ChangeNotifier(
                CollectionIndex<TEntity, TKeyValue, TDispatcher> parent,
                DispatcherProxy dispatcherProxy)
            {
                _parent = parent;
                _dispatcherProxy = dispatcherProxy;
            }

            public void NotifyAdd((TKeyValue key, TEntity entity) tuple)
            {
                if (_dispatcherProxy != null)
                {
                    _dispatcherProxy.TryDispatchAsync(() =>
                        _parent.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add, new[] { tuple.Item2 })));
                }
                else
                {
                    _parent.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, new[] { tuple.Item2 }));
                }
            }
            public void NotifyRemove((TKeyValue key, TEntity entity) tuple)
            {
                if (_dispatcherProxy != null)
                {
                    _dispatcherProxy.TryDispatchAsync(() =>
                        _parent.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove, new[] { tuple.Item2 })));
                }
                else
                {
                    _parent.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, new[] { tuple.Item2 }));
                }
            }

            public void NotifyUpdated(List<TEntity> replacements, TEntity newEntity)
            {
                if (!replacements.Any()) return;

                _parent.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    new List<TEntity>(new[] { newEntity }), replacements));
            }
        }

        public bool TryAddEntity(TEntity entity)
        {
            var (keyValue, _) = _mapper(entity);

            (TKeyValue, TEntity) tuple = (keyValue, entity);

            return TryAddInternal(tuple);
        }

        public bool TryRemove(TEntity toRemoveEntity, out TEntity removedEntity)
        {
            var (newKey, _) = _mapper(toRemoveEntity);

            if (_index.TryGetValue(newKey, out var bucket))
            {
                var entities = bucket.ToList();

                if (bucket.Remove(toRemoveEntity))
                {
                    if (!bucket.Any())
                    {
                        _index.TryRemove(newKey, out _);
                    }

                    removedEntity = toRemoveEntity;
                    _notifier.NotifyRemove((newKey, toRemoveEntity));
                    return true;
                }
            }

            removedEntity = default;
            return false;
        }

        public bool ContainsEntity(TEntity entity)
        {
            var keys = Keys.ToArray();
            var keysLength = keys.Length;

            for(var i=0; i < keysLength; ++i)
            {
                var key = keys[i];
                var bucket = _index[key];
                var bucketCount = bucket.Count;

                for(var j = 0; j < bucketCount; ++j)
                {
                    if (entity.Equals(bucket[j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryAddOrUpdateEntity(TEntity newEntity, out List<TEntity> oldEntities)
        {
            if (!ContainsEntity(newEntity))
            {
                oldEntities = new List<TEntity>();
                return TryAddInternal(_mapper(newEntity));
            }

            var (newKey, _) = _mapper(newEntity);

            if (_index.TryGetValue(newKey, out var bkt))
            {
                var exsting = bkt
                    .Where(entity => (object)entity != (object)newEntity &&
                                            entity.Equals(newEntity)).ToArray();

                if (exsting.Length > 0)
                {
                    foreach (var item in exsting)
                    {
                        bkt.Remove(item);
                    }

                    oldEntities = exsting.ToList();
                    return TryAddInternal(_mapper(newEntity));
                }
            }

            oldEntities = new List<TEntity>();

            var existing = _index.Values
                .SelectMany(val =>
                    val.Where(entity => (object)entity != (object)newEntity &&
                                        entity.Equals(newEntity))
                        .Select(item => (val, item))).ToList();

            if (!existing.Any())
            {
                TryAddInternal(_mapper(newEntity));
                return true;
            }

            foreach (var (bucket, item) in existing)
            {
                if (!bucket.Remove(item)) continue;

                bucket.Add(newEntity);
                oldEntities.Add(item);
            }

            if (oldEntities.Count > 0)
            {
                _notifier.NotifyUpdated(oldEntities, newEntity);
                return true;
            }


            return false;
        }

        private class IndexBucket : //IList<TEntity>
            IEnumerable<TEntity>
        {
            public event Action<string> LogThis;
            public TKeyValue Key { get; }
            //private SortedSet<TEntity> _values;

            private int _count;

            private List<TEntity[]> _list = new List<TEntity[]>();

            private int _pageSize = 500;
            private int _currentPage = 0;

            public IndexBucket(TKeyValue key, IEnumerable<TEntity> values)
            {
                Key = key;

                _list.Add(new TEntity[_pageSize]);
                _count = 0;
                _currentPage = 0;

                foreach (var item in values)
                {
                    var index = _count - (_currentPage * _pageSize);
                    _list[_currentPage][index] = item;
                    _count++;

                    if (Count % _pageSize != 0) continue;
                    _list.Add(new TEntity[_pageSize]);
                    _currentPage++;
                }
            }

            public IndexBucket(TKeyValue key, TEntity value)
            {
                Key = key;

                _list.Add(new TEntity[_pageSize]);
                _currentPage = 0;

                _list[0][0] = value;
                _count = 1;
            }

            public IEnumerator<TEntity> GetEnumerator()
            {
                for (var i = 0; i < _count; ++i)
                {
                    if (_list[i / _pageSize][i % _pageSize]?.Equals(default(TEntity)) ?? false) continue;

                    yield return _list[i / _pageSize][i % _pageSize];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(TEntity item)
            {
                var index = _count - (_currentPage * _pageSize);
                _list[_currentPage][index] = item;
                _count++;

                if (Count % _pageSize != 0) return;
                _list.Add(new TEntity[_pageSize]);
                _currentPage++;
            }

            public void Clear()
            {
                _list.Clear();
            }

            public bool Contains(TEntity item)
            {
                return this.Any(entity => entity?.Equals(item) ?? false);
            }

            public bool Remove(TEntity item)
            {
                for (var i = 0; i < _count; ++i)
                {
                    var current = _list[i / _pageSize][i % _pageSize];

                    if (!(current?.Equals(item) ?? false)) continue;

                    _count--;

                    _list[i / _pageSize][i % _pageSize] = default;

                    return true;
                }

                return false;
            }

            public int Count
            {
                get => _count;
                set => _count = value;
            }

            //public bool IsReadOnly => false;

            public int IndexOf(TEntity item)
            {
                for (var i = 0; i < _count; ++i)
                {
                    var current = _list[i / _pageSize][i % _pageSize];

                    if ((current?.Equals(item) ?? false)) return i;
                }

                return -1;
            }

            //public void Insert(int index, TEntity item)
            //{
            //    _values.Insert(index, item);

            //    //if (LogThis != null && _values.Count % 100 == 0)
            //    //{
            //    //    LogThis($"Added {_values.Count} items.");
            //    //}
            //}

            //public void RemoveAt(int index)
            //{
            //    //_values.RemoveAt(index);
            //    throw new NotImplementedException();
            //}

            public TEntity this[int index]
            {
                get => _list[index / _pageSize][index % _pageSize];
                set => _list[index / _pageSize][index % _pageSize] = value;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        IEnumerator<(TKeyValue, IEnumerable<TEntity>)> IEnumerable<(TKeyValue, IEnumerable<TEntity>)>.GetEnumerator()
        {
            return _index
                .Select(item
                    => (item.Key, item.Value.AsEnumerable())
                )
                .GetEnumerator();
        }

        //public IEnumerator GetEnumerator()
        //{
        //    return _index.Values.SelectMany(item => item).GetEnumerator();
        //}
        public IEnumerator GetEnumerator()
        {
            return _index
                .Select(item
                    => (item.Key, item.Value.AsEnumerable())
                )
                .GetEnumerator();
        }
    }

    internal abstract class UniquenessViolationExceptionBase : Exception
    {
        protected UniquenessViolationExceptionBase(string message) : base(message) { }
    }

    internal class UniquenessViolationException<TEntity, TKeyValue, TDispatcher> :
        UniquenessViolationExceptionBase
        where TDispatcher : DispatcherProxy
    {
        public CollectionIndex<TEntity, TKeyValue, TDispatcher> CollectionIndex { get; }
        public TKeyValue TupleKey { get; }
        public TEntity Entity { get; }

        public UniquenessViolationException(
            CollectionIndex<TEntity, TKeyValue, TDispatcher> collectionIndex,
            TKeyValue key,
            TEntity entity) : base($"Uniqueness Violation on {typeof(TDispatcher).Name} value of {key}")
        {
            CollectionIndex = collectionIndex;
            TupleKey = key;
            Entity = entity;
        }
    }

    public class DefaultDispatcher : DispatcherProxy
    {
        public override Task<bool> TryDispatchAsync(Action action)
        {
            try
            {
                action?.Invoke();

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return Task.FromException<bool>(ex);
            }
        }
    }
}