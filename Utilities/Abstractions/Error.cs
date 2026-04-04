namespace UsersMicroservice.Utilities.Abstractions;

public class Error
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
