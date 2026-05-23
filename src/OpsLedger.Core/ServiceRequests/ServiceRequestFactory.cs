using System.Net.Mail;
using OpsLedger.Core.Common.Models;
using OpsLedger.Core.ServiceRequests.Constants;
using OpsLedger.Core.ServiceRequests.Dto;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Core.ServiceRequests;

public static class ServiceRequestFactory
{
    public static OperationResult<ServiceRequest> Create(
        CreateServiceRequestCommand command,
        DateTimeOffset createdAt)
    {
        List<string> errors = Validate(command);

        if (errors.Count > 0)
        {
            return OperationResult<ServiceRequest>.Failure(errors);
        }

        ServiceRequest request = new(
            command.Title.Trim(),
            command.Category,
            command.Priority,
            command.Description.Trim(),
            command.RequesterName.Trim(),
            command.RequesterEmail.Trim(),
            NormalizeOptional(command.ImpactDetails),
            RequestStatus.New,
            createdAt,
            CalculateSlaDueAt(command.Priority, createdAt),
            new[]
            {
                new RequestActivity(
                    RequestActivityType.Created,
                    createdAt,
                    "Service request created.")
            });

        return OperationResult<ServiceRequest>.Success(request);
    }

    private static List<string> Validate(CreateServiceRequestCommand command)
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            errors.Add("Description is required.");
        }

        if (string.IsNullOrWhiteSpace(command.RequesterName))
        {
            errors.Add("Requester name is required.");
        }

        if (!IsValidEmail(command.RequesterEmail))
        {
            errors.Add("Requester email must be a valid email address.");
        }

        if (command.Priority == RequestPriority.Critical &&
            string.IsNullOrWhiteSpace(command.ImpactDetails))
        {
            errors.Add("Critical requests require impact details.");
        }

        return errors;
    }

    private static DateTimeOffset CalculateSlaDueAt(
        RequestPriority priority,
        DateTimeOffset createdAt)
    {
        return priority switch
        {
            RequestPriority.Low => createdAt.AddHours(72),
            RequestPriority.Normal => createdAt.AddHours(24),
            RequestPriority.High => createdAt.AddHours(8),
            RequestPriority.Critical => createdAt.AddHours(4),
            _ => createdAt.AddHours(24)
        };
    }

    private static bool IsValidEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            MailAddress address = new(value.Trim());
            return string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
