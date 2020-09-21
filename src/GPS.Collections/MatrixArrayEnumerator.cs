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
    public partial class MatrixArray<TValue>
    {
        /// <summary>
        /// Enumerator for <see cref="LinkedArray{TValue}" />.
        /// </summary>
        /// <typeparam name="TValue">Data type in the LinkedArray</typeparam>
        [Serializable]
        public class MatrixArrayEnumerator : IEnumerator<TValue>
        {
            private MatrixArray<TValue> _array;
            private int _index = Int32.MinValue;

            /// <summary>
            /// Constructor that initializes the enumerator.
            /// </summary>
            /// <param name="array"></param>
            public MatrixArrayEnumerator(MatrixArray<TValue> array)
            {
                _array = array;
            }

            /// <summary>
            /// Current value of TValue under the cursor.
            /// </summary>
            /// <returns>Value of TValue</returns>
            public TValue Current => _array[_index];

            /// <summary>
            /// Interface Accessor of the Current property.
            /// </summary>
            object IEnumerator.Current => this.Current;

            /// <summary>
            /// Disposable pattern.
            /// </summary>
            public void Dispose()
            {
                _array = null;
            }

            /// <summary>
            /// Move the cursor to the next value of the
            /// data set.
            /// </summary>
            /// <returns>True if move was successful.</returns>
            public bool MoveNext()
            {
                try
                {
                    if (!_array._isInitialized) return false;

                    if (_index == Int32.MinValue) _index = _array.Lowest - 1;

                    ++_index;

                    if (_index > _array.Highest) return false;

                    var page = Math.Abs(_index) / _array._depth;
                    var position = Math.Abs(_index) % _array._depth;

                    var array = _index >= 0 ? _array._posData : _array._negData;

                    while (_index <= _array.Highest && (array[page] == null || !array[page][position].set))
                    {
                        ++_index;
                    }

                    if (_index > _array.Highest) return false;
                }
                catch { }

                return true;
            }

            /// <summary>
            /// Resets the cursor to default.
            /// </summary>
            public void Reset()
            {
                _index = Int32.MinValue;
            }
        }
    }
}
