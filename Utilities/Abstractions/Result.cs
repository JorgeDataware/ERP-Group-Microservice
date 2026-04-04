namespace UsersMicroservice.Utilities.Abstractions;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public Error error { get; private set; }
    
    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        this.error = error!;
    }
    
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null);
    }
    
    public static Result<T> Failure(Error error)
    {
        return new Result<T>(false, default, error);
    }
}
