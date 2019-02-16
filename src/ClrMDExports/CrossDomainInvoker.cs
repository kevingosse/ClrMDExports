using System;
using Microsoft.Diagnostics.Runtime;

namespace ClrMDExports
{
    [Serializable]
    public delegate void DebuggingMethod(ClrRuntime runtime, string args);

    [Serializable]
    public class CrossDomainInvoker
    {
        private readonly IntPtr _client;
        private readonly string _args;
        private readonly DebuggingMethod _callback;

        public CrossDomainInvoker(IntPtr client, string args, DebuggingMethod callback)
        {
            _client = client;
            _args = args;
            _callback = callback;
        }

        public void Invoke()
        {
            DebuggingContext.Initialize(_client, true);

            _callback(DebuggingContext.Runtime, _args);
        }
    }
}