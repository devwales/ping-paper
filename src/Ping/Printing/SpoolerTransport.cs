using System.Runtime.InteropServices;

namespace Ping.Printing;

/// <summary>
/// Sends raw ESC/POS bytes through the Windows print spooler using the RAW datatype.
/// This is the reliable USB path on Windows: install the printer with the vendor
/// driver or the built-in "Generic / Text Only" driver, then select its queue name
/// in Ping settings. No extra USB drivers or Zadig swaps needed.
/// </summary>
public class SpoolerTransport : IPrintTransport
{
    private readonly string _printerName;

    public SpoolerTransport(string printerName) => _printerName = printerName;

    public string Describe() => $"Windows printer '{_printerName}'";

    public Task SendAsync(byte[] payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_printerName))
            throw new InvalidOperationException("No printer selected yet - pick one in Settings.");

        if (!OpenPrinter(_printerName, out var hPrinter, IntPtr.Zero))
            throw new InvalidOperationException("Printer not found - check it's installed and the USB cable is connected.");

        try
        {
            var docInfo = new DOC_INFO_1
            {
                pDocName = "Ping receipt",
                pDataType = "RAW",
                pOutputFile = null
            };

            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                throw new InvalidOperationException("The printer queue isn't accepting jobs - is the printer on?");

            try
            {
                if (!StartPagePrinter(hPrinter))
                    throw new InvalidOperationException("Couldn't start the page - the printer may be busy or out of paper.");

                try
                {
                    var pBytes = Marshal.AllocCoTaskMem(payload.Length);
                    try
                    {
                        Marshal.Copy(payload, 0, pBytes, payload.Length);
                        if (!WritePrinter(hPrinter, pBytes, payload.Length, out var written) || written != payload.Length)
                            throw new InvalidOperationException("Printing stopped partway - check paper and connection.");
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pBytes);
                    }
                }
                finally
                {
                    EndPagePrinter(hPrinter);
                }
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            ClosePrinter(hPrinter);
        }

        return Task.CompletedTask;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOC_INFO_1
    {
        public string? pDocName;
        public string? pOutputFile;
        public string? pDataType;
    }

    [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOC_INFO_1 pDocInfo);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
}
