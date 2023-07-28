using MassTransit;
using MassTransitSandbox.Messaging;
using MassTransitSandbox.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace MassTransitSandbox.Controllers;

[ApiController]
[Route("sandbox")]
public class SandboxController : ControllerBase
{
    private readonly ILogger<SandboxController> _logger;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly AppDbContext _appDbContext;

    public SandboxController(ILogger<SandboxController> logger, 
        ISendEndpointProvider sendEndpointProvider,
        AppDbContext appDbContext)
    {
        _logger = logger;
        _sendEndpointProvider = sendEndpointProvider;
        _appDbContext = appDbContext;
    }

    [HttpPost("user")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto requestDto, 
        CancellationToken cancellationToken)
    {
        var integrationCmd = new CreateUserIntegrationCommand(
            requestDto.FirstName,
            requestDto.LastName
        );

        var queueUri = new Uri($"queue:{QueuesConstants.PrimaryQueueName}");
        var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(queueUri);
        await sendEndpoint.Send(integrationCmd, context => context.SetSessionId("XD"));
        
        _logger.LogInformation("Create user integration for user '{FirstName} {LastName}' sent'",
            requestDto.FirstName, requestDto.LastName);

        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    public record CreateUserRequestDto(
        string FirstName,
        string LastName
    );
}