using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using LineProfiler.Models;
using Profiler.Core;

namespace LineProfiler.Instrumentation;



public class InstructionBuilder
{
    private readonly List<Instruction> _instructions = new();
    
    public InstructionBuilder CreateArray(MdTypeRef elementTypeRef, int size, uint localVarCount)
    {
        var arrayPtrLocalVarIndex = localVarCount - 4;
        
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
        var arrayPtrLocalVarIndex = localVarCount - 4;
        
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
        int arrayLength)
    {
        var arrayPtrLocalVarIndex = localVarCount - 4;
        var activityLocalVarIndex = localVarCount - 3;
        var traceIdLocalVarIndex = localVarCount - 2;
        var traceIdPtrLocalVarIndex = localVarCount - 1;

        
        var activityNotNull = new Instruction()
        {
            OpCode = OpCodes.Nop
        };


        var end = new Instruction()
        {
            OpCode = OpCodes.Nop
        };
        

        // Get current activity
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Call,
            Operand = new Int32Operand() { Value = metadata.GetCurrentActivity.Value }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand() { Value = activityLocalVarIndex }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand() { Value = activityLocalVarIndex }
        });

        // If activity is not null, get the trace id
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Brtrue,
            Operand = new InlineBrTargetOperand { Value = 0, Instruction = activityNotNull },
        });
        
        
        // branch to store null in the trace id ptr variable
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
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Br,
            Operand = new InlineBrTargetOperand { Value = 0, Instruction = end },
        });
        

        
        
        _instructions.Add(activityNotNull);
        
        
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand() { Value = activityLocalVarIndex }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Call,
            Operand = new Int32Operand() { Value = metadata.GetTraceId.Value }
        });
        

        
        _instructions.Add(new Instruction()
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand() { Value = traceIdLocalVarIndex }
        });
        
        // Convert trace id to string

        _instructions.Add(new Instruction()
        {
            OpCode = OpCodes.Ldloca,
            Operand = new UInt32Operand() { Value = traceIdLocalVarIndex }
        });
        
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Call,
            Operand = new Int32Operand() { Value = metadata.TraceIdToHexString.Value }
        });
        
        // Convert trace id string to char pointer

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand() { Value = traceIdPtrLocalVarIndex }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand() { Value = traceIdPtrLocalVarIndex }
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
        
        
        _instructions.Add(end);
        
        
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
            OpCode = OpCodes.Ldc_I8,
            Operand = new Int64Operand() { Value = metadata.AssemblyId.Value }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new Int64Operand { Value = metadata.FunctionId.Value }
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
