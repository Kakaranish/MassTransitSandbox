namespace MassTransitSandbox.Messaging;

public record CreateUserIntegrationCommand(
    string FirstName,
    string LastName
);