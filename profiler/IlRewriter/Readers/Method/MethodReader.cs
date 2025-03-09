using IlRewriter.Models;

namespace IlRewriter.Readers.Method;

public class MethodReader
{
	private readonly IlReader<MethodHeader> _methodHeaderReader;
	private readonly IlReader<MethodBody> _methodBodyReader;
	
	public MethodReader(Stream stream)
	{
		var readContext = new ReadContext();
		_methodHeaderReader = new MethodHeaderReader(stream, readContext);
		_methodBodyReader = new MethodBodyReader(stream, readContext);
	}

	public Models.Method ReadFromStream()
	{
		var header = _methodHeaderReader.ReadFromStream();
		var body =  _methodBodyReader.ReadFromStream();
		return new Models.Method
		{
			Header = header,
			Body = body
		};
	}
}

/*

public class IlMethodBodyReader : BinaryStreamReader
{
    
    public Instruction[] Instructions { get; }
	public MethodExceptionHandlers? ExceptionHandlers { get; set; }

	private readonly MethodHeader _header;
    private readonly Instruction?[] _instructionsByOffset;
    
    private int _start;
    private int Offset => Position - _start;
    private const byte ExtendedOpCode = 0xfe;
    
    public IlMethodBodyReader(MethodHeader header, Stream stream) : base(stream)
    {
        _header = header;
        _instructionsByOffset = new Instruction[_header.CodeSize + 1];
        
        ReadInstructions();
        Instructions = _instructionsByOffset.OfType<Instruction>().ToArray();

        if (_header.HasExceptionHandler())
        {
	        ExceptionHandlers = new MethodExceptionHandlers(this, reader);
        }
    }

    private void ReadInstructions()
    {
	    MoveTo((uint) _header.SizeInBytes);
        _start = Position;
		
        while (Position < _start + _header.CodeSize)
        {
            ReadInstruction();
        }
		
        ResolveBranches();
    }
    
    private void ReadInstruction()
	{
		var offset = Offset;
		var opCode = ReadOpCode();
		var instruction = new Instruction(offset, opCode);
		
		if (opCode.OperandType != OperandType.InlineNone)
		{
			instruction.Operand = ReadOperand(instruction);
		}

		_instructionsByOffset[offset] = instruction;
    }


    private OpCode ReadOpCode()
    {
	    var byteCode = ReadByte();
	    
	    // Single byte opcodes
	    if (byteCode != ExtendedOpCode)
	    {
		    return OpCodes.OneByteOpCode[byteCode];
	    }

	    // Two byte opcodes
	    byteCode = ReadByte();
	    return OpCodes.TwoBytesOpCode[byteCode];
    }
    
    private Operand ReadOperand(Instruction instruction)
    {
	    switch (instruction.OpCode.OperandType) {
		    case OperandType.InlineSwitch:
			    return new InlineSwitchOperand(_reader, () => Offset);
		    case OperandType.ShortInlineBrTarget:
			    return new ShortInlineBrTargetOperand(_reader, () => Offset);
		    case OperandType.InlineBrTarget:
			    return new InlineBrTargetOperand(_reader, () => Offset);
		    case OperandType.ShortInlineI:
			    return new ShortInlineIOperand(_reader, instruction.OpCode);
		    case OperandType.InlineI:
			    return new Int32Operand(_reader);
		    case OperandType.ShortInlineR:
			    return new SingleOperand(_reader);
		    case OperandType.InlineR:
			    return new DoubleOperand(_reader);
		    case OperandType.InlineI8:
			    return new Int64Operand(_reader);
		    case OperandType.ShortInlineVar:
		    case OperandType.ShortInlineArg:
			    return new ByteOperand(_reader);
		    case OperandType.InlineVar:
		    case OperandType.InlineArg:
			    return new UInt16Operand(_reader);
		    case OperandType.InlineSig:
		    case OperandType.InlineString:
		    case OperandType.InlineTok:
		    case OperandType.InlineType:
		    case OperandType.InlineMethod:
		    case OperandType.InlineField:
			    return new UInt32Operand(_reader);
		    case OperandType.InlineNone:
		    case OperandType.InlinePhi:
		    default:
			    throw new NotSupportedException();
	    }
    }

    private void ResolveBranches()
    {
	    foreach (var instruction in _instructionsByOffset)
	    {
		    if (instruction is null) continue;
		    switch (instruction.Operand)
		    {
			    case InlineSwitchOperand inlineSwitchOperand:
				    inlineSwitchOperand.ResolveBranches(_instructionsByOffset);
				    break;
			    case ShortInlineBrTargetOperand shortInlineBrTargetOperand:
				    shortInlineBrTargetOperand.ResolveBranches(_instructionsByOffset);
				    break;
			    case InlineBrTargetOperand inlineBrTargetOperand:
				    inlineBrTargetOperand.ResolveInstruction(_instructionsByOffset);
				    break;
		    }   
	    }
    }
    
    public void WriteToStream(Stream stream)
    {
	    foreach (var instruction in Instructions)
	    {
		    instruction.WriteToStream(stream);
	    }
    }

    public Instruction GetInstructionAtOffset(int offset)
    {
	    return _instructionsByOffset[offset] ?? throw new InvalidOperationException();
    }
    
}
*/