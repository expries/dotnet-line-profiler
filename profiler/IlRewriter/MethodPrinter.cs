using System.Runtime.InteropServices;
using IlRewriter.Models;
using IlRewriter.Models.Operands;

namespace IlRewriter;

public static class MethodPrinter
{
    public static void PrintMethodBody(MethodBody body)
    {
        foreach (var instruction in body.Instructions)
        {
            PrintInstruction(instruction);
            Console.WriteLine();
        }
    }


    public static void PrintHeader(MethodHeader header)
    {
        Console.WriteLine();
        Console.WriteLine("Method Header Information:");
        Console.WriteLine($"Size: {header.Size}");
        Console.WriteLine($"SizeInBytes: {header.SizeInBytes}");
        Console.WriteLine($"MaxStack: {header.MaxStack}");
        Console.WriteLine($"CodeSize: {header.CodeSize}");
        Console.WriteLine($"LocalVarSigToken: {header.LocalVarSigToken}");
        Console.WriteLine($"MoreSections: {header.MoreSections}");
        Console.WriteLine($"InitLocals: {header.InitLocals}");
        Console.WriteLine($"IsTiny: {header.IsTiny}");
        Console.WriteLine($"IsFat: {header.IsFat}");
        Console.WriteLine();
    }

    private static void PrintInstruction(Instruction instruction)
    {
        Console.Write($"{instruction.Offset}: {instruction.OpCode.Name}");
        if (instruction.Operand != null)
        {
            PrintOperand(instruction.Operand);
        }
    }

    public static void PrintOperand(Operand operand)
    {
        switch (operand)
        {
            case ByteOperand byteOperand:
                Console.Write($" ({byteOperand.Value})");
                break;
            case DoubleOperand doubleOperand:
                Console.Write($" ({doubleOperand.Value})");
                break;
            case InlineBrTargetOperand inlineBrTargetOperand:
                Console.Write($" ({inlineBrTargetOperand.Value})");
                break;
            case InlineSwitchOperand switchOperand:
                Console.Write($" ({switchOperand.BaseOffset}: {string.Join(',',switchOperand.BranchOffsets)})");
                break;
            case Int16Operand int16Operand:
                Console.Write($" ({int16Operand.Value})");
                break;
            case Int32Operand int32Operand:
                Console.Write($" ({int32Operand.Value})");
                break;
            case Int64Operand int64Operand:
                Console.Write($" ({int64Operand.Value})");
                break;
            case SByteOperand sByteOperand:
                Console.Write($" ({sByteOperand.Value})");
                break;
            case ShortInlineBrTargetOperand shortInlineBrTargetOperand:
                Console.Write($" ({shortInlineBrTargetOperand.Value})");
                break;
            case ShortInlineIOperand shortInlineIOperand:
                Console.Write($" ({shortInlineIOperand.Value})");
                break;
            case SingleOperand singleOperand:
                Console.Write($" ({singleOperand.Value})");
                break;
            case UInt16Operand uInt16Operand:
                Console.Write($" ({uInt16Operand.Value})");
                break;
            case UInt32Operand uInt32Operand:
                Console.Write($" ({uInt32Operand.Value})");
                break;
            case UInt64Operand uInt64Operand:
                Console.Write($" ({uInt64Operand.Value})");
                break;
            default:
                throw new NotImplementedException();
        }
    }
}


