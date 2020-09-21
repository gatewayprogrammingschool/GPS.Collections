using System.Collections;
using System.Collections.Generic;

namespace GPS.Collections.Tests
{
    public class IntDataSet2 : IEnumerable<object[]>
    {
        private static readonly int size = ArrayLink<object>.InitialSize;
        private static readonly int growth = (int)(size + size * ArrayLink<object>.GrowthRate);
        private readonly object[] _data =
        {
                  (set: new [] { -1, -2, -3 }, size: 3, count: growth, higher: false, lower: true)
                , (set: new [] { 0, 1, 2 }, size: 3, count: size, higher: false, lower: false)
                , (set: new [] { size - 1, size }, size: 2, count: growth, higher: true, lower: false)
                , (set: new [] { -(size - 1), size }, size: size * 2, count: growth + (int)(size*ArrayLink<object>.GrowthRate), higher: true, lower: true)
            };

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


/*
 * (C) 2019 Your Legal Entity's Name
 */
