namespace ScheduleMaker.App.Application;

public class ApplicationOperationResult
{
    protected ApplicationOperationResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public string? ErrorMessage { get; }

    public static ApplicationOperationResult Success() => new(true, null);

    public static ApplicationOperationResult Failure(string errorMessage) => new(false, errorMessage);
}

public sealed class ApplicationOperationResult<T> : ApplicationOperationResult
{
    private ApplicationOperationResult(bool isSuccess, string? errorMessage, T? value)
        : base(isSuccess, errorMessage)
    {
        Value = value;
    }

    public T? Value { get; }

    public static ApplicationOperationResult<T> Success(T value) => new(true, null, value);

    public new static ApplicationOperationResult<T> Failure(string errorMessage) => new(false, errorMessage, default);
}
