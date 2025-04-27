using FluentAssertions;
using IlRewriter.Readers.Method;
using IlRewriter.Writers;
using Xunit.Abstractions;

namespace ILRewriter.Tests;

public class MethodBodyTests : MethodTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public MethodBodyTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(AllIlMethodBodies))]
    public void MethodBody_CtorAndWriteToStream_ReturnsOriginalByteArray(params byte[] ilMethodBody)
    {
        var inputMemoryStream = new MemoryStream(ilMethodBody);
        var reader = new MethodReader(inputMemoryStream);
        var method = reader.ReadFromStream();
        
        var headerBytes = ilMethodBody[0..method.Header.SizeInBytes];
        var instructionBytes = ilMethodBody[method.Header.SizeInBytes..(method.Header.SizeInBytes+method.Header.CodeSize)];
        var dataSectionBytes = ilMethodBody[(method.Header.SizeInBytes+method.Header.CodeSize)..];

        // Depends on the implementation of MethodHeader
        var methodBodyOffset = method.Header.SizeInBytes;
        var methodBodyBytes = ilMethodBody[methodBodyOffset..];
        
        var outputMemoryStream = new MemoryStream();
        var writer = new MethodBodyWriter();
        writer.WriteToStream(method.Body, outputMemoryStream);
        
        var headerOutput = new MemoryStream();
        var binaryWriter = new BinaryStreamWriter(headerOutput);
        var instructionOutput = new MemoryStream();
        var dataSectionOutput = new MemoryStream();
        
        var headerWriter = new MethodHeaderWriter();
        headerWriter.WriteToStream(method.Header, binaryWriter);

        var instructionWriter = new InstructionWriter();
        method.Body.Instructions.ForEach(instruction => instructionWriter.WriteToStream(instruction, instructionOutput));

        var dataSectionWriter = new MethodDataSectionWriter();
        dataSectionWriter.WriteToStream(method.Body.DataSection, dataSectionOutput);
        
        CheckOutput("Header", headerBytes, headerOutput.ToArray());
        CheckOutput("Instructions", instructionBytes, instructionOutput.ToArray());
        CheckOutput("DataSection", dataSectionBytes, dataSectionOutput.ToArray());

        var serializedBytes = outputMemoryStream.ToArray();
        serializedBytes.Should().Equal(methodBodyBytes);
    }

    private void CheckOutput(string name, byte[] input, byte[] output)
    {
        var pass = output.SequenceEqual(input);
        if (pass) return;
        _testOutputHelper.WriteLine(name);
        _testOutputHelper.WriteLine($"Original: {BitConverter.ToString(input)}");
        _testOutputHelper.WriteLine($"Written:  {BitConverter.ToString(output)}");
    }

    [Theory]
    [MemberData(nameof(IlMethodBodiesWithExceptionHandlers))]
    public void MethodBody_Ctor_ParsesExceptionHandlers(params byte[] ilMethodBody)
    {
        var inputMemoryStream = new MemoryStream(ilMethodBody);
        var reader = new MethodReader(inputMemoryStream);
        var method = reader.ReadFromStream();

        method.Body.DataSection.Should().NotBeNull();
        method.Body.DataSection.EhSections.Should().NotBeEmpty();
        method.Body.DataSection.EhSections[0].Handlers.Should().NotBeEmpty();
    }
}