// See https://aka.ms/new-console-template for more information

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Mono.Cecil;
using PdbReader;
using PdbReader.DebugInfo;

var pdb = new Pdb(@"C:\Coding\CodeLevelTracing\SampleConsoleApp\obj\Debug\net8.0\SampleConsoleApp.pdb");
pdb.Print();

var assembly = AssemblyDefinition.ReadAssembly(@"C:\Coding\CodeLevelTracing\SampleConsoleApp\obj\Debug\net8.0\SampleConsoleApp.dll");
foreach (var assemblyReference in assembly.MainModule.AssemblyReferences)
{
    Console.WriteLine("Assembly");
    Console.WriteLine(assemblyReference.FullName);
    
    var resolved = assembly.MainModule.AssemblyResolver.Resolve(assemblyReference);
    Console.WriteLine("Assembly types");
    foreach (var assemblyType in resolved.MainModule.Types)
    {
        Console.WriteLine(assemblyType.FullName);
    }
}

Console.WriteLine("Types in main module");
foreach (var type in assembly.MainModule.Types)
{
    Console.WriteLine(type.FullName);
}

Console.WriteLine("Type references in main module");
foreach (var typeReference in assembly.MainModule.GetTypeReferences())
{
    Console.WriteLine(typeReference.FullName);
    var token = typeReference.MetadataToken.ToUInt32();
    var tokenType = typeReference.MetadataToken.TokenType;
    Console.WriteLine($"{token} {(tokenType)}");

}

