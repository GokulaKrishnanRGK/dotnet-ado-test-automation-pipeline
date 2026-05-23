namespace OpsLedger.Core.Common.Models;

public sealed class OperationResult<T>
{
    private readonly T? _value;

    private OperationResult(T? value, IReadOnlyList<string> errors)
    {
        _value = value;
        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed operation.");

    public IReadOnlyList<string> Errors { get; }

    public static OperationResult<T> Success(T value)
    {
        return new OperationResult<T>(value, Array.Empty<string>());
    }

    public static OperationResult<T> Failure(IReadOnlyList<string> errors)
    {
        return new OperationResult<T>(default, errors);
    }
}
