using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImGui.Forms.Localization;

namespace ImGui.Forms.Modals.IO.Windows;

public class WindowsSelectFolderDialog
{
    private const int MaxPath = 260;
    private const uint BifReturnOnlyFsDirs = 0x0001;
    private const uint BifEditBox = 0x0010;
    private const uint BifValidate = 0x0020;
    private const uint BifNewDialogStyle = 0x0040;
    private const int BffmInitialized = 1;
    private const uint BffmSetSelectionW = 0x467;

    public LocalizedString Title { get; set; }
    public string InitialDirectory { get; set; } = null;
    public bool Success { get; private set; }
    public string Directory { get; private set; }

    public async Task<DialogResult> ShowAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return await ShowDefaultAsync();

        return await ShowWindowsAsync();
    }

    private async Task<DialogResult> ShowDefaultAsync()
    {
        var sfd = new SelectFolderDialog
        {
            Caption = Title,
            Directory = InitialDirectory
        };

        DialogResult result = await sfd.ShowAsync();
        if (result == DialogResult.Ok)
            Directory = sfd.Directory;

        Success = result == DialogResult.Ok;
        return result;
    }

    private Task<DialogResult> ShowWindowsAsync()
    {
        var taskCompletion = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            int oleResult = OleInitialize(nint.Zero);

            try
            {
                ShowSelectFolderDialog();
                taskCompletion.SetResult(Success ? DialogResult.Ok : DialogResult.Cancel);
            }
            catch (Exception ex)
            {
                taskCompletion.SetException(ex);
            }
            finally
            {
                if (oleResult >= 0)
                    OleUninitialize();
            }
        });

        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletion.Task;
    }

    private void ShowSelectFolderDialog()
    {
        Success = false;
        Directory = null;

        var displayNamePtr = Marshal.AllocHGlobal(MaxPath * sizeof(char));

        var initialDirectory = !string.IsNullOrEmpty(InitialDirectory) && System.IO.Directory.Exists(InitialDirectory)
            ? InitialDirectory
            : null;

        BrowseCallbackProc callback = null;
        if (initialDirectory != null)
        {
            callback = (hwnd, msg, _, _) =>
            {
                if (msg == BffmInitialized)
                {
                    nint pathPtr = Marshal.StringToHGlobalUni(initialDirectory);
                    try
                    {
                        SendMessage(hwnd, BffmSetSelectionW, (nint)1, pathPtr);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pathPtr);
                    }
                }

                return 0;
            };
        }

        try
        {
            var browseInfo = new BrowseInfo
            {
                hwndOwner = nint.Zero,
                pidlRoot = nint.Zero,
                pszDisplayName = displayNamePtr,
                lpszTitle = Title,
                ulFlags = BifReturnOnlyFsDirs | BifEditBox | BifValidate | BifNewDialogStyle,
                lpfn = callback,
                lParam = nint.Zero,
                iImage = 0
            };

            nint pidl = SHBrowseForFolder(ref browseInfo);
            if (pidl == nint.Zero)
                return;

            try
            {
                var path = new StringBuilder(MaxPath);
                if (SHGetPathFromIDList(pidl, path))
                {
                    Directory = path.ToString();
                    Success = true;
                }
            }
            finally
            {
                CoTaskMemFree(pidl);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(displayNamePtr);
        }
    }

    private delegate int BrowseCallbackProc(nint hwnd, int msg, nint lParam, nint lpData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BrowseInfo
    {
        public nint hwndOwner;
        public nint pidlRoot;
        public nint pszDisplayName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszTitle;

        public uint ulFlags;
        public BrowseCallbackProc lpfn;
        public nint lParam;
        public int iImage;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SHBrowseForFolder([In] ref BrowseInfo lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SHGetPathFromIDList(nint pidl, StringBuilder pszPath);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(nint pv);

    [DllImport("ole32.dll")]
    private static extern int OleInitialize(nint pvReserved);

    [DllImport("ole32.dll")]
    private static extern void OleUninitialize();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}
