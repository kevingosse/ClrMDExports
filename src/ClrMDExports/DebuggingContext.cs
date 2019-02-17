using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;

namespace ClrMDExports
{
    public class DebuggingContext
    {
        private static AppDomain ChildDomain;

        public static bool IsWinDbg { get; set; }

        public static IDebugClient DebugClient { get; private set; }
        public static DataTarget DataTarget { get; private set; }
        public static ClrRuntime Runtime { get; private set; }

        private static readonly string[] vtable =
        {
            "QueryInterface",
            "AddRef",
            "Release",
            "GetCoreClrDirectory",
            "GetExpression",
            "VirtualUnwind",
            "SetExceptionCallback",
            "ClearExceptionCallback",

            //----------------------------------------------------------------------------
            // IDebugControl2
            //----------------------------------------------------------------------------

            "GetInterrupt",
            "OutputVaList",
            "GetDebuggeeType",
            "GetPageSize",
            "GetExecutingProcessorType",
            "Execute",
            "GetLastEventInformation",
            "Disassemble",

            //----------------------------------------------------------------------------
            // IDebugControl4
            //----------------------------------------------------------------------------

            "GetContextStackTrace",

            //----------------------------------------------------------------------------
            // IDebugDataSpaces
            //----------------------------------------------------------------------------

            "ReadVirtual",
            "WriteVirtual",
                
            //----------------------------------------------------------------------------
            // IDebugSymbols
            //----------------------------------------------------------------------------

            "GetSymbolOptions",
            "GetNameByOffset",
            "GetNumberModules",
            "GetModuleByIndex",
            "GetModuleByModuleName",
            "GetModuleByOffset",
            "GetModuleNames",
            "GetModuleParameters",
            "GetModuleNameString",
            "IsPointer64Bit",
            "GetLineByOffset",
            "GetSourceFileLineOffsets",
            "FindSourceFile",

            //----------------------------------------------------------------------------
            // IDebugSystemObjects
            //----------------------------------------------------------------------------

            "GetCurrentProcessId",
            "GetCurrentThreadId",
            "SetCurrentThreadId",
            "GetCurrentThreadSystemId",
            "GetThreadIdBySystemId",
            "GetThreadContextById",

            //----------------------------------------------------------------------------
            // IDebugRegisters
            //----------------------------------------------------------------------------

            "GetValueByName",
            "GetInstructionOffset",
            "GetStackOffset",
            "GetFrameOffset",

            //----------------------------------------------------------------------------
            // LLDBServices (internal)
            //----------------------------------------------------------------------------

            "GetModuleDirectory"
        };

        public static void Execute(IntPtr client, string args, DebuggingMethod callback)
        {
            if (!callback.Method.IsStatic)
            {
                Console.WriteLine("The callback given to DebuggingContext.Execute needs to be a static method");
                return;
            }

            if (IsWinDbg)
            {
                ExecuteWinDbg(client, args, callback);
            }
            else
            {
                if (Runtime == null)
                {
                    Initialize(client, false);
                }

                callback(Runtime, args);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ExecuteWinDbg(IntPtr client, string args, DebuggingMethod callback)
        {
            if (ChildDomain == null)
            {
                var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                ChildDomain = AppDomain.CreateDomain("WinDbgExtension", AppDomain.CurrentDomain.Evidence, assemblyPath, ".", false);
            }

            var invoker = new CrossDomainInvoker(client, args, callback.Method.DeclaringType.AssemblyQualifiedName, callback.Method.MetadataToken);

            ChildDomain.DoCallBack(invoker.Invoke);
        }

        public static void Initialize(IntPtr ptrClient, bool isWinDbg)
        {
            // On our first call to the API:
            //   1. Store a copy of IDebugClient in DebugClient.
            //   2. Replace Console's output stream to be the debugger window.
            //   3. Create an instance of DataTarget using the IDebugClient.
            if (DebugClient == null)
            {
                if (!isWinDbg)
                {
                    DebugClient = NativeInvoker.GetInstance<ILLDBDebugClient>(ptrClient, vtable);

                    var linuxFunctionsType = Type.GetType("Microsoft.Diagnostics.Runtime.LinuxFunctions, Microsoft.Diagnostics.Runtime");

                    var field = typeof(DataTarget).GetField("<PlatformFunctions>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

                    field.SetValue(null, Activator.CreateInstance(linuxFunctionsType));
                }
                else
                {
                    DebugClient = (IDebugClient)Marshal.GetUniqueObjectForIUnknown(ptrClient);

                    var stream = new StreamWriter(new DbgEngStream(DebugClient)) { AutoFlush = true };
                    Console.SetOut(stream);
                }

                DataTarget = DataTarget.CreateFromDebuggerInterface(DebugClient);
            }

            // If our ClrRuntime instance is null, it means that this is our first call, or
            // that the dac wasn't loaded on any previous call.  Find the dac loaded in the
            // process (the user must use .cordll), then construct our runtime from it.
            if (Runtime == null)
            {
                if (!isWinDbg)
                {
                    Runtime = DataTarget.ClrVersions.Single().CreateRuntime();
                }
                else
                {

                    // Just find a module named mscordacwks and assume it's the one the user
                    // loaded into windbg.
                    var process = Process.GetCurrentProcess();
                    foreach (ProcessModule module in process.Modules)
                    {
                        if (module.FileName.ToLower().Contains("mscordacwks"))
                        {
                            // TODO:  This does not support side-by-side CLRs.
                            Runtime = DataTarget.ClrVersions.Single().CreateRuntime(module.FileName);
                            break;
                        }
                    }
                }

                // Otherwise, the user didn't run .cordll.
                if (Runtime == null)
                {
                    Console.WriteLine("Mscordacwks.dll not loaded into the debugger.");
                    Console.WriteLine("Run .cordll to load the dac before running this command.");
                }
            }
            else
            {
                // If we already had a runtime, flush it for this use.  This is ONLY required
                // for a live process or iDNA trace.  If you use the IDebug* apis to detect
                // that we are debugging a crash dump you may skip this call for better perf.
                Runtime.Flush();
            }
        }
    }
}