using System;
using System.Linq;
using System.Reflection;
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
        private readonly string _type;
        private readonly int _methodMetadataToken;

        public CrossDomainInvoker(IntPtr client, string args, string type, int methodMetadataToken)
        {
            _client = client;
            _args = args;
            _type = type;
            _methodMetadataToken = methodMetadataToken;
        }

        public void Invoke()
        {
            DebuggingContext.Initialize(_client, true);

            MethodInfo method = null;
            var type = Type.GetType(_type);

            if (type == null)
            {
                Console.WriteLine("Could not load type {0} in child AppDomain", _type);
                return;
            }

            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            method = methods.FirstOrDefault(m => m.MetadataToken == _methodMetadataToken);

            if (method == null)
            {
                Console.WriteLine("Could not find static method with metadata token {0} in type {1}", _methodMetadataToken, _type);
                return;
            }

            try
            {
                method.Invoke(null, new object[] { DebuggingContext.Runtime, _args });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while executing the command: " + ex);
            }
        }
    }
}