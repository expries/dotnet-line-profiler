using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;

namespace IlRewriter.Models;


public class Instruction
{
    public int Offset { get; set; }
    public int EndOffset => Offset + SizeInBytes;
    public OpCode OpCode { get; set; }
    public Operand? Operand { get; set; } 
    
    public bool IsUnconditionalJump => OpCode == OpCodes.OpCodes.Br ||
                                       OpCode == OpCodes.OpCodes.Br_S ||
                                       OpCode == OpCodes.OpCodes.Leave ||
                                       OpCode == OpCodes.OpCodes.Leave_S;

    public bool IsJump => IsUnconditionalJump ||
                          OpCode == OpCodes.OpCodes.Beq ||
                          OpCode == OpCodes.OpCodes.Beq_S || OpCode == OpCodes.OpCodes.Bne_Un ||
                          OpCode == OpCodes.OpCodes.Bne_Un_S || OpCode == OpCodes.OpCodes.Bge ||
                          OpCode == OpCodes.OpCodes.Bge_S || OpCode == OpCodes.OpCodes.Bgt ||
                          OpCode == OpCodes.OpCodes.Bgt_S || OpCode == OpCodes.OpCodes.Ble ||
                          OpCode == OpCodes.OpCodes.Ble_S || OpCode == OpCodes.OpCodes.Blt ||
                          OpCode == OpCodes.OpCodes.Blt_S || OpCode == OpCodes.OpCodes.Bge_Un ||
                          OpCode == OpCodes.OpCodes.Bge_Un_S || OpCode == OpCodes.OpCodes.Bgt_Un ||
                          OpCode == OpCodes.OpCodes.Bgt_Un_S || OpCode == OpCodes.OpCodes.Ble_Un ||
                          OpCode == OpCodes.OpCodes.Ble_Un_S || OpCode == OpCodes.OpCodes.Ble_Un_S ||
                          OpCode == OpCodes.OpCodes.Brfalse || OpCode == OpCodes.OpCodes.Brfalse_S ||
                          OpCode == OpCodes.OpCodes.Brtrue || OpCode == OpCodes.OpCodes.Brtrue_S || 
                          OpCode == OpCodes.OpCodes.Switch;
    
    public int SizeInBytes
    {
        get
        {
            var opCodeSize = OpCode.SizeInBytes;
            var operandSize = Operand?.SizeInBytes ?? 0;
            return opCodeSize + operandSize;
        }
    }
}