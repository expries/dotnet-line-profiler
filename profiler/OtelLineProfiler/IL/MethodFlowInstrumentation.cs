using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using OtelLineProfiler.Interception;
using Profiler.Core;

namespace OtelLineProfiler.IL;

public static class MethodFlowInstrumentation
{
    public static byte[] ProbeSignature = MethodSignatureFactory.Create(
        CorUnmanagedCallingConventionFlags.IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL, 
        CorElementTypeFlags.ELEMENT_TYPE_VOID, 2, 
        CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN, 
        CorElementTypeFlags.ELEMENT_TYPE_I4);
    
    public static LocalSignature PatchLocalVariableSignature(LocalSignature localSignature)
    {
        byte[] pinnedBoolArraySignature = [CorElementTypeFlags.ELEMENT_TYPE_PINNED, CorElementTypeFlags.ELEMENT_TYPE_SZARRAY, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        byte[] boolPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        localSignature.AddLocalVariable(pinnedBoolArraySignature);
        localSignature.AddLocalVariable(boolPtrSignature);
        return localSignature;
    }
    
    public static Method InstrumentMethod(Method method, uint localVarCount, MdTypeRef boolTypeRef, MdSignature localSignature, MdSignature probeSignature)
    {
        var localVarIndex = localVarCount - 1;
        AddProbeAt(method, boolTypeRef, localVarIndex, localSignature, probeSignature);
        return method;
    }
    
    private static void AddProbeAt(Method method, MdTypeRef boolTypeRef, uint localVarIndex, MdSignature localSignature, MdSignature probeSignature)
    {
        unsafe
        {
            var returnStatementCount = method.Body.Instructions.Count(i => i.OpCode == OpCodes.Ret);
            var arrayLength = returnStatementCount;
            
            var probePointer = new UIntPtr(Callbacks.ProbePointer);

            var init = new InstructionBuilder().CreateArray(boolTypeRef, arrayLength, localVarIndex).Build();
            method.Body.Instructions.InsertRange(0, init);
            
            var instructionsCopy = method.Body.Instructions.ToList();
            var newInstructionCount = 0;
            
            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instruction = method.Body.Instructions[i];
                if (instruction.OpCode != OpCodes.Ret) continue;
                
                var targetIndex = i + newInstructionCount;
                
                var newInstructions = new InstructionBuilder()
                    .WriteTrueToArray(newInstructionCount, localVarIndex)
                    .AddMethodCall(probePointer, probeSignature, arrayLength, localVarIndex)
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

