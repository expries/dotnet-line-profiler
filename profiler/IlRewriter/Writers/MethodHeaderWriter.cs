using System.Text.Json;
using IlRewriter.Models;

namespace IlRewriter.Writers;

public class MethodHeaderWriter
{
    public void WriteToStream(MethodHeader header, BinaryStreamWriter writer)
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
        
        if (header.IsTiny)
        {
            
            WriteTinyHeader(header, writer);
        }
        else
        {
            WriteFatHeader(header, writer);
        }
    }
    
    private void WriteTinyHeader(MethodHeader header, BinaryStreamWriter writer)
    {
        var headerByte = (byte)((header.CodeSize << 2) | 0x2);
        writer.WriteByte(headerByte);
    }
    
    private void WriteFatHeader(MethodHeader header, BinaryStreamWriter writer)
    {
        ushort flags = 0;
        
        flags |= 0x3;
        if (header.MoreSections) flags |= 0x8;
        if (header.InitLocals) flags |= 0x10;

        var firstWord = (ushort) ((header.Size << 12) | flags);
        
        writer.WriteUInt16(firstWord);
        
        writer.WriteUInt16(header.MaxStack);
        writer.WriteInt32(header.CodeSize);
        writer.WriteUInt32(header.LocalVarSigToken);
    }
}