using AutoMapper;
using CommandService.Data;
using CommandService.Dtos;
using CommandService.Models;
using System.Text.Json;

namespace CommandService.EventProcessing
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public EventProcessor(IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }
        public void ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);          

            switch (eventType)
            {
                case EventType.PlatformPublish:
                    AddPlatform(message);
                    break;
                default:
                    break;
            }
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            Console.WriteLine("-->Determining Event");

            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            switch (eventType?.Event)
            {
                case "Platform_Published":
                    return EventType.PlatformPublish;

                default:
                    Console.WriteLine("-->Could not determine eventtype");
                    return EventType.Undetermined;
            }
        }

        private void AddPlatform(string platformPublishMessage)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<ICommandRepo>();
                var platformPublishDto = JsonSerializer.Deserialize<PlatformPublishDto>(platformPublishMessage);

                try
                {
                    var platform = _mapper.Map<Platform>(platformPublishDto);
                    if (!repo.ExternalPlatformExists(platform.ExternalID))
                    {
                        repo.CreatePlatform(platform);
                        repo.SaveChanges();
                        Console.WriteLine($"-->Platform added");
                    }
                    else
                    {
                        Console.WriteLine($"-->Platform already exist");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-->Could not add platform {ex.Message}");
                }
            }
        }
    }

    enum EventType
    {
        PlatformPublish,
        Undetermined
    }
}
