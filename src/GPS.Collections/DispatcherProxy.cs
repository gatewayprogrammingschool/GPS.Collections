using System;
using System.Threading.Tasks;

namespace GPS.Collections
{
    public abstract class DispatcherProxy
    {
        public abstract Task<bool> TryDispatchAsync(Action action);
    }
}