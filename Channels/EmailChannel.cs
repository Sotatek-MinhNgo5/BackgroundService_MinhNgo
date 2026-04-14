using BackgroundServices.Models;
using System.Threading.Channels;
namespace BackgroundServices.Channels;

public class EmailChannel
{
    // Giới hạn 100 message trong queue
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateBounded<EmailMessage>(100);

    public ChannelWriter<EmailMessage> Writer => _channel.Writer;
    public ChannelReader<EmailMessage> Reader => _channel.Reader;
}