namespace Cybertron;

public class ThreadSafeProgress<T> : IProgress<T>
{
    private readonly object _lock = new object();
    private readonly Action<T> _handler;
    
    public ThreadSafeProgress(Action<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }
    
    public void Report(T value)
    {
        lock (_lock)
        {
            _handler(value);
        }
    }
}
