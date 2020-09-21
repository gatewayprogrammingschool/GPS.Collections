/*
    # GPS.Collections
    
    ## LinkedArrayEnumerator.cs

    Iterates over the LinkedArray data structure.

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
    /// Enumerator for <see cref="LinkedArray{TValue}" />.
    /// </summary>
    /// <typeparam name="TValue">Data type in the LinkedArray</typeparam>
    [Serializable]
    public class LinkedArrayEnumerator<TValue> : IEnumerator<TValue>, IDisposable
    {
        private int _index = int.MinValue;
        private LinkedArray<TValue> _values = null;

        /// <summary>
        /// Constructor that initializes the enumerator.
        /// </summary>
        /// <param name="values"></param>
        public LinkedArrayEnumerator(LinkedArray<TValue> values)
        {
            _values = values;
            _index = values.Lowest - 1;
        }

        /// <summary>
        /// Current value of TValue under the cursor.
        /// </summary>
        /// <returns>Value of TValue</returns>
        public TValue Current => _index != int.MinValue
                ? _values[_index]
                : default;

        /// <summary>
        /// Interface Accessor of the Current property.
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Disposable pattern.
        /// </summary>
        public void Dispose()
        {
            _values = null;
        }

        /// <summary>
        /// Move the cursor to the next value of the 
        /// data set.
        /// </summary>
        /// <returns>True if move was successful.</returns>
        public bool MoveNext()
        {
            while (_index <= _values.Highest && !_values.IsSet(++_index)) ;

            if (_index == _values.Highest + 1) return false;

            return true;
        }

        /// <summary>
        /// Resets the cursor to default.
        /// </summary>
        public void Reset()
        {
            _index = _values.Lowest - 1;
        }
    }
}
