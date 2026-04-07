namespace LeilaoAuto.Domain.Entities;

public class ConnectorExecutionLog
{
    private ConnectorExecutionLog()
    {
    }

    public ConnectorExecutionLog(
        string connectorName,
        DateTime executedAt,
        bool success,
        int recordsRead,
        int recordsSaved,
        string? message,
        string? payloadJson)
    {
        Id = Guid.NewGuid();
        ConnectorName = connectorName.Trim();
        ExecutedAt = executedAt;
        Success = success;
        RecordsRead = recordsRead;
        RecordsSaved = recordsSaved;
        Message = message;
        PayloadJson = payloadJson;
    }

    public Guid Id { get; private set; }
    public string ConnectorName { get; private set; } = string.Empty;
    public DateTime ExecutedAt { get; private set; }
    public bool Success { get; private set; }
    public int RecordsRead { get; private set; }
    public int RecordsSaved { get; private set; }
    public string? Message { get; private set; }
    public string? PayloadJson { get; private set; }
}
