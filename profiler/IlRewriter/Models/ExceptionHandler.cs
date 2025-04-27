namespace IlRewriter.Models;

public class ExceptionHandler
{
    public ExceptionHandler(
        ExceptionHandlerType handlerType, 
        Instruction tryStart, 
        Instruction tryEnd,
        Instruction handlerStart, 
        Instruction handlerEnd)
    {
        TryStart = tryStart;
        TryEnd = tryEnd;
        HandlerStart = handlerStart;
        HandlerEnd = handlerEnd;
        HandlerType = handlerType;
    }

    public ExceptionHandlerType HandlerType { get; set; }
    public Instruction TryStart { get; set; }
    public Instruction TryEnd { get; set; }
    public Instruction HandlerStart { get; set; }
    public Instruction HandlerEnd { get; set; }
    public Instruction? FilterStart { get; set; }
    public uint? CatchTypeToken { get; set; }
}