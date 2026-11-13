namespace PayLedger.Domain.Entities;

public class IdempotencyRecord
{
    public string Key { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public string RequestBodyHash { get; private set; } = string.Empty;
    public int ResponseStatusCode { get; private set; }
    public string ResponseBody { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    protected IdempotencyRecord() { }

    public IdempotencyRecord(
        string key,
        string endpoint,
        string requestBodyHash,
        int responseStatusCode,
        string responseBody,
        TimeSpan timeToLive)
    {
        Key = key.Trim();
        Endpoint = endpoint;
        RequestBodyHash = requestBodyHash;
        ResponseStatusCode = responseStatusCode;
        ResponseBody = responseBody;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow.Add(timeToLive);
    }

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;
}
