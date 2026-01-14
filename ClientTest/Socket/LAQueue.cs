namespace ClientTest.Socket;

public class LAQueue<T>
{
    private readonly LinkedList<T> _list = [];
    private readonly Lock _lock = new();

    public int Count { get { lock(_lock) {return _list.Count;} } }

    public T Peek()
    {
        lock (_lock) 
        {
            if (_list.Count <= 0)
                throw new InvalidOperationException ();
            
            return _list.First();
        }
    }

    public T Dequeue()
    {
        lock (_lock) 
        {
            if (_list.Count <= 0)
                throw new InvalidOperationException ();

            var ret = _list.First();
            _list.RemoveFirst ();
            return ret;
        }
    }

    public void Enqueue(T entity)
    {
        lock (_lock) 
        {
            if (null == entity)
                return;

            _list.AddLast (entity);
        }
    }

    public void EnqueueFirst(T entity)
    {
        lock (_lock)
        {
            if (null == entity)
                return;

            _list.AddFirst(entity);
        }
    }


/*        public void ChangeFirstValue(T entity)
        {
            lock (_que_lock)
            {
                if (_list.Count <= 0)
                    throw new System.InvalidOperationException ();

                _list.RemoveFirst ();
                _list.AddFirst (entity);
            }
        }*/

    public void Clear()
    {
        lock (_lock) 
        {
            _list.Clear ();
        }
    }
}