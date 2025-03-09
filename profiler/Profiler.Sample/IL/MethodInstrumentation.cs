using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using Profiler.Core;
using Profiler.Sample.Interception;

namespace Profiler.Sample.IL;

public static class MethodInstrumentation
{
    public static Method PatchMethod(Method method, MdTypeRef boolTypeRef, uint localIndex, MdSignature localSignature, MdSignature probeSignature)
    {
        AddProbeAt(0, method, boolTypeRef, localIndex, localSignature, probeSignature);
        return method;
    }
    
    private static void AddProbeAt(int index, Method method, MdTypeRef boolTypeRef, uint localIndex, MdSignature localSignature, MdSignature probeSignature)
    {
        unsafe
        {
            var returnStatementCount = method.Body.Instructions.Count(i => i.OpCode == OpCodes.Ret);
            var arrayLength = returnStatementCount;
            
            var callbackPointer = new UIntPtr(Callbacks.ProbePointer);

            var init = new InstructionBuilder().CreateArray(boolTypeRef, arrayLength, localIndex).Build();
            method.Body.Instructions.InsertRange(0, init);
            
            var instructionsCopy = method.Body.Instructions.ToList();
            var newInstructionCount = 0;
            
            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instruction = method.Body.Instructions[i];
                if (instruction.OpCode != OpCodes.Ret) continue;
                
                var targetIndex = i + newInstructionCount;
                
                var newInstructions = new InstructionBuilder()
                    .WriteTrueToArray(newInstructionCount, localIndex)
                    .AddMethodCall(callbackPointer, probeSignature, arrayLength, localIndex)
                    .Build();
                
                instructionsCopy.InsertRange(targetIndex, newInstructions);
                newInstructionCount++;
            }
            
            method.Body.Instructions = instructionsCopy;
            method.Header.MaxStack = (ushort) (method.Header.MaxStack + 3);
            method.Header.LocalVarSigToken = (uint) localSignature.Value;
        }
    }
}

