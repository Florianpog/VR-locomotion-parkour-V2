using System;
using System.Collections;
using System.Collections.Generic;

public class MyQueue<T> : IEnumerable<T>
{
    private List<T> _list = new List<T>();

    // Enqueue adds an element to the end of the queue
    public void Enqueue(T item)
    {
        _list.Add(item);
    }

    // Dequeue removes and returns the element at the front of the queue
    public T Dequeue()
    {
        if (_list.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        T item = _list[0];
        _list.RemoveAt(0);
        return item;
    }

    // Peek returns the element at the front of the queue without removing it
    public T Peek()
    {
        if (_list.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _list[0];
    }

    // Indexer to allow access by index
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _list.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            return _list[index];
        }
    }

    // Count property to get the number of elements in the queue
    public int Count => _list.Count;

    // Clear removes all elements from the queue
    public void Clear()
    {
        _list.Clear();
    }

    // Contains checks if the queue contains a specific element
    public bool Contains(T item) => _list.Contains(item);

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
