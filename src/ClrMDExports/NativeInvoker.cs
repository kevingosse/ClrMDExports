using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ClrMDExports
{
    internal class NativeInvoker : DispatchProxy
    {
        private IntPtr _address;
        private Type _interfaceType;
        private string[] _vtable;

        public static T GetInstance<T>(IntPtr address, string[] vtable = null)
        {
            var proxy = DispatchProxy.Create<T, NativeInvoker>();

            if (vtable == null)
            {
                vtable = typeof(T).GetMethods()
                    .Select(m => m.Name)
                    .ToArray();
            }

            (proxy as NativeInvoker).Configure(typeof(T), address, vtable);

            return proxy;
        }

        internal void Configure(Type interfaceType, IntPtr address, string[] vtable)
        {
            _interfaceType = interfaceType;
            _address = address;
            _vtable = vtable;
        }


        protected override unsafe object Invoke(MethodInfo targetMethod, object[] args)
        {
            var delegateType = DelegateBuilder.GenerateDelegateType(targetMethod);

            var vtable = *(long**)_address;

            int offset = -1;

            for (int i = 0; i < _vtable.Length; i++)
            {
                if (_vtable[i] == targetMethod.Name)
                {
                    offset = i;
                    break;
                }
            }

            if (offset == -1)
            {
                Console.WriteLine("Could not find {0} in the vtable for {1}", targetMethod.Name, _interfaceType.Name);
            }

            var func = Marshal.GetDelegateForFunctionPointer(new IntPtr(vtable[offset]), delegateType);

            var argsWithSelf = new object[args.Length + 1];

            argsWithSelf[0] = _address;

            for (int i = 0; i < args.Length; i++)
            {
                argsWithSelf[i + 1] = args[i];
            }

            var result = func.DynamicInvoke(argsWithSelf);

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = argsWithSelf[i + 1];
            }

            return result;
        }
    }
}