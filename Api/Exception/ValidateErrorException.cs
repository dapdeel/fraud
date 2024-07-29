namespace Api.Exception;
using System;
public class ValidateErrorException : Exception
{
    public ValidateErrorException()
    {
    }

    public ValidateErrorException(string message) : base(message)
    {
    }

    public ValidateErrorException(string message, Exception innerException) : base(message, innerException)
    {
    }

    // Optionally, add any additional properties or methods you need
}