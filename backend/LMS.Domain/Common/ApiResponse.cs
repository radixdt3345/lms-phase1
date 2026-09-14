namespace LMS.Domain.Common;

/// <summary>
/// Universal response envelope. Every API endpoint wraps its payload in this type
/// so callers always receive { "data": T } — never a bare object.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public class ApiResponse<T>
{
    public T Data { get; set; } = default!;

    public static ApiResponse<T> Ok(T data) => new() { Data = data };
}
