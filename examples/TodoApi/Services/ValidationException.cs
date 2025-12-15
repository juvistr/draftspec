namespace TodoApi.Services;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
