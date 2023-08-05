using Telegram.Bot.Types.Enums;

namespace Allowed.Telegram.Bot.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class UpdateCommandAttribute : Attribute
{
    private readonly UpdateType _type;

    public UpdateCommandAttribute(UpdateType type)
    {
        _type = type;
    }

    public UpdateType GetMessageType()
    {
        return _type;
    }
}