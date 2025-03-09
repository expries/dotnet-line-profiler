using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using Profiler.Core;


namespace Profiler.Sample.IL;



public class InstructionBuilder
{
    private readonly List<Instruction> _instructions = new List<Instruction>();
    
    public InstructionBuilder AddMethodCall(UIntPtr methodPointer, MdSignature methodSignature, int arrayLength, uint localIndex)
    {
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = localIndex}
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I4,
            Operand = new Int32Operand { Value = arrayLength}
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldc_I8,
            Operand = new UInt64Operand {Value = methodPointer}
        });
        
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Calli,
            Operand = new Int32Operand { Value = methodSignature.Value }
        });


        return this;
    }
    
    public InstructionBuilder CreateArray(MdTypeRef typeRef, int size, uint localIndex)
    {
        return CreateNewArray(typeRef, size).InitializePointer(typeRef, localIndex);
    }
    

    public InstructionBuilder WriteTrueToArray(int index, uint localIndex)
    {
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc,
            Operand = new UInt32Operand { Value = localIndex}
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

    public List<Instruction> Build()
    {

        return _instructions;
    }


    private InstructionBuilder CreateNewArray(MdTypeRef elementTypeRef, int size)
    {

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
        
        /*
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Stloc_0,
        });
        */

        return this;
    }

    
    private InstructionBuilder InitializePointer(MdTypeRef elementTypeRef, uint localIndex)
    {
        /*
        _instructions.Add(new Instruction
        {
            OpCode = OpCodes.Ldloc_0,
        });
        */
        
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
            Operand = new UInt32Operand() { Value = localIndex }
        });

        return this;
    }
} 
