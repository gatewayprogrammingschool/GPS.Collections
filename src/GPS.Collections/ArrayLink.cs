/*
    # GPS.Collections
    
    ## ArrayLink.cs

    Data structure that contains an array of `T`
    given an initial size and lower bound equating
    to index zero of the backing array.

    ## Copyright

    2019 - Gateway Programming School, Inc.
    
    This notice must be retained for any use the code
    herein in whole or in part for any use.
 */

using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("GPS.Collections.Tests")]
namespace GPS.Collections
{
    /// <summary>
    /// Data structure that represents a single node of a LinkedArray.
    /// </summary>
    /// <typeparam name="T">Type of data stored in the ArrayLink.</typeparam>
    [Serializable]
    internal class ArrayLink<T> : IDisposable
    {
        /// <summary>
        /// Growth rate of the next ArrayLink&lt;T&gt; in the list.
        /// </summary>
        /// <remarks>
        /// Read-only.
        /// </remarks>
        public static readonly decimal GrowthRate = 1.5m;

        /// <summary>
        /// Default size of an ArrayLink&lt;T&gt; instance.
        /// </summary>
        /// <remarks>
        /// Read-only.
        /// </remarks>
        public static readonly int InitialSize = 1024;

        /// <summary>
        /// Starting logical offset of the ArrayLink&lt;T&gt; instance.
        /// </summary>
        /// <value>Logical offset</value>
        public int Start { get; private set; }

        /// <summary>
        /// Lowest index associated with this ArrayLink&lt;T&gt; instance.  This may
        /// be different that the <see cref="Start"/> property.
        /// </summary>
        /// <value>Lowest index</value>
        public int Lowest { get; private set; }

        /// <summary>
        /// Highest index associated with this ArrayLink&lt;T&gt; instance.
        /// </summary>
        /// <value>Highest index</value>
        public int Highest { get; private set; }

        /// <summary>
        /// False until a value is added to the ArrayLink&lt;T&gt;.
        /// </summary>
        internal bool Initialized = false;

        /// <summary>
        /// Size of the <see cref="Values" /> array.
        /// </summary>
        internal int ArraySize = 0;

        /// <summary>
        /// Logical size of the ArrayLink&lt;T&gt; instance.
        /// </summary>
        /// <value>Highest index minus Lowest index plus one if initialized.</value>
        public int Size {
            get{
                var a = Math.Abs(Lowest);
                var b = Math.Abs(Highest);
                return (Lowest < 0 
                    ? (Highest > 0 ? b + a : a - b)
                    : b - a) + (Initialized ? 1 : 0);
            }
        }

        /// <summary>
        /// Array of T containing the values of the ArrayLink&lt;T&gt; instance.
        /// </summary>
        /// <value>T[]</value>
        public (bool set, T value)[] Values { get; private set; }

        /// <summary>
        /// Reference to the ArrayLink&lt;T&gt; representing the previous block
        /// of values in the chain of ArrayLink&lt;T&gt; instances.
        /// </summary>
        /// <value>Lower ArrayLink&lt;T&gt;</value>
        public ArrayLink<T> Lower { get; set; }

        /// <summary>
        /// Reference to the ArrayLink&lt;T&gt; representing the next block
        /// of values in the chain of ArrayLink&lt;T&gt; instances.
        /// </summary>
        /// <value></value>
        public ArrayLink<T> Higher { get; set; }

        /// <summary>
        /// Constructor accepting the logical start index
        /// of the ArrayLink&lt;T&gt; and the requested size of
        /// the ArrayLink&lt;T&gt;.Values array.
        /// </summary>
        /// <param name="start">Logical index of the lowest possible item in ArrayLink&lt;T&gt;.Values.</param>
        /// <param name="size">Requested size of ArrayLink&lt;T&gt;.Values.</param>
        public ArrayLink(int start, int size)
        {
            Start = start;
            ArraySize = Math.Abs(size);
            if (ArraySize < InitialSize) ArraySize = InitialSize;
        }

