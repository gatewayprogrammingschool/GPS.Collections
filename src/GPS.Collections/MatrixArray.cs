/*
    # GPS.Collections

    ## MatrixArray.cs

    Data structure that comprises an implementation of
    ICollection<T> that is backed by the MatrixArray object.

    ## Copyright

    2019 - Gateway Programming School, Inc.

    This notice must be retained for any use the code
    herein in whole or in part for any use.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace GPS.Collections
{
    /// <summary>
    /// Collection of generic values TValue that
    /// uses an Array  of Array&lt;TValue&gt;
    /// instances holding the values of the collection.
    /// </summary>
    /// <typeparam name="TValue">Type of data held in the collection.</typeparam>
    [Serializable]
    public partial class MatrixArray<TValue> : IList<TValue>, IDisposable
    {
        /// <summary>
        /// Depth of each Array
        /// </summary>
        private int _depth = 0x400;

        /// <summary>
        /// Total width of the structure
        /// </summary>
        private readonly int _width = 0x800;

        /// <summary>
        /// Upper bounds of the size of the
        /// MatrixArray depth.
        /// </summary>
        private int _maxDepth = 0x800;

        /// <summary>
        /// Maximum depth of the MatrixArray.
        /// </summary>
        public int MaxDepth => _maxDepth;

        /// <summary>
        /// Depth of each column of the matrix
        /// </summary>
        /// <value>int reflecting the depth</value>
        /// <remarks>Must be between 2 and 32768 (0x800).</remarks>
        public int ArrayDepth
        {
            get => _depth;
            set
            {
                if (_depth > _maxDepth) throw new IndexOutOfRangeException($"The max depth of the Array is {_maxDepth}.");
                if (_depth < 2) throw new IndexOutOfRangeException("The minimum depth of the Array is 2.");

                _depth = value;
                Reset();
            }
        }

        /// <summary>
        /// Indicates whether the matrix is
        /// initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// The upper allowable size of the MatrixArray.
        /// </summary>
        public int MaxIndex => _depth * _width;

        /// <summary>
        /// The lower allowable size of the MatrixArray.
        /// </summary>
        public int MinIndex => -MaxIndex;

        /// <summary>
        /// Array of positive indices.
        /// </summary>
        private (bool set, TValue data)[][] _posData = null;

        /// <summary>
        /// Array of negative indices.
        /// </summary>
        private (bool set, TValue data)[][] _negData = null;

        /// <summary>
        /// Default constructor that initializes the MatrixArray
        /// with the default ArrayDepth of 1024 (0x400).
        /// </summary>
        public MatrixArray()
        {
            Reset();
        }

        /// <summary>
        /// Constructor that takes a key and value
        /// of a piece of data being initially added
        /// to the collection.
        /// </summary>
        /// <param name="index">Key of the value being added.</param>
        /// <param name="value">Value to be added.</param>
        /// <returns></returns>
        public MatrixArray(int index, TValue value) : this()
        {
            this[index] = value;
        }

        /// <summary>
        /// Constructor that creates an MatrixArray&lt;T&gt; of
        /// the requested size.
        /// </summary>
        /// <param name="initialDepth">Requested size of the Collection.</param>
        public MatrixArray(int initialDepth)
        {
            this.ArrayDepth = initialDepth;

            Reset();
        }

        /// <summary>
        /// Constructor that accepts an ICollection&lt;T&gt; containing
        /// values to initialize the collection starting at key = 0.
        /// </summary>
        /// <param name="collection">Values to initialize the collection.</param>
        public MatrixArray(ICollection<TValue> collection, int arrayDepth = 0x400) : this()
        {
            ArrayDepth = arrayDepth;
            AddRange(collection);
        }

        /// <summary>
        /// Adds a range represented by an instance of
        /// ICollection&lt;T&gt; to add to the collection
        /// at the current Highest + 1 index.
        /// </summary>
        /// <param name="collection">Values to add to the collection.</param>
        public void AddRange(ICollection<TValue> collection)
        {
            AddRangeAt(_isInitialized ? Highest + 1 : 0, collection);
        }

        /// <summary>
        /// Adds a range represented by an instance of
        /// ICollection&lt;T&gt; to add to the collection
        /// at the specific relative index requested.
        /// </summary>
        /// <param name="index">Logical index to start adding the collection</param>
        /// <param name="collection">Values to add to the collection.</param>
        /// <remarks>
        /// Existing values will be overwritten where the intersect with
        /// the indices of the inbound collection.
        /// </remarks>
        public void AddRangeAt(int index, ICollection<TValue> collection)
        {
            if (collection is IList<TValue> list)
            {
                for (int i = index; i < index + collection.Count; ++i)
                {
                    this[i] = list[i - index];
                }
            }
            else
            {
                foreach (var item in collection)
                {
                    this[index++] = item;
                }
            }
        }

        /// <summary>
        /// Indexor of the MatrixArray&lt;TValue&gt;
        /// </summary>
        /// <value>TValue in the cell.</value>
        /// <throws>IndexOutOfRangeException</throws>
        public TValue this[int index]
        {
            get
            {
                if (index > MaxIndex || index < MinIndex)
                {
                    throw new IndexOutOfRangeException($"Index must be between {MinIndex} and {MaxIndex}");
                }

                var page = Math.Abs(index) / _depth;
                var position = Math.Abs(index) % _depth;

                var array = index >= 0 ? _posData : _negData;

                if (array[page] != null && array[page][position].set)
                {
                    return array[page][position].data;
                }

                throw new IndexOutOfRangeException("No value is set at this location.");
            }
            set
            {
                if (index > MaxIndex || index < MinIndex)
                {
                    throw new IndexOutOfRangeException($"Index must be between {MinIndex} and {MaxIndex}");
                }

                var page = Math.Abs(index) / _depth;
                var position = Math.Abs(index) % _depth;

                var array = index >= 0 ? _posData : _negData;

                if (array[page] == null) array[page] = new (bool set, TValue data)[_depth];

                switch (_isInitialized)
                {
                    case true:
                        Lowest = Math.Min(index, Lowest);
                        Highest = Math.Max(index, Highest);
                        break;

                    case false:
                        Lowest = Highest = index;
                        _isInitialized = true;
                        break;
                }

                array[page][position] = (set: true, data: value);
            }
        }

        public int Lowest { get; private set; }
        public int Highest { get; private set; }

        /// <summary>
        /// Determines if a value is set at an index.
        /// </summary>
        /// <param name="index">Index of value to analyze.</param>
        /// <returns>True if value is set.</returns>
        public bool IsSet(int index)
        {
            if (index > MaxIndex || index < MinIndex)
            {
                throw new IndexOutOfRangeException($"Index must be between {MinIndex} and {MaxIndex}");
            }

            var page = Math.Abs(index) / _depth;
            var position = Math.Abs(index) % _depth;

            var array = index >= 0 ? _posData : _negData;

            if (array[page] == null || !array[page][position].set)
            {
                throw new IndexOutOfRangeException("No value is set at this location.");
            }

            try
            {
                return array[page][position].set;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Number of values in the MatrixArray&lt;TValue&gt;
        /// from the Lowest to Highest.
        /// </summary>
        /// <value>int count</value>
        public int Count
        {
            get
            {
                var a = Math.Abs(Lowest);
                var b = Math.Abs(Highest);
                return (Lowest < 0
                    ? (Highest > 0 ? b + a : a - b)
                    : b - a) + (_isInitialized ? 1 : 0);
            }
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the end of the
        /// MatrixArray&lt;TValue&gt;.
        /// </summary>
        /// <param name="item"></param>
        public void Add(TValue item)
        {
            this[Highest + (_isInitialized ? 1 : 0)] = item;
        }

        /// <summary>
        /// Clears the MatrixArray&lt;TValue&gt;.
        /// </summary>
        public void Clear()
        {
            Reset();
        }

        /// <summary>
        /// Restores the MatrixArray to its original
        /// state.
        /// </summary>
        private void Reset()
        {
            _isInitialized = false;
            Lowest = Highest = 0;
            _posData = new (bool set, TValue data)[_width][];
            _negData = new (bool set, TValue data)[_width][];
        }

        /// <summary>
        /// Determines of the MatrixArray&lt;TValue&gt;
        /// contains the requested value.
        /// </summary>
        /// <param name="item">Value to search for.</param>
        /// <returns>True if it is found, otherwise false.</returns>
        public bool Contains(TValue item)
        {
            return IndexOf(item) >= MinIndex;
        }

        /// <summary>
        /// Copies the contents of the MatrixArray&lt;TValue&gt;
        /// to a suitably sized target array.
        /// </summary>
        /// <param name="array">Target array</param>
        /// <param name="arrayIndex">Index within the target
        /// array to place the contents of the MatrixArray&lt;TValue&gt;.
        /// </param>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Target array is not large enough.");

            for (int index = arrayIndex; index <= Highest + arrayIndex; ++index)
            {
                array[index] = this[Lowest + index - arrayIndex];
            }
        }

        /// <summary>
        /// Returns a new enumerator covering the MatrixArray&lt;TValue&gt;.
        /// </summary>
        /// <returns><see cref="MatrixArrayEnumerator"/></returns>
        public IEnumerator<TValue> GetEnumerator()
        {
            return new MatrixArrayEnumerator(this);
        }

        /// <summary>
        /// Returns the index of the requested value if
        /// it exists.
        /// </summary>
        /// <param name="item">Value to locate.</param>
        /// <returns>The actual index of the item, or Int32.MinValue
        /// if not found.</returns>
        public int IndexOf(TValue item)
        {
            int index = Lowest;

            while (index <= Highest)
            {
                try
                {
                    if (item.Equals(this[index])) return index;
                }
                catch { }

                ++index;
            }

            return Int32.MinValue;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, TValue item)
        {
            throw new System.NotImplementedException("Shape of data may not be changed.");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(TValue item)
        {
            throw new System.NotImplementedException("Shape of data may not be changed.");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException("Shape of data may not be changed.");
        }

        /// <summary>
        /// Returns a new enumerator covering the MatrixArray&lt;TValue&gt;.
        /// </summary>
        /// <returns><see cref="MatrixArrayEnumerator"/></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #region IDisposable Support

        /// <summary>
        /// Disposable pattern flag.
        /// </summary>
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < _width; ++i)
                    {
                        _posData[i] = null;
                        _negData[i] = null;
                    }
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposable pattern.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
