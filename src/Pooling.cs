using System;
using System.Collections.Concurrent;

//
// A generic Pool
public class Pool<T> : IDisposable where T : new()
{
    //
    // Create a Pool
    //
    // blockSize - the size for automatic allocations
    // initialSize - the initial number of items in the pool
    // onAllocateItem - optional allocator to use when adding a new item to the pool
    // onDeallocateItem - optional action for each item when deallocating the pool
    // onTakeItem - optional action when taking an item from the pool
    // onReturnItem -optional action when returning an item to the pool
    public Pool(
        int blockSize,
        int initialSize = 0,
        Func<T> onAllocateItem = null,
        Action<T> onDeallocateItem = null,
        Action<T> onTakeItem = null,
        Action<T> onReturnItem = null)
    {
        this.blockSize = blockSize > 0 ? blockSize : 1;
        this.onAllocateItem = onAllocateItem;
        this.onDeallocateItem = onDeallocateItem;
        this.onTakeItem = onTakeItem;
        this.onReturnItem = onReturnItem;
        pool = new ConcurrentBag<T>();
        Allocate(initialSize);
    }

    // Allocate a block of pool items
    public void Allocate(int blockSize)
    {
        for (var i=0; i<blockSize; ++i) {
            pool.Add(onAllocateItem == null ? new T() : onAllocateItem());
        }
    }

    // Take an item from the pool
    public T Take()
    {
        if (pool.TryTake(out T item)) {
            onTakeItem?.Invoke(item);
            return item;
        }
        Allocate(blockSize);
        return Take();
    }

    // Return an item to the pool
    public void Return(T item)
    {
        if (item != null) {
            onReturnItem?.Invoke(item);
            pool.Add(item);
        }
    }

    // Dispose the entire pool
    public void Dispose()
    {
        while (pool.TryTake(out T item)) {
            onDeallocateItem?.Invoke(item);
        }
        GC.SuppressFinalize(this);
    }

    //
    // Private data
    //
    readonly int blockSize;
    readonly ConcurrentBag<T> pool;
    readonly Func<T> onAllocateItem;
    readonly Action<T> onDeallocateItem;
    readonly Action<T> onTakeItem;
    readonly Action<T> onReturnItem;
}
