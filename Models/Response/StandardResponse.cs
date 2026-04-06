namespace GroupsMicroservice.Models.Response;

public class StandardResponse<T>
{
    public int StatusCode { get; set; }
    public string IntOpCode { get; set; } = null!;
    public string Message { get; set; } = null!;
    public T[] Data { get; set; } = Array.Empty<T>();
}
