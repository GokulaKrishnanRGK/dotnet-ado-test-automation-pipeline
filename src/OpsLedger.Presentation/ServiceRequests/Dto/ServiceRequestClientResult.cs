namespace OpsLedger.Presentation.ServiceRequests.Dto;

public sealed class ServiceRequestClientResult
{
    private ServiceRequestClientResult(ServiceRequestSummary? value, IReadOnlyList<string> errors)
    {
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess => Value is not null;

    public ServiceRequestSummary? Value { get; }

    public IReadOnlyList<string> Errors { get; }

    public static ServiceRequestClientResult Created(ServiceRequestSummary value)
    {
        return new ServiceRequestClientResult(value, []);
    }

    public static ServiceRequestClientResult Invalid(IReadOnlyList<string> errors)
    {
        return new ServiceRequestClientResult(null, errors);
    }
}
