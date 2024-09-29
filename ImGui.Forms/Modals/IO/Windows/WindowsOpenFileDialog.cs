using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ImGui.Forms.Localization;

namespace ImGui.Forms.Modals.IO.Windows
{
    public class WindowsOpenFileDialog
    {
        public LocalizedString Title { get; set; }
        public bool Multiselect { get; set; } = false;
        public string InitialDirectory { get; set; } = null;
        public IList<FileFilter> Filters { get; set; } = new List<FileFilter> { new("All Files", "*") };
        public bool ShowHidden { get; set; } = false;
        public bool Success { get; private set; }
        public string[] Files { get; private set; }

        public async Task<DialogResult> ShowAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await ShowDefaultAsync();

            await Task.Run(ShowOpenFileDialog);

            return Success ? DialogResult.Ok : DialogResult.Cancel;
        }

        private async Task<DialogResult> ShowDefaultAsync()
        {
            var ofd = new OpenFileDialog
            {
                Caption = Title,
                InitialDirectory = InitialDirectory
            };

            foreach (FileFilter filter in Filters)
                ofd.FileFilters.Add(filter);

            DialogResult result = await ofd.ShowAsync();
            if (result == DialogResult.Ok)
                Files = new[] { ofd.SelectedPath };

            Success = result == DialogResult.Ok;
            return result;
        }

        private void ShowOpenFileDialog()
        {
            const int MAX_FILE_LENGTH = 2048;

            Success = false;
            Files = null;

            OpenFileName ofn = new OpenFileName();

            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = string.Join('\0', Filters.Select(f => f.Name + " (" + string.Join(';', f.Extensions.Select(e => "*." + e)) + ")\0"+ string.Join(';', f.Extensions.Select(e => "*." + e)))) + "\0";
            ofn.fileTitle = new string(new char[MAX_FILE_LENGTH]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = InitialDirectory;
            ofn.title = Title;
            ofn.flags = (int)OpenFileNameFlags.OFN_HIDEREADONLY | (int)OpenFileNameFlags.OFN_EXPLORER | (int)OpenFileNameFlags.OFN_FILEMUSTEXIST | (int)OpenFileNameFlags.OFN_PATHMUSTEXIST;

            // Create buffer for file names
            ofn.file = Marshal.AllocHGlobal(MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize);
            ofn.maxFile = MAX_FILE_LENGTH;

            // Initialize buffer with NULL bytes
            for (int i = 0; i < MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize; i++)
            {
                Marshal.WriteByte(ofn.file, i, 0);
            }

            if (ShowHidden)
            {
                ofn.flags |= (int)OpenFileNameFlags.OFN_FORCESHOWHIDDEN;
            }

            if (Multiselect)
            {
                ofn.flags |= (int)OpenFileNameFlags.OFN_ALLOWMULTISELECT;
            }

            Success = GetOpenFileName(ofn);

            if (Success)
            {
                nint filePointer = ofn.file;
                long pointer = (long)filePointer;
                string file = Marshal.PtrToStringAuto(filePointer);
                List<string> strList = new List<string>();

                // Retrieve file names
                while (file.Length > 0)
                {
                    strList.Add(file);

                    pointer += file.Length * Marshal.SystemDefaultCharSize + Marshal.SystemDefaultCharSize;
                    filePointer = (nint)pointer;
                    file = Marshal.PtrToStringAuto(filePointer);
                }

                if (strList.Count > 1)
                {
                    Files = new string[strList.Count - 1];
                    for (int i = 1; i < strList.Count; i++)
                    {
                        Files[i - 1] = Path.Combine(strList[0], strList[i]);
                    }
                }
                else
                {
                    Files = strList.ToArray();
                }
            }

            Marshal.FreeHGlobal(ofn.file);
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class OpenFileName
        {
            public int structSize = 0;
            public nint dlgOwner = nint.Zero;
            public nint instance = nint.Zero;
            public string filter;
            public string customFilter;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public nint file;
            public int maxFile = 0;
            public string fileTitle;
            public int maxFileTitle = 0;
            public string initialDir;
            public string title;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt;
            public nint custData = nint.Zero;
            public nint hook = nint.Zero;
            public string templateName;
            public nint reservedPtr = nint.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        private enum OpenFileNameFlags
        {
            OFN_HIDEREADONLY = 0x4,
            OFN_FORCESHOWHIDDEN = 0x10000000,
            OFN_ALLOWMULTISELECT = 0x200,
            OFN_EXPLORER = 0x80000,
            OFN_FILEMUSTEXIST = 0x1000,
            OFN_PATHMUSTEXIST = 0x800
        }
    }
}
