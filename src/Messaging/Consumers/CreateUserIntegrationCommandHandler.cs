using MassTransit;

namespace MassTransitSandbox.Messaging.Consumers;

public class CreateUserIntegrationCommandHandler : IConsumer<CreateUserIntegrationCommand>
{
    private readonly ILogger<CreateUserIntegrationCommandHandler> _logger;

    public CreateUserIntegrationCommandHandler(ILogger<CreateUserIntegrationCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateUserIntegrationCommand> context)
    {
        await Task.CompletedTask;
        
        _logger.LogInformation("User '{FirstName} {LastName}' created", 
            context.Message.FirstName, context.Message.LastName);

        if (new Random().Next() == 2137)
        {
            throw new InvalidOperationException();
        }
        if (new Random().Next() == 2138)
        {
            return;
        }

        throw new TransientException();
    }
}