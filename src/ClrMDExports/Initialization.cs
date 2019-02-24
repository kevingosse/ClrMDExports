namespace ClrMDExports.Private
{
    public class Initialization
    {
        public static bool IsWinDbg
        {
            set => DebuggingContext.IsWinDbg = value;
        }
    }
}
