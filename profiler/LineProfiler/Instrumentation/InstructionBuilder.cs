using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using LineProfiler.Instrumentation.Metadata;
using Profiler.Core;

namespace LineProfiler.Instrumentation;

public class InstructionBuilder
{
    private readonly List<Instruction> _instructions = new();

    public InstructionBuilder CreateArray(MdTypeRef elementTypeRef, int size, uint localVarCount)
    {
        var arrayPtrLocalVarIndex = localVarCount - 4;

        // Create array of given size
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

        // Get address of first element
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4_0
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldelema,
            Operand = new Int32Operand { Value = elementTypeRef.Value }
        });

        // Convert first element's address to bool*
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Conv_U
        });

        // Store pointer to array (bool*) in local variable
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand { Value = arrayPtrLocalVarIndex }
        });

        return this;
    }


    public InstructionBuilder WriteTrueToArray(int index, uint localVarCount)
    {
        var arrayPtrLocalVarIndex = localVarCount - 4;

        // Load pointer to array (bool+)
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = arrayPtrLocalVarIndex }
        });

        // Add the index to the pointer of the array to get the pointer to the element at the given index 
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand { Value = index }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Add
        });

        // Write true to the array element at index
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4_1
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stind_I1
        });

        return this;
    }

    public InstructionBuilder AddMethodCall(
        UIntPtr methodPointer,
        InstrumentationMetadata metadata,
        int arrayLength)
    {
        var localVarCount = metadata.LocalVarCount;
        var arrayPtrLocalVarIndex = localVarCount - 4;
        var activityLocalVarIndex = localVarCount - 3;
        var traceIdLocalVarIndex = localVarCount - 2;
        var traceIdPtrLocalVarIndex = localVarCount - 1;

        // Instructions to branch to
        var beforeReadTraceId = new Instruction
        {
            OpCode = OpCodes.Nop
        };

        var beforeMethodCall = new Instruction
        {
            OpCode = OpCodes.Nop
        };

        // Get current activity
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Call,
            Operand = new Int32Operand { Value = metadata.ActivityType.Current.Value }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand { Value = activityLocalVarIndex }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = activityLocalVarIndex }
        });

        // If current activity is set, jump to reading its trace id
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Brtrue,
            Operand = new InlineBrTargetOperand { Value = 0, Instruction = beforeReadTraceId }
        });


        // If current activity is null, store null in the trace id char* variable
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldnull
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Conv_U
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand { Value = traceIdPtrLocalVarIndex }
        });

        // Jump to method call
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Br,
            Operand = new InlineBrTargetOperand { Value = 0, Instruction = beforeMethodCall }
        });

        _instructions.Add(beforeReadTraceId);

        // Read trace id from current activity 
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = activityLocalVarIndex }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Call,
            Operand = new Int32Operand { Value = metadata.ActivityType.GetTraceId.Value }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand { Value = traceIdLocalVarIndex }
        });

        // Convert trace id to string
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloca,
            Operand = new UInt32Operand { Value = traceIdLocalVarIndex }
        });


        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Call,
            Operand = new Int32Operand { Value = metadata.TraceIdType.ToHexString.Value }
        });

        // Convert trace id string to char*
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand { Value = traceIdPtrLocalVarIndex }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = traceIdPtrLocalVarIndex }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Conv_U
        });

        // Store trace id char* in variable
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc,
            Operand = new UInt32Operand { Value = traceIdPtrLocalVarIndex }
        });

        _instructions.Add(beforeMethodCall);


        // Construct method call
        // Load method arguments (Note: The order and type of these has to match the method's signature)
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = arrayPtrLocalVarIndex }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand { Value = arrayLength }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new Int64Operand { Value = metadata.AssemblyId.Value }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new Int64Operand { Value = metadata.FunctionId.Value }
        });

        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = traceIdPtrLocalVarIndex }
        });

        // Load method pointer
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new UInt64Operand { Value = methodPointer }
        });

        // Call method
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Calli,
            Operand = new Int32Operand { Value = metadata.SignatureTokens.Callback.Value }
        });

        return this;
    }

    public List<Instruction> Build()
    {
        return _instructions;
    }
}