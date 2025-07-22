using System;
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
        public string InitialFileName { get; set; } = null;
        public IList<FileFilter> Filters { get; set; } = new List<FileFilter> { new("All Files", "*") };
        public bool ShowHidden { get; set; } = false;
        public bool Success { get; private set; }
        public string[] Files { get; private set; }

        public async Task<DialogResult> ShowAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await ShowDefaultAsync();

            ShowOpenFileDialog();

            return Success ? DialogResult.Ok : DialogResult.Cancel;
        }

        private async Task<DialogResult> ShowDefaultAsync()
        {
            var ofd = new OpenFileDialog
            {
                Caption = Title,
                InitialDirectory = InitialDirectory,
                InitialFileName = InitialFileName
            };

            foreach (FileFilter filter in Filters)
                ofd.FileFilters.Add(filter);

            DialogResult result = await ofd.ShowAsync();
            if (result == DialogResult.Ok)
                Files = [ofd.SelectedPath];

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
            ofn.filter = string.Join('\0', Filters.Select(f => f.Name + " (" + string.Join(';', f.Extensions.Select(e => "*." + e)) + ")\0" + string.Join(';', f.Extensions.Select(e => "*." + e)))) + "\0";
            ofn.fileTitle = new string(new char[MAX_FILE_LENGTH]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = InitialDirectory;
            ofn.title = Title;
            ofn.flags = (int)(OpenFileNameFlags.HideReadOnly
                              | OpenFileNameFlags.Explorer
                              | OpenFileNameFlags.FileMustExist
                              | OpenFileNameFlags.PathMustExist
                              | OpenFileNameFlags.LongNames);

            // Create buffer for file names
            ofn.file = Marshal.AllocHGlobal(MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize);
            ofn.maxFile = MAX_FILE_LENGTH;

            var initFileLength = string.IsNullOrEmpty(InitialFileName) ? 0 : InitialFileName.Length * Marshal.SystemDefaultCharSize;
            var initFilePtr = Marshal.StringToHGlobalAuto(InitialFileName);

            // Initialize file name buffer
            for (int i = 0; i < initFileLength; i++)
            {
                Marshal.WriteByte(ofn.file, i, Marshal.ReadByte(initFilePtr, i));
            }

            for (int i = initFileLength; i < MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize; i++)
            {
                Marshal.WriteByte(ofn.file, i, 0);
            }

            if (ShowHidden)
            {
                ofn.flags |= (int)OpenFileNameFlags.ForceShowHidden;
            }

            if (Multiselect)
            {
                ofn.flags |= (int)OpenFileNameFlags.AllowMultiSelect;
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

        [Flags]
        enum OpenFileNameFlags
        {
            ReadOnly = 0x00000001,
            OverwritePrompt = 0x00000002,
            HideReadOnly = 0x00000004,
            NoChangeDir = 0x00000008,
            ShowHelp = 0x00000010,
            EnableHook = 0x00000020,
            EnableTemplate = 0x00000040,
            EnableTemplateHandle = 0x00000080,
            NoValidate = 0x00000100,
            AllowMultiSelect = 0x00000200,
            ExtensionDifferent = 0x00000400,
            PathMustExist = 0x00000800,
            FileMustExist = 0x00001000,
            CreatePrompt = 0x00002000,
            ShareAware = 0x00004000,
            NoReadOnlyReturn = 0x00008000,
            NoTestFileCreate = 0x00010000,
            NoNetworkButton = 0x00020000,
            NoLongNames = 0x00040000,          // Deprecated
            Explorer = 0x00080000,
            NoDereferenceLinks = 0x00100000,
            LongNames = 0x00200000,            // Deprecated
            EnableIncludeNotify = 0x00400000,
            EnableSizing = 0x00800000,
            DontAddToRecent = 0x02000000,
            ForceShowHidden = 0x10000000
        }
    }
}
