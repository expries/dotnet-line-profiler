using System.Runtime.InteropServices;
using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using LineProfiler.Caches;
using LineProfiler.Instrumentation.Metadata;
using Profiler.Core;

namespace LineProfiler.Instrumentation;

public class InstructionBuilder
{
    private readonly List<Instruction> _instructions = new();

    public InstructionBuilder CreateArray(MdTypeRef elementTypeRef, int size, uint localVarCount)
    {
        var arrayPtrLocalVarIndex = localVarCount - 1;

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
        var arrayPtrLocalVarIndex = localVarCount - 1;

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

    public unsafe InstructionBuilder AddMethodCall(
        InstrumentationMetadata metadata,
        int arrayLength)
    {
        var localVarCount = metadata.LocalVarCount;
        var arrayPtrLocalVarIndex = localVarCount - 1;
        
        // Construct method call
        // Load method arguments (Note: The order and type of these has to match the method's signature)
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new Int64Operand { Value = metadata.FunctionToken.Value }
        });
        
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
            Operand = new Int64Operand { Value = GCHandle.ToIntPtr(CallbackCache.Instance.Handle) }
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new Int64Operand { Value = GCHandle.ToIntPtr(CallbackCache.Instance.SizeHandle) }
        });

        // Call method
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Call,
            Operand = new Int32Operand { Value = metadata.CallbackRef.Value }
        });

        return this;
    }

    public List<Instruction> Build()
    {
        return _instructions;
    }
}