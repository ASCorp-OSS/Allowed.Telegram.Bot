using Allowed.Telegram.Bot.Attributes;
using Allowed.Telegram.Bot.Controllers;
using Allowed.Telegram.Bot.Enums;
using Allowed.Telegram.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Allowed.Telegram.Bot.Sample.NoDb.Controllers;

[BotName("Sample")]
public class SampleController : CommandController
{
    [Command("start")]
    public async Task Start(MessageData data)
    {
        var result = "You pressed: /start";
        
        if (!string.IsNullOrEmpty(data.Params))
            result = $"{result}\nParams: {data.Params}";

        await data.Client.SendTextMessageAsync(data.Message.Chat!.Id, result);
    }

    // You can use only top example for strict also
    [Command("start", Type = ComparisonTypes.Strict)]
    public async Task StrictStart(MessageData data)
    {
        await data.Client.SendTextMessageAsync(data.Message.Chat!.Id, "You pressed: /start");
    }

    [DefaultCommand]
    public async Task DefaultCommand(MessageData data)
    {
        await data.Client.SendTextMessageAsync(data.Message.Chat!.Id, $"You pressed unknown command: {data.Message.Text}");
    }

    [TextCommand]
    public async Task TextCommand(MessageData data)
    {
        await data.Client.SendTextMessageAsync(data.Message.Chat!.Id, $"You say: {data.Message.Text}");
    }

    [TextCommand("Test text command", Type = ComparisonTypes.Parameterized)]
    public async Task TestTextMessage(MessageData data)
    {
        await data.Client.SendTextMessageAsync(data.Message.Chat!.Id, "You have selected a test text command!");
    }

    // If you type for example "Test text command 2" you will get an unknown command
    // because this text is also correct for previous method
    // Parameterized text commands are recommended only for different initial texts
    [TextCommand("Test text command 2", Type = ComparisonTypes.Parameterized)]
    public async Task TestTextMessage2(MessageData data)
    {
        await data.Client.SendTextMessageAsync(data.Message.Chat!.Id, "You have selected a test text command 2!");
    }

    [TextCommand("cmd1")]
    [TextCommand("cmd2")]
    [TextCommand("CmD3")]
    public async Task TextCommandTest(MessageData data)
    {
        await data.Client.SendTextMessageAsync(data.Message.Chat!.Id, "You have selected a test text command 3!");
    }

    [UpdateCommand(UpdateType.ChatJoinRequest)]
    public async Task UpdateCommandTest(UpdateData data)
    {
        await data.Client.SendTextMessageAsync(data.Chat!.Id,$"Update: {data.Update.Type}");

    }
}