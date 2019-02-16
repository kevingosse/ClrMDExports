using Microsoft.Diagnostics.Runtime.Interop;

namespace ClrMDExports
{
    internal interface ILLDBDebugClient : IDebugClient, IDebugClient2, IDebugClient3, IDebugClient4, IDebugClient5, IDebugClient6,
        IDebugControl, IDebugControl2, IDebugControl3, IDebugControl4, IDebugControl5, IDebugControl6,
        IDebugSymbols, IDebugSymbols2, IDebugSymbols3, IDebugSymbols4, IDebugSymbols5,
        IDebugAdvanced, IDebugAdvanced2, IDebugAdvanced3,
        IDebugRegisters, IDebugRegisters2,
        IDebugBreakpoint, IDebugBreakpoint2, IDebugBreakpoint3,
        IDebugDataSpaces, IDebugDataSpaces2, IDebugDataSpaces3, IDebugDataSpaces4,
        IDebugSymbolGroup, IDebugSymbolGroup2,
        IDebugSystemObjects, IDebugSystemObjects2, //IDebugSystemObjects3,
        IDebugOutputCallbacks, IDebugOutputCallbacks2,
        IDebugInputCallbacks,
        IDebugDataSpacesPtr,
        IDebugEventCallbacksWide,
        IDebugOutputCallbacksWide,
        IDebugEventContextCallbacks
    {
    }
}