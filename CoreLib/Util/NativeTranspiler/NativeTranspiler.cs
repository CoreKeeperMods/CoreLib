using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Iced.Intel;
using Il2CppInterop.Common;
using Decoder = Iced.Intel.Decoder;

namespace CoreLib.Util;

public static class NativeTranspiler
{
    public static ToggleSwitch printDebugInfo = new ToggleSwitch();

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern bool VirtualProtect(IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

    private static bool VirtualProtect(IntPtr lpAdress, IntPtr dwSize, ProtectMode flNewProtect, out ProtectMode lpflOldProtect)
    {
        bool result = VirtualProtect(lpAdress, dwSize, (uint)flNewProtect, out uint oldProtect);
        lpflOldProtect = (ProtectMode)oldProtect;
        return result;
    }
    
    private static unsafe Decoder GetDecoder(IntPtr codeStart, int capacity = 2000)
    {
        var stream = new UnmanagedMemoryStream((byte*)codeStart, capacity, capacity, FileAccess.ReadWrite);
        var codeReader = new StreamCodeReader(stream);
        var decoder = Decoder.Create(IntPtr.Size * 8, codeReader);
        decoder.IP = (ulong)codeStart;
        return decoder;
    }

    private static void PrintInstructions(List<Instruction> instructions) {

        var formatter = new NasmFormatter();
        var output = new StringOutput();
        foreach (Instruction instr in instructions)
        {
            formatter.Format(instr, output);
            CoreLibPlugin.Logger.LogDebug($"{instr.IP:X16} {output.ToStringAndReset()}");
        }
    }
    
    private static void PrintMethodCodeFromMemory(IntPtr codeStart, int capacity = 2000)
    {
        Decoder decoder = GetDecoder(codeStart, capacity);

        var formatter = new NasmFormatter();
        var output = new StringOutput();
        decoder.IP = (ulong)codeStart;
        while (true)
        {
            decoder.Decode(out Instruction instr);
            if (decoder.LastError == DecoderError.NoMoreBytes)
            {
                break;
            }

            formatter.Format(instr, output);
            CoreLibPlugin.Logger.LogDebug($"{instr.IP:X16} {output.ToStringAndReset()}");

            if (instr.FlowControl == FlowControl.Return)
                break;
        }
    }
    
    internal static MethodBase GetOriginalMethod(this HarmonyMethod attr)
    {
        try
        {
            switch (attr.methodType)
            {
                case MethodType.Normal:
                    if (attr.methodName is null)
                        return null;
                    return AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes);

                case MethodType.Getter:
                    if (attr.methodName is null)
                        return null;
                    return AccessTools.DeclaredProperty(attr.declaringType, attr.methodName).GetGetMethod(true);

                case MethodType.Setter:
                    if (attr.methodName is null)
                        return null;
                    return AccessTools.DeclaredProperty(attr.declaringType, attr.methodName).GetSetMethod(true);

                case MethodType.Constructor:
                    return AccessTools.DeclaredConstructor(attr.declaringType, attr.argumentTypes);

                case MethodType.StaticConstructor:
                    return AccessTools
                        .GetDeclaredConstructors(attr.declaringType)
                        .FirstOrDefault(c => c.IsStatic);

                case MethodType.Enumerator:
                    if (attr.methodName is null)
                        return null;
                    return AccessTools.EnumeratorMoveNext(AccessTools.DeclaredMethod(attr.declaringType,
                        attr.methodName, attr.argumentTypes));
            }
        }
        catch (AmbiguousMatchException ex)
        {
            throw new AmbiguousMatchException($"Failed to patch method {attr.Description()}", ex.InnerException ?? ex);
        }

        return null;
    }
    
    private static string Description(this HarmonyMethod method) => "(class=" + ((object) method.declaringType != null ? method.declaringType.FullDescription() : "undefined") + ", methodname=" + (method.methodName ?? "undefined") + ", type=" + (method.methodType.HasValue ? method.methodType.Value.ToString() : "undefined") + ", args=" + (method.argumentTypes != null ? method.argumentTypes.Description() : "undefined") + ")";
    
