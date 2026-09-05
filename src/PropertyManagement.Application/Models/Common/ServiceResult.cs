namespace PropertyManagement.Application.Models.Common;

public class ServiceResult<T>
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult<T> Success(T data, string message = "")
    {
        return new ServiceResult<T>
        {
            Succeeded = true,
            Message = message,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string message, IEnumerable<string>? errors = null)
    {
        return new ServiceResult<T>
        {
            Succeeded = false,
            Message = message,
            Errors = errors?.ToList() ?? new List<string>()
        };
    }

    public static ServiceResult<T> Failure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new ServiceResult<T>
        {
            Succeeded = false,
            Message = errorList.FirstOrDefault() ?? "An error occurred.",
            Errors = errorList
        };
    }
}

public class ServiceResult
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ServiceResult Success(string message = "")
    {
        return new ServiceResult
        {
            Succeeded = true,
            Message = message
        };
    }

    public static ServiceResult Failure(string message, IEnumerable<string>? errors = null)
    {
        return new ServiceResult
        {
            Succeeded = false,
            Message = message,
            Errors = errors?.ToList() ?? new List<string>()
        };
    }
}
