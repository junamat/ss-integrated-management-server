namespace ss.Internal.Management.Server.AutoRef;

public interface IAutoRef
{
    Task StartAsync();
    
    Task StopAsync();
    
    Task SendMessageFromDiscord(string content);
}