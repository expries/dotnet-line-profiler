namespace LineProfiler.DebugInfo;

public class PdbModule
{
    private readonly Dictionary<int, PdbFunction> _functionsByToken = new();
    private readonly Dictionary<nint, PdbFunction> _functionsByFunctionId = new();
    
    public bool TryGetFunctionByToken(int token, out PdbFunction? function)
    {
        return _functionsByToken.TryGetValue(token, out function);
    }
    
    public bool TryGetFunctionById(nint functionId, out PdbFunction? function)
    {
        return _functionsByFunctionId.TryGetValue(functionId, out function);
    }
    
    public void AddFunction(int functionToken, PdbFunction function)
    {
        _functionsByToken[functionToken] = function;
    }
    
    public void AddFunctionAlias(int functionToken, nint functionId)
    {
        if (_functionsByToken.TryGetValue(functionToken, out var function))
        {
            _functionsByFunctionId[functionId] = function;
        }
    }
}