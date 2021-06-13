# ClrMDExports

Helpers to easily create WinDbg extensions using ClrMD.

Create a new class library project, reference the nuget package, then add the `DllExport` attribute to the functions you want to export to WinDbg. Then call `DebuggingContext.Execute` with a callback, to be able to use ClrMD.

**Note: you need to compile your project for x86 or x64 depending on the bitness of your target. AnyCPU is not supported.**

```C#
using System;
using System.Linq;
using System.Runtime.InteropServices;
using ClrMDExports;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace TestWindbgExtension
{
    public class Class1
    {
        [DllExport("helloworld")]
        public static void HelloWorld(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            // Can't use ClrMD here. Use DebuggingContext.Execute with a callback to switch to another context:
            DebuggingContext.Execute(client, args, HelloWorld);
        }

        private static void HelloWorld(ClrRuntime runtime, string args)
        {
            // Can use ClrMD here
            Console.WriteLine("The first 10 types on the heap are: ");

            foreach (var obj in runtime.Heap.EnumerateObjects().Take(10))
            {
                Console.WriteLine(obj.Type);
            }
        }
    }
}

```

