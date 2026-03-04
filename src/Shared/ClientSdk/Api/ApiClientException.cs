using System.Net;

namespace GalacticTrader.Desktop.Api;

public sealed class ApiClientException : InvalidOperationException
{
    public ApiClientException(
        string operation,
        HttpStatusCode statusCode,
        string? detail)
        : base($"{operation} failed ({(int)statusCode}): {detail}")
    {
        Operation = operation;
        StatusCode = statusCode;
        Detail = detail ?? string.Empty;
    }

    public string Operation { get; }

    public HttpStatusCode StatusCode { get; }

    public string Detail { get; }
}
