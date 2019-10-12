using System.Threading;
/*
    # GPS.Collections

    ## OrderedConcurrentDictionary.cs

    Data structure that comprises an implementation of
    IDictionary<TKey, TValue> that is backed by the ConcurrentDictionary
    and ConcurrentQueue objects.

    ## Copyright

    2019 - Gateway Programming School, Inc.

    This notice must be retained for any use the code
    herein in whole or in part for any use.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GPS.Collections
{
    /// <summary>
    /// Collection of generic key-value pairs that
    /// uses a ConcurrentDictionary&lt;TKey, TValue&gt;
    /// along with a ConcurrentQueue&lt;TKey&gt;
    /// instances holding the values of the collection.
    /// </summary>
    /// <remarks>
    /// Accessing the data randomly is performed by pulling from the
    /// ConcurrentDictionary and acessing the data sequentially is
    /// performed by pulling from the ConcurrentQueue.
    /// </remarks>
    /// <typeparam name="TKey">Type of the Key</typeparam>
    /// <typeparam name="TValue">Type of the Value</typeparam>
    public class OrderedConcurrentDictionary<TKey, TValue> :
          ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>
        , IDictionary<TKey, TValue>
        , IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>
        , IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>
        , IReadOnlyDictionary<TKey, TValue>
        , IDictionary
    {
        private readonly object _lock = new object();

        private ConcurrentQueue<TKey> _queue = new ConcurrentQueue<TKey>();

        private IDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();

        /// <summary>
        /// EqualityComparer used for match keys.
        /// </summary>
        public IEqualityComparer<TKey> KeyEqualityComparer { get; }

        /// <summary>
        /// Default Implementation of EqualityComparer&lt;TKey&gt;
        /// </summary>
        public class KeyEqualityComparerImpl : EqualityComparer<TKey>
        {
            /// <inheritdocs />
            public override bool Equals(TKey x, TKey y)
            {
                return EqualityComparer<TKey>.Default.Equals(x, y);
            }

            /// <inheritdocs />
            public override int GetHashCode(TKey obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Default constructor that initializes an empty collection.
        /// </summary>
        public OrderedConcurrentDictionary(IEqualityComparer<TKey> comparer = null)
        {
            KeyEqualityComparer = comparer ?? new KeyEqualityComparerImpl();
        }

        /// <summary>
        /// Constructor that initializes the collection with the supplied dictionary.
        /// </summary>
        /// <param name="data">IDictionary&lt;TKey, TValue&gt; containing the initial
        /// data for the collection.</param>
        public OrderedConcurrentDictionary(IDictionary<TKey, TValue> data
            , IEqualityComparer<TKey> comparer = null) : this(comparer)
        {
            _queue = new ConcurrentQueue<TKey>(data.Select(p => p.Key));

            _dictionary = new ConcurrentDictionary<TKey, TValue>(data);
        }


        /// <summary>
        /// Constructor that initializes the collection with the supplied collection.
        /// </summary>
        /// <param name="data">IEnumerable&lt;(TKey, TValue)&gt; containing the initial
        /// data for the collection.</param>
        public OrderedConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> data
            , IEqualityComparer<TKey> comparer = null)
            : this(new Dictionary<TKey, TValue>(data)
                    , comparer)
        {
        }

        /// <summary>
        /// Public indexer that retrieves data directly from the
        /// underlying ConcurrentDictionary.
        /// </summary>
        /// <value>TValue value for the supplied key.</value>
        /// <exception member="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if the requested Key does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                _dictionary[key] = value;
            }
        }

        /// <summary>
        /// Untyped public indexer that retrieves data directly from the
        /// underlying ConcurrentDictionary.
        /// </summary>
        /// <value>Value for the supplied key.</value>
        /// <exception member="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if the requested Key does not exist in the collection.</exception>
        public object this[object key]
        {
            get => this[(TKey)key];
            set => this[(TKey)key] = AddOrUpdate(key, value, (k, v) => value);
        }



        /// <summary>
        /// Keys in the collection.
        /// </summary>
        public ICollection<TKey> Keys => _queue.ToList();

        /// <summary>
        /// Values in the collection.
        /// </summary>
        public ICollection<TValue> Values => _dictionary.Values;

        /// <summary>
        /// Number of items in the collection.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Is the collection Reay Only?
        /// </summary>
        public bool IsReadOnly => false;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public bool IsFixedSize => false;

        ICollection IDictionary.Keys => Keys.ToArray();

        ICollection IDictionary.Values => Values.ToArray();

        public bool IsSynchronized => true;

        public object SyncRoot { get; } = SynchronizationContext.Current;

        /// <summary>
        /// Add a key-value pair to the collection.
        /// </summary>
        /// <param name="key">Key of the pair.</param>
        /// <param name="value">Value of the pair.</param>
        public void Add(TKey key, TValue value)
        {
            TryAdd(key, value);
        }

        /// <summary>
        /// Attempts to add the data to the collection.  Does not
        /// over-write existing data.
        /// </summary>
        /// <param name="key">Key of the data</param>
        /// <param name="value">Value of the data</param>
        /// <returns>Returns true if the data was added, 
        /// false if the key was already present.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (!_dictionary.ContainsKey(key))
                {
                    _queue.Enqueue(key);
                    _dictionary.TryAdd(key, value);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Adds or Updates the data in the collection.
        /// </summary>
        /// <param name="key">Key of the data.</param>
        /// <param name="addValueFactory">Factory to generate data on add.</param>
        /// <param name="updateValueFactory">Factory to generate the data on update.</param>
        /// <returns>The value added or updated in the collection.</returns>
        public TValue AddOrUpdate(
            TKey key
            , Func<TKey, TValue> addValueFactory
            , Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (_lock)
            {
                var value = addValueFactory(key);
                if (!_dictionary.ContainsKey(key))
                {
                    _queue.Enqueue(key);
                    _dictionary.TryAdd(key, value);
                }

                else
                {
                    value = updateValueFactory(key, addValueFactory(key));

                    _dictionary[key] = value;
                }

                return value;
            }
        }

        /// <summary>
        /// Adds or Updates the data in the collection.
        /// </summary>
        /// <param name="key">Key of the data.</param>
        /// <param name="addValue">Value of the data on add.</param>
        /// <param name="updateValueFactory">Factory to generate the data on update.</param>
        /// <returns>The value added or updated in the collection.</returns>
        public TValue AddOrUpdate(
            TKey key
            , TValue addValue
            , Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (_lock)
            {
                var value = addValue;
                if (!_dictionary.ContainsKey(key))
                {
                    _queue.Enqueue(key);
                    _dictionary.TryAdd(key, value);
                }

                else
                {
                    value = updateValueFactory(key, value);

                    _dictionary[key] = value;
                }

                return value;
            }
        }

        /// <summary>
        /// Adds or Updates the data in the collection.
        /// </summary>
        /// <param name="key">Key of the data.</param>
        /// <param name="addValueFactory">Factory to generate data on add.</param>
        /// <param name="updateValueFactory">Factory to generate the data on update.</param>
        /// <param name="factoryArgument">Argument to pass to factory functions.</param>
        /// <returns>The value added or updated in the collection.</returns>
        public TValue AddOrUpdate<TArg>(
            TKey key
            , Func<TKey, TArg, TValue> addValueFactory
            , Func<TKey, TValue, TArg, TValue> updateValueFactory
            , TArg factoryArgument)
        {
            lock (_lock)
            {
                var value = addValueFactory(key, factoryArgument);

                if (!_dictionary.ContainsKey(key))
                {

                    _queue.Enqueue(key);
                    _dictionary.TryAdd(key, value);
                }

                else
                {
                    value = updateValueFactory(key, value, factoryArgument);

                    _dictionary[key] = value;
                }

                return value;
            }
        }

        /// <summary>
        /// Adds the KeyValuePair to the collection.
        /// </summary>
        /// <param name="item">Data to add to the collection.</param>
        /// <remarks>The KeyValuePair struct is not preserved.false</remarks>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Resets the collection to an empty state.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _dictionary = new ConcurrentDictionary<TKey, TValue>();
                _queue = new ConcurrentQueue<TKey>();
            }
        }

        /// <summary>
        /// Tests if the collection contains the specified 
        /// KeyValuePair struct.
        /// </summary>
        /// <param name="item">Data to search for.</param>
        /// <returns>True if the data is found in the collection.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        /// <summary>
        /// Tests if the key is found in the collection.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <returns>True if the key is found in the collection.</returns>
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Copies the data of the collection from the ConcurrentQueue
        /// into the provided array beginning at the specified index.
        /// </summary>
        /// <param name="array">Target array.</param>
        /// <param name="arrayIndex">Beginning index to copy to.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (_lock)
            {
                var arr = new KeyValuePair<TKey, TValue>[Count];

                var enumerator = GetEnumerator();

                var index = 0;
                while (enumerator.MoveNext())
                {
                    arr[index++] = enumerator.Current;
                }

                var pairs = arr.ToList();
                pairs.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Get an instance of an IEnumerator&lt;KeyValuePair(TKey, TValue)&gt;
        /// for the data in the collection.
        /// </summary>
        /// <returns>Data of the collection with order preserved from the 
        /// ConcurrentQueue.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (_lock)
            {
                return _queue.Select(t => KeyValuePair.Create(t, _dictionary[t])).GetEnumerator();
            }
        }

        /// <summary>
        /// Removes data from the collection.
        /// </summary>
        /// <param name="key">Key of the data to remove.</param>
        /// <returns>True if the data was present and has been removed.</returns>
        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (ContainsKey(key))
                {
                    var list = _queue.ToList();
                    list.Remove(list.FirstOrDefault(t => KeyEqualityComparer.Equals(t, key)));
                    _queue = new ConcurrentQueue<TKey>(list);
                    _dictionary.Remove(key);

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Removes data from the collection.
        /// </summary>
        /// <param name="item">KeyValuePair of the data to remove.</param>
        /// <returns>True if the data was present and has been removed.</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        /// <summary>
        /// Retrieves the data directly from the underlying
        /// ConcurrentDictionary.
        /// </summary>
        /// <param name="key">Key of the data to return.</param>
        /// <param name="value">Value of the data to return.</param>
        /// <returns>True if the Key is present and the data is returned.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Get an instance of an IEnumerator
        /// for the data in the collection.
        /// </summary>
        /// <returns>Data of the collection with order preserved from the 
        /// ConcurrentQueue.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Reorders the data in the collection using a default selector on the Key
        /// and ReorderDirection specified.
        /// </summary>
        /// <param name="direction">ReorderDirection specifying the direction
        /// to sort the data.</param>
        public void Reorder(ReorderDirection direction = ReorderDirection.Ascending)
        {
            var selector = new Func<TKey, TKey>(key => key);
            Reorder(selector, direction);
        }

        /// <summary>
        /// Reorders the data in the collection according the supplied selector
        /// and ReorderDirection specified.
        /// </summary>
        /// <param name="selector">Func of the selector.</param>
        /// <param name="direction">ReorderDirection specifying the direction
        /// to sort the data.</param>
        public void Reorder(Func<TKey, TKey> selector
            , ReorderDirection direction = ReorderDirection.Ascending)
        {
            var ordered = direction == ReorderDirection.Ascending ? _queue.OrderBy(selector) : _queue.OrderByDescending(selector);

            _queue = new ConcurrentQueue<TKey>(ordered);
        }

        /// <summary>
        /// Reorders the data in the collection according the supplied selector,
        /// IComparer and ReorderDirection specified.
        /// </summary>
        /// <param name="selector">Func of the selector.</param>
        /// <param name="comparer">IComparer instance that performs the test
        /// to determine the ordinality of two datum in the collection.</comparer>
        /// <param name="direction">ReorderDirection specifying the direction
        /// to sort the data.</param>
        public void Reorder(Func<TKey, TKey> selector
            , IComparer<TKey> comparer
            , ReorderDirection direction = ReorderDirection.Ascending)
        {
            var ordered = direction == ReorderDirection.Ascending ? _queue.OrderBy(selector, comparer) : _queue.OrderByDescending(selector, comparer);

            _queue = new ConcurrentQueue<TKey>(ordered);
        }

        /// <summary>
        /// Untyped Add.
        /// </summary>
        /// <param name="key">Key of data</param>
        /// <param name="value">Value of data</param>
        public void Add(object key, object value)
        {
            if (key is TKey k && value is TValue v)
            {
                Add(k, v);
            }
            else
            {
                throw new ArgumentException("key or value is not a supported type.", nameof(key));
            }
        }

        /// <summary>
        /// Untyped Contains
        /// </summary>
        /// <param name="key">Key of data to find.</param>
        /// <returns>True if data exists in the collection.</returns>
        public bool Contains(object key)
        {
            if (key is TKey k)
            {
                return Contains(k);
            }
            else
            {
                throw new ArgumentException("key is not a supported type.", nameof(key));
            }
        }

        /// <summary>
        /// Returns ConcurrentDictionaryEnumerator for the collection.
        /// </summary>
        /// <returns>ConcurrentDictionaryEnumerator with snapshot of the data at the point in time
        /// of calling the method.</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            lock (_lock)
            {
                IReadOnlyDictionary<TKey, TValue> dictionary = null;

                var values = new KeyValuePair<TKey, TValue>[Count];
                CopyTo(values, 0);
                dictionary = new OrderedConcurrentDictionary<TKey, TValue>(values);

                return new ConcurrentDictionaryEnumerator(dictionary);
            }
        }

        /// <inheritdocs />
        public class ConcurrentDictionaryEnumerator : IDictionaryEnumerator
        {
            readonly IReadOnlyDictionary<TKey, TValue> _values = null;
            readonly Queue<TKey> _keys = null;
            private TKey _k;

            /// <inheritdocs />
            public DictionaryEntry Entry => new DictionaryEntry(Key, _values[K]);

            /// <inheritdocs />
            public object Key { get => _k; set => K = (TKey)value; }

            /// <inheritdocs />
            private TKey K { get => _k; set => _k = value; }

            /// <inheritdocs />
            public object Value => _values[K];

            /// <inheritdocs />
            public object Current { get => K; set { } }

            public ConcurrentDictionaryEnumerator(IReadOnlyDictionary<TKey, TValue> dictionary)
            {
                _values = dictionary;
                _keys = new Queue<TKey>(dictionary.Keys);
                K = default;
            }

            /// <inheritdocs />
            public bool MoveNext()
            {
                return _keys.TryDequeue(out _k);
            }

            /// <inheritdocs />
            public void Reset()
            {
                _keys.Clear();
                _values.Keys.ToList().ForEach(k => _keys.Enqueue(k));
                K = default;
            }
        }

        /// <summary>
        /// Untyped Remove
        /// </summary>
        /// <param name="key">Key of data to remove.</param>
        public void Remove(object key)
        {
            if (key is TKey k)
            {
                Remove(k);
            }
            else
            {
                throw new ArgumentException("key is not a supported type.", nameof(key));
            }
        }

        /// <summary>
        /// Untyped CopyTo
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="index">Starting index</param>
        public void CopyTo(Array array, int index)
        {
            var kvpType = typeof(KeyValuePair<TKey, TValue>);

            if (array.GetType().GetElementType() == kvpType)
            {
                CopyTo((KeyValuePair<TKey, TValue>[])array, index);
                return;
            }

            throw new ArgumentException("array is not of the correct type.", nameof(array));
        }
    }
}


/*
 * (C) 2019 Gateway Programming School , Inc.
 */
