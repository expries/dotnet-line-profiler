using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using Profiler.Core;

namespace LineProfiler.IL;



public class InstructionBuilder
{
    private readonly List<Instruction> _instructions = new();
    
    public InstructionBuilder CreateArray(MdTypeRef elementTypeRef, int size, uint localVarCount)
    {
        var arrayPtrLocalVarIndex = localVarCount - 2;
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand
            {
                Value = size
            }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Newarr,
            Operand = new Int32Operand
            {
                Value = elementTypeRef.Value
            }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4_0,
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldelema,
            Operand = new Int32Operand { Value = elementTypeRef.Value }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Conv_U,
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand() { Value = arrayPtrLocalVarIndex }
        });

        return this;
    }
    

    public InstructionBuilder WriteTrueToArray(int index, uint localVarCount)
    {
        var arrayPtrLocalVarIndex = localVarCount - 2;
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = arrayPtrLocalVarIndex }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand() { Value = index }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Add,
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4_1,
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stind_I1,
        });

        return this;
    }
    
    public InstructionBuilder AddMethodCall(
        UIntPtr methodPointer, 
        InstrumentationMetadata metadata,
        uint localVarCount,
        int arrayLength, 
        int assemblyId, 
        int functionId)
    {
        var arrayPtrLocalVarIndex = localVarCount - 2;
        var traceIdPtrLocalVarIndex = localVarCount - 1;
        /*


var callGetTraceId = new Instruction
{
    OpCode = OpCodes.Call,
    Operand = new Int32Operand() { Value = metadata.GetTraceId.Value }
};

var storeToTraceIdLocalVar = new Instruction
{
    OpCode = OpCodes.Stloc,
    Operand = new UInt32Operand { Value = traceIdPtrLocalVarIndex }
};


// Get current activity
_instructions.Add(new Instruction
{
    OpCode = OpCodes.Call,
    Operand = new Int32Operand() { Value = metadata.GetCurrentActivity.Value }
});

// If activity is not null, get the trace id
_instructions.Add(new Instruction
{
    OpCode = OpCodes.Brtrue,
    Operand = new InlineBrTargetOperand { Value = 0, Instruction = callGetTraceId },
});

// Otherwise load null
_instructions.Add(new Instruction
{
    OpCode = OpCodes.Ldnull,
});

// branch to store null in the trace id ptr variable
_instructions.Add(new Instruction
{
    OpCode = OpCodes.Br,
    Operand = new InlineBrTargetOperand { Value = 0, Instruction = storeToTraceIdLocalVar },
});

// Get trace id
_instructions.Add(callGetTraceId);

// Convert trace id to string
_instructions.Add(new Instruction
{
    OpCode = OpCodes.Call,
    Operand = new Int32Operand() { Value = metadata.TraceIdToHexString.Value }
});

// Get char pointer to trace id
_instructions.Add(new Instruction
{
    OpCode = OpCodes.Conv_U,
});

// Store char pointer in trace id ptr variable
_instructions.Add(storeToTraceIdLocalVar);
*/
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldnull,
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Conv_U,
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand() { Value = traceIdPtrLocalVarIndex }
        });
        
        
        // Call probe method
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = arrayPtrLocalVarIndex }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand { Value = arrayLength}
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand { Value = assemblyId }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand { Value = functionId }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand() { Value = traceIdPtrLocalVarIndex }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new UInt64Operand {Value = methodPointer}
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Calli,
            Operand = new Int32Operand { Value = metadata.CallbackSignature.Value }
        });

        return this;
    }

    public List<Instruction> Build()
    {

        return _instructions;
    }
} 
