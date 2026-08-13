namespace sellthenews.Services;

public sealed class NewsApiException : Exception
{
    public int? StatusCode { get; }

    public NewsApiException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
