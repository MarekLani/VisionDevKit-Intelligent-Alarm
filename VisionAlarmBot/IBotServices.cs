using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;

namespace VisionAlarmBot
{
    public interface IBotServices
    {
        LuisRecognizer LuisRecognizer { get; }
    }
}
