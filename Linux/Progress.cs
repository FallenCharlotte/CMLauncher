using System;

public interface IProgress<T>
{
    void Report(T value);
}
public class Progress<T> : IProgress<T>
{
    private Action<T> _action;
    
    public Progress(Action<T> func)
    {
        _action = func;
    }

    public void Report(T value)
    {
        _action.Invoke(value);
    }
}