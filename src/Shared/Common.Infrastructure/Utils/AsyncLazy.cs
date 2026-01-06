using System.Runtime.CompilerServices;

namespace Common.Infrastructure.Utils;

public class AsyncLazy<T>(Func<Task<T>> valueFactory) : Lazy<Task<T>>(() => Task.Run(valueFactory))
{
    public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();
}