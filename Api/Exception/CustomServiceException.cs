namespace Api.Exception;
using System;
public class CustomServiceException : Exception
{
    public CustomServiceException()
    {
    }

    public CustomServiceException(string message) : base(message)
    {
    }

    public CustomServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }

    // Optionally, add any additional properties or methods you need
}