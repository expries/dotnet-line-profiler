namespace Profiler.Core;

public readonly struct HResult
{
    public const int S_OK = 0; // Vorgang erfolgreich (0x00000000)
    public const int S_FALSE = 1; // Vorgang teilweise erfolgreich

    public const int E_FAIL = unchecked((int)0x80004005); // Nicht spezifizierter Fehler
    public const int E_INVALIDARG = unchecked((int)0x80070057); // Mindestens ein Argument ist ungültig.
    public const int E_NOTIMPL = unchecked((int)0x80004001); // Nicht implementiert
    public const int E_NOINTERFACE = unchecked((int)0x80004002); // Schnittstelle nicht unterstützt.
    public const int CORPROF_E_UNSUPPORTED_CALL_SEQUENCE = unchecked((int)0x80131363); // Nicht unterstützte Aufrufreihenfolge

    public const int E_ABORT = unchecked((int)0x80004004); // Vorgang abgebrochen
    public const int E_ACCESSDENIED = unchecked((int)0x80070005); // Fehler "Allgemeiner Zugriff verweigert"
    public const int E_HANDLE = unchecked((int)0x80070006); // Ungültiges Handle
    public const int E_OUTOFMEMORY = unchecked((int)0x8007000E); // Fehler beim Zuweisen des erforderlichen Arbeitsspeichers
    public const int E_POINTER = unchecked((int)0x80004003); // Ungültiger Zeiger
    public const int E_UNEXPECTED = unchecked((int)0x8000FFFF); // Unerwarteter Fehler
    public const int COR_E_EXCEPTION = unchecked((int)0x80131500); // Unerwarteter Fehler
    public const int COR_E_TYPELOAD = unchecked((int)0x80131130);
    
    public static string GetStatusMessage(HResult hresult)
    {
        switch (hresult)
        {
            case S_OK:
                return "OK";
            case S_FALSE:
                return "FALSE";
            case E_FAIL:
                return "FAIL";
            case E_INVALIDARG:
                return "INVALIDARG";
            case E_NOTIMPL:
                return "NOTIMPL";
            case E_NOINTERFACE:
                return "NOINTERFACE";
            case CORPROF_E_UNSUPPORTED_CALL_SEQUENCE:
                return "UNSUPPORTED_CALL_SEQUENCE";
            case E_ABORT:
                return "ABORT";
            case E_ACCESSDENIED:
                return "ACCESSDENIED";
            case E_HANDLE:
                return "HANDLE";
            case E_OUTOFMEMORY:
                return "OUTOFMEMORY";
            case E_POINTER:
                return "POINTER";
            case E_UNEXPECTED:
                return "UNEXPECTED";
            case COR_E_EXCEPTION:
                return "COR_E_EXCEPTIONs";
            case COR_E_TYPELOAD :
                return "COR_E_TYPELOAD";
            default:
                return "UNKNOWN"; // No match found
        }
    }
    public bool IsOK => Code == S_OK;
    
    public bool IsDuplicate => Code == 1249687;

    public int Code { get; }

    public HResult(int hr) => Code = hr;

    public static implicit operator HResult(int hr) => new(hr);

    /// <summary>
    /// Helper to convert to int for comparisons.
    /// </summary>
    public static implicit operator int(HResult hr) => (int)hr.Code;

    /// <summary>
    /// This makes "if (hr)" equivalent to SUCCEEDED(hr).
    /// </summary>
    public static implicit operator bool(HResult hr) => hr.Code >= 0;

    public static string ToString(int code)
    {
        return code switch
        {
            S_OK => "S_OK",
            S_FALSE => "S_FALSE",
            E_FAIL => "E_FAIL",
            E_INVALIDARG => "E_INVALIDARG",
            E_NOTIMPL => "E_NOTIMPL",
            E_NOINTERFACE => "E_NOINTERFACE",
            CORPROF_E_UNSUPPORTED_CALL_SEQUENCE => "CORPROF_E_UNSUPPORTED_CALL_SEQUENCE",
            _ => $"{code:x8}",
        };

    }

    public override string ToString() => ToString(Code);
}