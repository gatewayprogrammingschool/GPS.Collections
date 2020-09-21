using System.Collections;
using System.Collections.Generic;

namespace GPS.Collections.Tests
{
    public class LinkedArrayDataSet1 : IEnumerable<object[]>
    {
        private readonly object[] _data = {
                10, 20, 30, 40, 50, 60, 70, 80, 90, 100,
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
