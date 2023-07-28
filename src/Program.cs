using MassTransit;
using MassTransitSandbox;
using MassTransitSandbox.Messaging.Consumers;
using MassTransitSandbox.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// SqlServer
var sqlServerConnectionStr = builder.Configuration.GetValue<string>("Database:ConnectionString");
builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
{
    optionsBuilder.UseSqlServer(sqlServerConnectionStr);
});

// Mass Transit
builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<CreateUserIntegrationCommandHandler>();
    
    configurator.AddDelayedMessageScheduler();
    configurator.AddEntityFrameworkOutbox<AppDbContext>();
    
    configurator.UsingAzureServiceBus((azContext, azBusConfigurator) =>
    {
        azBusConfigurator.UseServiceBusMessageScheduler();
        azBusConfigurator.Host(builder.Configuration.GetValue<string>("ServiceBus:ConnectionString"));
        
        // // Global delayed redelivery settings
        // azBusConfigurator.UseDelayedRedelivery(retryConfigurator =>
        // {
        //     retryConfigurator.Handle<InvalidOperationException>();
        //     retryConfigurator.Interval(10, TimeSpan.FromSeconds(10));
        // });

        // Receive endpoints configuration
        azBusConfigurator.ReceiveEndpoint(QueuesConstants.PrimaryQueueName, endpointConfigurator =>
        {
            endpointConfigurator.UseEntityFrameworkOutbox<AppDbContext>(azContext);

            // endpointConfigurator.UseDelayedRedelivery(redeliveryConfigurator =>
            // {
            //     redeliveryConfigurator.Handle<TransientException>();
            //     redeliveryConfigurator.Interval(3, TimeSpan.FromSeconds(10));
            // });
            
            endpointConfigurator.ConfigureConsumer<CreateUserIntegrationCommandHandler>(azContext,
                consumerConfigurator =>
                {
                    consumerConfigurator.UseDelayedRedelivery(redeliveryConfigurator =>
                    {
                        redeliveryConfigurator.Handle<TransientException>();
                        redeliveryConfigurator.Interval(3, TimeSpan.FromSeconds(10));
                    });
                });
            
            endpointConfigurator.MaxAutoRenewDuration = TimeSpan.FromMinutes(30);
            endpointConfigurator.RequiresSession = true;
            
            // endpointConfigurator.UseMessageRetry(r =>
            // {
            //     r.Interval(10, TimeSpan.FromMinutes(1));
            // });
        });
    });
});

var app = builder.Build();
app.MapControllers();
app.Run();