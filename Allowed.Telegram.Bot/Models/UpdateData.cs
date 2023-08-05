using Telegram.Bot;
using Telegram.Bot.Types;

namespace Allowed.Telegram.Bot.Models;

public class UpdateData
{
    public ITelegramBotClient Client { get; set; }
    public SimpleTelegramBotClientOptions Options { get; set; }
    public Update Update { get; set; }
    public Chat Chat { get; set; }
}