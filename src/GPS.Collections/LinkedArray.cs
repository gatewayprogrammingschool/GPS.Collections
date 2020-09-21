/*
    # GPS.Collections
    
    ## LinkedArray.cs

    Data structure that comprises an implementation of 
    ICollection<TValue> that is backed by the ArrayLink object.

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
    /// uses a linked list of ArrayLink&lt;TValue&gt;
    /// instances holding the values of the collection.
    /// </summary>
    /// <typeparam name="TValue">Type of data held in the collection.</typeparam>
    [Serializable]
    public sealed class LinkedArray<TValue> : ICollection<TValue>, IList<TValue>, IDisposable
    {
        /// <summary>
        /// Root ArrayLink&lt;TValue&gt;
        /// </summary>
        private ArrayLink<TValue> _root = null;

        /// <summary>
        /// Capacity of the Root
        /// </summary>
        private int Size => _root.Size;

        /// <summary>
        /// Lowest value in the Collection
        /// </summary>
        public int Lowest => _root.Lowest;

        /// <summary>
        /// Highest value in the Collection
        /// </summary>
        public int Highest => _root.Highest;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <remarks>
        /// Initializes the collection with a
        /// root ArrayLink&lt;TValue&gt;.
        /// </remarks>
        public LinkedArray()
        {
            _root = new ArrayLink<TValue>(0, ArrayLink<TValue>.InitialSize);
        }

        /// <summary>
        /// Constructor that takes a key and value
        /// of a piece of data being initially added
        /// to the collection.
        /// </summary>
        /// <param name="index">Key of the value being added.</param>
        /// <param name="value">Value to be added.</param>
        /// <returns></returns>
        public LinkedArray(int index, TValue value) : this()
        {
            _root[index] = (set: true, value);
        }

        /// <summary>
        /// Constructor that creates an ArrayLink&lt;TValue&gt; of
        /// the requested size.
        /// </summary>
        /// <param name="initialSize">Requested size of the Collection.</param>
        public LinkedArray(int initialSize)
        {
            _root = new ArrayLink<TValue>(0, initialSize);
        }

        /// <summary>
        /// Constructor that accepts an ICollection&lt;TValue&gt; containing
        /// values to initialize the collection starting at key = 0.
        /// </summary>
        /// <param name="collection">Values to initialize the collection.</param>
        public LinkedArray(ICollection<TValue> collection)
        {
            _root = new ArrayLink<TValue>(0, Math.Max(collection.Count, ArrayLink<TValue>.InitialSize));

            AddRange(collection);
        }

        /// <summary>
        /// Adds a range represented by an instance of
        /// ICollection&lt;TValue&gt; to add to the collection
        /// at the current Highest + 1 index.
        /// </summary>
        /// <param name="collection">Values to add to the collection.</param>
        public void AddRange(ICollection<TValue> collection)
        {
            AddRangeAt(_root.Initialized ? Highest + 1 : 0, collection);
        }

        /// <summary>
        /// Adds a range represented by an instance of
        /// ICollection&lt;TValue&gt; to add to the collection
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
        /// Gets or Sets values in the collection.
        /// </summary>
        /// <value>Value of TValue</value>
        public TValue this[int index]
        {
            get => _root[index].value;
            set
            {
                _root[index] = (set: true, value);
            }
        }

        /// <summary>
        /// Determines if a value is set at an index.
        /// </summary>
        /// <param name="index">Index of value to analyze.</param>
        /// <returns>True if value is set.</returns>
        public bool IsSet(int index)
        {
            return _root[index].set;
        }

        /// <summary>
        /// Size of the collection.
        /// </summary>
        public int Count => Size;

        /// <summary>
        /// Flag indicated the writability of the collection.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds a single value TValue at the end of the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Add(TValue item)
        {
            AddRange(new[] { item });
        }

        /// <summary>
        /// Resets the collection to its default state.
        /// </summary>
        public void Clear()
        {
            _root.Dispose();
            _root = new ArrayLink<TValue>(0, ArrayLink<TValue>.InitialSize);
        }

        /// <summary>
        /// Indicates that the collection contains the specified value.
        /// </summary>
        /// <param name="item">Value of TValue to search for.</param>
        /// <returns>True if the value is found, otherwise false.</returns>
        public bool Contains(TValue item)
        {
            return IndexOf(item) >= Lowest;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            throw new NotImplementedException(
                "Cannot preserve spatial integrity");
        }

        /// <summary>
        /// Returns the <see cref="LinkedArrayEnumerator{TValue}"/> for the collection.
        /// </summary>
        /// <returns>IEnumerator&lt;TValue&gt;</returns>
        public IEnumerator<TValue> GetEnumerator()
        {
            return new LinkedArrayEnumerator<TValue>(this);
        }

        /// <summary>
        /// Returns the logical index of the specified value.
        /// </summary>
        /// <param name="item">Value to search for.</param>
        /// <returns>Logical index of the value.  Returns Lowest - 1 if the value
        /// is not found.</returns>
        public int IndexOf(TValue item)
        {
            for (int i = Lowest; i <= Highest; ++i)
            {
                if (_root[i].Equals((true, item))) return i;
            }

            return _root.Lowest - 1;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, TValue item)
        {
            throw new InvalidOperationException(
                "LinkedArray<TValue> cannot change indices of elements.");
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(TValue item)
        {
            throw new InvalidOperationException(
                "LinkedArray<TValue> cannot change indices of elements.");
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            throw new InvalidOperationException(
                "LinkedArray<TValue> cannot change indices of elements.");
        }

        /// <summary>
        /// Explicit interface reference to get the Enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _root.Dispose();
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
