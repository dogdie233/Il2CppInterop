using System;
using System.Linq;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime.Runtime;
using Microsoft.Extensions.Logging;

namespace Il2CppInterop.Runtime.Injection.Hooks;

internal class GarbageCollector_RunFinalizer_Patch : Hook<GarbageCollector_RunFinalizer_Patch.MethodDelegate>
{
    public override string TargetMethodName => "GarbageCollector::RunFinalizer";
    public override MethodDelegate GetDetour() => Hook;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MethodDelegate(IntPtr obj, IntPtr data);

    private void Hook(IntPtr obj, IntPtr data)
    {
        unsafe
        {
            var nativeClassStruct = UnityVersionHandler.Wrap((Il2CppClass*)IL2CPP.il2cpp_object_get_class(obj));
            if (nativeClassStruct.HasFinalize)
            {
                Original(obj, data);
            }
        }
        Il2CppObjectPool.Remove(obj);
    }

    private static readonly MemoryUtils.SignatureDefinition[] s_signatures =
    {
        new()
        {
            pattern = "H\u0089\\$\u0010H\u0089t$\u0018WH\u0083ì H\u008b\u0019",
            mask = "xxxxxxxxxxxxxxxxxx",
            xref = false
        }
    };

    public override IntPtr FindTargetMethod()
    {
        return s_signatures
            .Select(s => MemoryUtils.FindSignatureInModule(InjectorHelpers.Il2CppModule, s))
            .FirstOrDefault(p => p != 0);
    }

    public override void TargetMethodNotFound()
    {
        Il2CppObjectPool.DisableCaching = true;
        Logger.Instance.LogWarning("{MethodName} not found, disabling Il2CppObjectPool", TargetMethodName);
    }
}
