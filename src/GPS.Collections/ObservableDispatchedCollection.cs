using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GPS.Collections
{
    public class ObservableDispatchedCollection<TDispatcher, TItem> :
        INotifyCollectionChanged, INotifyPropertyChanged,
        IList<TItem>, IReadOnlyList<TItem>, IList, IDisposable
        where TDispatcher : DispatcherProxy
    {
        private ObservableCollection<TItem> _collection;

        private ObservableCollection<TItem> Collection
        {
            get => _collection ?? (Collection = new ObservableCollection<TItem>());
            set
            {
                if (_collection == value) return;

                _collection = value;
                _collection.CollectionChanged += OnCollectionChanged;
                ((INotifyPropertyChanged)_collection).PropertyChanged += OnPropertyChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyEventQueue.Enqueue((sender, e));
            ProcessPropertyChangedQueue();
        }

        private void ProcessPropertyChangedQueue()
        {
            if (HoldEvents || !PropertyEventQueue.Any()) return;

            var success = PropertyEventQueue.TryDequeue(out var args);

            while (success)
            {
                foreach (var pair in PropertyChangedHandlers)
                {
                    var key = pair.Key;
                    var value = pair.Value;

                    key.dispatcher?.TryDispatchAsync(() =>
                        value?.Invoke(args.Item1, args.Item2));
                }
                success=PropertyEventQueue.TryDequeue(out args);
            }
        }

        // ReSharper disable once StaticMemberInGenericType
        [NonSerialized]
        private static TDispatcher _defaultDispatcher;
        private bool _disposed;

        public ObservableDispatchedCollection(TDispatcher dispatcher)
        {
            SyncRoot = new object();
            _defaultDispatcher = dispatcher;
            Collection = new ObservableCollection<TItem>();
        }

        public ObservableDispatchedCollection(IEnumerable<TItem> items, TDispatcher dispatcher = null)
            : this(dispatcher)
        {
            if (items != null)
            {
                Collection = new ObservableCollection<TItem>(items);
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionEventQueue.Enqueue(e);
            ProcessCollectionChangedQueue();
        }

        private void ProcessCollectionChangedQueue()
        {
            if (HoldEvents || !CollectionEventQueue.Any()) return;

            CollectionEventQueue.TryDequeue(out var args);

            while (args != null)
            {
                foreach (var pair in CollectionChangedHandlers)
                {
                    var key = pair.Key;
                    var value = pair.Value;

                    var dispatcher = key.dispatcher;

                    dispatcher?.TryDispatchAsync(() => value?.Invoke(this, args));
                }

                CollectionEventQueue.TryDequeue(out args);
            }
        }

        private ConcurrentDictionary<(TDispatcher dispatcher, NotifyCollectionChangedEventHandler handler), NotifyCollectionChangedEventHandler> CollectionChangedHandlers
        {
            get;
        } = new ConcurrentDictionary<(TDispatcher dispatcher, NotifyCollectionChangedEventHandler handler), NotifyCollectionChangedEventHandler>();

        private ConcurrentDictionary<(TDispatcher dispatcher, PropertyChangedEventHandler handler), PropertyChangedEventHandler> PropertyChangedHandlers
        {
            get;
        } = new ConcurrentDictionary<(TDispatcher dispatcher, PropertyChangedEventHandler handler), PropertyChangedEventHandler>();

        private static TDispatcher DefaultTDispatcher => _defaultDispatcher ??=
            DispatcherResolver?.Invoke();

        private static Func<TDispatcher> DispatcherResolver { get; set; }

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        public bool Remove(TItem item)
        {
            return _collection.Remove(item);
        }

        public void CopyTo(Array array, int index)
        {
            var typedArray = new TItem[array.Length];

            array.CopyTo(typedArray, 0);

            _collection.CopyTo(typedArray, index);

            typedArray.CopyTo(array, 0);
        }

        public int Count => Collection.Count;
        public bool IsSynchronized { get; } = true;
        public object SyncRoot { get; }

        public void Add(TItem item)
        {
            _collection.Add(item);
        }

        public int Add(object value)
        {
            _collection.Add((TItem)value);
            return _collection.IndexOf((TItem)value);
        }

        public void Clear()
        {
            Collection.Clear();
        }

        public bool Contains(object value)
        {
            return _collection.Contains((TItem)value);

        }

        public int IndexOf(object value)
        {
            return _collection.IndexOf((TItem)value);
        }

        public void Insert(int index, object value)
        {
            if (index > Count)
            {
                foreach (var i in Enumerable.Range(Count, index))
                {
                    _collection.Add(default);
                }
            }

            _collection.Add((TItem)value);
        }

        public void Remove(object value)
        {
            _collection.Remove((TItem)value);
        }

        void IList.RemoveAt(int index)
        {
            _collection.RemoveAt(index);
        }

        public bool IsFixedSize { get; } = false;

        public bool Contains(TItem item)
        {
            return _collection.Contains(item);
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly { get; } = false;

        object IList.this[int index]
        {
            get => _collection[index];
            set => _collection[index] = (TItem)value;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                TDispatcher dispatcherToRegister;

                try
                {
                    dispatcherToRegister = DispatcherResolver?.Invoke();
                }
                catch
                {
                    dispatcherToRegister = DefaultTDispatcher;
                }

                dispatcherToRegister ??= DefaultTDispatcher;

                var item = (dispatcherToRegister, value);
                if (!CollectionChangedHandlers.ContainsKey(item))
                {
                    CollectionChangedHandlers.TryAdd(item, value);
                }
            }

            remove
            {
                var dispatcherToRegister = DispatcherResolver?.Invoke() ?? DefaultTDispatcher;

                var item = (dispatcherToRegister ?? DefaultTDispatcher, value);
                if (CollectionChangedHandlers.ContainsKey(item))
                {
                    CollectionChangedHandlers.TryRemove(item, out var _);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                TDispatcher dispatcherToRegister;

                try
                {
                    dispatcherToRegister = DispatcherResolver?.Invoke();
                }
                catch
                {
                    dispatcherToRegister = DefaultTDispatcher;
                }

                var item = (dispatcherToRegister, value);
                if (!PropertyChangedHandlers.ContainsKey(item))
                {
                    PropertyChangedHandlers.TryAdd(item, value);
                }
            }

            remove
            {
                var dispatcherToRegister = DispatcherResolver?.Invoke() ?? DefaultTDispatcher;

                var item = (dispatcherToRegister ?? DefaultTDispatcher, value);
                if (PropertyChangedHandlers.ContainsKey(item))
                {
                    PropertyChangedHandlers.TryRemove(item, out var _);
                }
            }
        }

        public int IndexOf(TItem item)
        {
            return _collection.IndexOf(item);
        }

        public void Insert(int index, TItem item)
        {
            if (index > Count)
            {
                foreach (var i in Enumerable.Range(Count, index))
                {
                    _collection.Add(default);
                }
            }

            _collection.Add(item);
        }

        void IList<TItem>.RemoveAt(int index)
        {
            _collection.RemoveAt(index);
        }

        public TItem this[int index]
        {
            get => _collection[index > -1 ? index : 0];
            set
            {
                if (_collection.Count > index)
                    _collection[index > -1 ? index : 0] = value;
                else
                    _collection.Insert(index, value);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _collection.Clear();
            _collection = null;
        }

        private bool _holdEvents = false;
        private ConcurrentQueue<NotifyCollectionChangedEventArgs> CollectionEventQueue { get; } =
            new ConcurrentQueue<NotifyCollectionChangedEventArgs>();

        private ConcurrentQueue<(object,PropertyChangedEventArgs)> PropertyEventQueue { get; } =
            new ConcurrentQueue<(object,PropertyChangedEventArgs)>();

        // ReSharper disable once MemberCanBePrivate.Global
        public bool HoldEvents
        {
            get => _holdEvents;
            set
            {
                _holdEvents = value;

                if (!_holdEvents)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    ProcessCollectionChangedQueue();
                    LogThis?.Invoke($"ProcessCollectionChangedQueue took: {sw.Elapsed:G}");
                    sw.Restart();
                    ProcessPropertyChangedQueue();
                    LogThis?.Invoke($"ProcessPropertyChangedQueue took: {sw.Elapsed:G}");
                }
            }
        }

        public event Action<string> LogThis;

        public void AddRange(IEnumerable<TItem> items)
        {
            foreach (var item in items)
            {
                _collection.Add(item);
            }
        }

        public List<string> ConsolidateQueues()
        {
            var list = new List<string>();

            var count = CollectionEventQueue.Count;
            var items = new NotifyCollectionChangedEventArgs[count];
            var start = DateTime.Now;

            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < count; ++i)
            {
                if (CollectionEventQueue.TryDequeue(out var item))
                {
                    items[i] = item;
                }
                else
                {
                    break;
                }
            }
            list.Add($"Building items took: {sw.Elapsed:G}");

            var newItems = new List<object>();
            var oldItems = new List<object>();
            NotifyCollectionChangedEventArgs? last = null;
            
            foreach (var item in items)
            {
                if (last is null || last.Action != item.Action)
                {
                    if (last != null)
                    {
                        CollectionEventQueue.Enqueue(
                            last.Action == NotifyCollectionChangedAction.Replace
                                ? new NotifyCollectionChangedEventArgs(last.Action, newItems, oldItems)
                                : new NotifyCollectionChangedEventArgs(last.Action, newItems));
                    }

                    last = item;
                    newItems = new List<object>(last.NewItems?.Cast<object>() ?? new object[0]);
                    oldItems = new List<object>(last.OldItems?.Cast<object>() ?? new object[0]);
                }
                else
                {
                    foreach (var newItem in item.NewItems ?? new object[0])
                    {
                        newItems.Add(newItem);
                    }

                    foreach (var newItem in item.OldItems ?? new object[0])
                    {
                        oldItems.Add(newItem);
                    }
                }
            }

            if (last != null)
            {
                CollectionEventQueue.Enqueue(
                    last.Action == NotifyCollectionChangedAction.Replace
                        ? new NotifyCollectionChangedEventArgs(last.Action, newItems, oldItems)
                        : new NotifyCollectionChangedEventArgs(last.Action, newItems));
            }

            list.Add($"Consolidation from {items.Length} to {CollectionEventQueue.Count} took {sw.Elapsed:G}");

            count = PropertyEventQueue.Count;
            var propItems = new (object, PropertyChangedEventArgs)[count];
            for (var i = 0; i < count; ++i)
            {
                if (PropertyEventQueue.TryDequeue(out var item))
                {
                    propItems[i] = item;
                }
                else
                {
                    break;
                }
            }
            list.Add($"Building property items took: {sw.Elapsed:G}");

            sw.Reset();
            sw.Start();
            Dictionary<object, List<string>> changes = new Dictionary<object, List<string>>();
            foreach (var (item, args) in propItems)
            {
                if (changes.TryGetValue(item, out List<string> bucket))
                {
                    if (!bucket.Contains(args.PropertyName))
                    {
                        bucket.Add(args.PropertyName);
                    }
                }
                else
                {
                    changes.Add(item, new List<string> { args.PropertyName });
                }
            }

            var buildDictionaryElapsed = sw.Elapsed;
            list.Add($"Building property changes dictionary took: {buildDictionaryElapsed:G}");

            sw.Reset();
            sw.Start();
            foreach (var item in changes.Keys)
            {
                changes.TryGetValue(item, out var bucket);

                foreach (var propertyName in bucket)
                {
                    PropertyEventQueue.Enqueue((item, new PropertyChangedEventArgs(propertyName)));
                }
            }

            var enqueueElapsed = sw.Elapsed;
            list.Add($"Enqueueing {PropertyEventQueue.Count} property changes dictionary took: {enqueueElapsed:G}");

            return list;
        }
    }
}