    public static void PatchAll(Type type)
    {
        MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Where(info =>
        {
            NativeTranspilerPatchAttribute attribute = info.GetCustomAttribute<NativeTranspilerPatchAttribute>();
            return attribute != null;
        }).ToArray();

        foreach (MethodInfo method in methods)
        {
            NativeTranspilerPatchAttribute attribute = method.GetCustomAttribute<NativeTranspilerPatchAttribute>();
            MethodBase original = attribute.info.GetOriginalMethod();
            if (original is null)
            {
                CoreLibPlugin.Logger.LogError($"Undefined target method for patch method {method.FullDescription()}");
                continue;
            }

            var traspiler = method.CreateDelegate<Func<List<Instruction>, List<Instruction>>>();
            PatchMethod(original, traspiler, attribute.capacity);

        }
    }
    
    public static unsafe void PatchMethod(MethodBase methodInfo, Func<List<Instruction>, List<Instruction>> transpiler, int capacity = 2000)
    {
        var fieldValue = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(methodInfo)?.GetValue(null);
        if (fieldValue == null)
        {
            CoreLibPlugin.Logger.LogError($"Failed to patch {methodInfo.FullDescription()}: failed to find native method pointer!");
            return;
        }
        
        IntPtr codeStart = *(IntPtr*)(IntPtr)fieldValue;

        Decoder decoder = GetDecoder(codeStart, capacity);

        List<Instruction> instructions = new List<Instruction>(capacity);

        while (true)
        {
            decoder.Decode(out Instruction instr);
            if (decoder.LastError == DecoderError.NoMoreBytes)
            {
                CoreLibPlugin.Logger.LogError($"Failed to patch method {methodInfo.FullDescription()}: not enough capacity to fully get method instructions!");
                return;
            }
            instructions.Add(instr);
            
            if (instr.FlowControl == FlowControl.Return)
                break;
        }

        IntPtr methodSizeInBytes = (IntPtr)(decoder.IP - (ulong)codeStart);

        IntPtr methodSize = (IntPtr)instructions.Count;

        if (printDebugInfo.Value)
        {
            CoreLibPlugin.Logger.LogDebug($"Original method {methodInfo.FullDescription()} body:");
            PrintInstructions(instructions);
        }

        CoreLibPlugin.Logger.LogDebug($"Patching {methodInfo.FullDescription()}");
        instructions = transpiler(instructions);

        if (instructions.Count != (int)methodSize)
        {
            CoreLibPlugin.Logger.LogError($"Failed to patch method {methodInfo.FullDescription()}: resulting code is longer or shorter than original. This is not supported!");
            return;
        }
        
        if (VirtualProtect(codeStart, methodSizeInBytes, ProtectMode.PAGE_EXECUTE_READWRITE, out ProtectMode oldProtect))
        {
            var codeWriter = new MemoryCodeWriter(codeStart);
        
            var block = new InstructionBlock(codeWriter, instructions, (ulong)codeStart);
            bool success = BlockEncoder.TryEncode(decoder.Bitness, block, out var errorMessage, out _);
            if (!success) {
                CoreLibPlugin.Logger.LogError($"Error patching method {methodInfo.FullDescription()}: {errorMessage}");
                return;
            }
            
            VirtualProtect(codeStart, methodSizeInBytes, oldProtect, out ProtectMode _);
            
            if (printDebugInfo.Value)
            {
                CoreLibPlugin.Logger.LogDebug($"Result method {methodInfo.FullDescription()} body:");
                PrintMethodCodeFromMemory(codeStart, capacity);
            }
        }
        else
        {
            CoreLibPlugin.Logger.LogError("Failed to patch method {methodInfo.Name}: failed to change protection!");
        }
    }
}