        /// <summary>
        /// Indexer of the ArrayLink&lt;T&gt; instance.
        /// </summary>
        /// <value>Value of T at the logical key.</value>
        /// <remarks>
        /// Reading:
        /// 
        /// Key values must be in the range from the logical start
        /// of the ArrayLink&lt;T&gt; and the logical Start + ArraySize.
        /// 
        /// If the value is outside of these bounds then the request
        /// will propagate to the Higher or Lower link as necessary.
        /// 
        /// In the event that a key is requested outside the actual
        /// available values of the point-in-time state of the ArrayLink&lt;T&gt;
        /// chain then an <see cref="System.IndexOutOfRangeException"/> is
        /// thrown.
        /// 
        /// Writing:
        /// 
        /// If the logical key is valid for the current ArrayLink&lt;T&gt; instance's
        /// Values array then the value is placed in the Values array of the
        /// instance.  
        /// 
        /// If the logical key falls below the Start property then
        /// the value is sent to the ArrayLink&lt;T&gt; instance in the <see cref="Lower"/>
        /// property. If the Lower property is null, a new ArrayLink&lt;T&gt; will be 
        /// instantiated and assigned to the Lower property before sending the value.
        /// 
        /// If the Logical key falls above the Start + ArraySize then the
        /// value is sent to the ArrayLink&lt;T&gt; instance in the <see cref-"Higher"/>
        /// property.static  If the Higher property is null, a new ArrayLink&lt;T&gt; will
        /// be instantiated and assigned to the Higher property before sending the value.
        /// </remarks>
        public (bool set, T value) this[int key]
        {
            get
            {
                // Ensure initialized
                if(!Initialized && Start == 0) 
                    throw new IndexOutOfRangeException();

                // Key is higher than this ArrayLink&lt;T&gt;
                // has storage for.
                if (key >= Start + ArraySize)
                {
                    if (Higher != null) return Higher[key];

                    // Higher is null and thus the key is 
                    // out of the bounds of the ArrayLink&lt;T&gt; chain.
                    throw new System.IndexOutOfRangeException();
                }
                // Key is lower than this ArrayLink&lt;T&gt;
                // has storage for.
                else if (key < Start)
                {
                    if (Lower != null) return Lower[key];

                    // Lower is null and thus the key is
                    // out of the bounds of the ArrayLink&lt;T&gt; chain.
                    throw new System.IndexOutOfRangeException();
                }

                // Calculate the difference of the key from the logical
                // Start of the ArrayLink&lt;T&gt;.Values array.
                var a = Math.Abs(key);
                var b = Math.Abs(Start);
                var index = key < 0 ? b - a : a - b; 

                // If the values have not been instantiated yet
                // then return the default value.
                if(Values == null) return (set: false, value: default(T));
                
                return Values[index];
            }

            set
            {
                // Calculates the new size of the next ArrayLink&lt;T&gt; in the chain.
                int newSize()
                {
                    var temp = ArraySize * GrowthRate;
                    return temp == (int)temp ? (int)temp : (int)temp + 1;
                }

                // Initializes the Highest and Lowest properties.
                if (!Initialized)
                {
                    Lowest = key;
                    Highest = key;
                    Initialized = true;
                }
                
                // If the key is less than our logical Start
                if (key < Start)
                {
                    // If the Lower has not been instantiated yet,
                    // then instantiate it now.
                    if (Lower == null)
                    {
                        var size = newSize();
                        Lower = new ArrayLink<T>(Start - size, size);
                    }
                    
                    Lowest = Math.Min(key, Lowest);

                    // Recursively set the value in the Lower instance.
                    Lower[key] = value;
                    return;
                }
                // If the key is greater than our Logical Start + ArraySize
                else if (key >= Start + ArraySize)
                {
                    // If the Higher has not bee instantiated yet,
                    // then instantiate it now.
                    if (Higher == null)
                    {
                        var size = newSize();
                        Higher = new ArrayLink<T>(Start + ArraySize, size);
                    }

                    Highest = Math.Max(key, Highest);

                    // Recursively set the value in the Higher instance.
                    Higher[key] = value;
                    return;
                }

                // Calculate the actual index of the 
                // logical key.
                var a = Math.Abs(key);
                var b = Math.Abs(Start);
                var index = key < 0 ? b - a : a - b;  

                Lowest = Math.Min(key, Lowest);
                Highest = Math.Max(key, Highest);                 

                // If Values is null, go ahead and instantiate it.
                if(Values == null) Values = new (bool set, T value)[ArraySize];
                Values[index] = value;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Values = null;
                    
                    if(Lower != null) Lower.Dispose();
                    if(Higher != null) Higher.Dispose();
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
