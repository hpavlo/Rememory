using System;
using System.Runtime.InteropServices;

namespace Rememory.Helper
{
    public static class FileDialog
    {
        private static bool _isDialogOpen = false;

        public static string? ShowSaveFileDialog(string defaultFileName, COMDLG_FILTERSPEC[] filters)
        {
            if (_isDialogOpen)
            {
                return string.Empty;
            }

            IFileDialog? saveDialog;
            try
            {
                saveDialog = Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_FileSaveDialog)) as IFileDialog;
            }
            catch
            {
                return string.Empty;
            }

            if (saveDialog is null)
            {
                return string.Empty;
            }

            saveDialog.SetFileTypes((uint)filters.Length, filters);
            saveDialog.SetFileTypeIndex(0);
            saveDialog.GetOptions(out uint options);
            saveDialog.SetOptions(options | FOS_OVERWRITEPROMPT | FOS_FORCEFILESYSTEM);
            saveDialog.SetFileName(defaultFileName);

            var filePath = string.Empty;
            _isDialogOpen = true;

            if (saveDialog.Show(IntPtr.Zero) == 0)   // S_OK
            {
                saveDialog.GetResult(out IShellItem item);
                item.GetDisplayName(SIGDN_FILESYSPATH, out IntPtr pszPath);
                filePath = Marshal.PtrToStringUni(pszPath);
                Marshal.FreeCoTaskMem(pszPath);
            }

            _isDialogOpen = false;
            return filePath;
        }

        // SIGDN constants
        public const uint SIGDN_FILESYSPATH = 0x80058000;

        // FOS flags
        public const uint FOS_OVERWRITEPROMPT = 0x00000002;
        public const uint FOS_FORCEFILESYSTEM = 0x00000040;

        // CLSIDs for the dialogs
        public static readonly Guid CLSID_FileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");

        // IModalWindow interface
        [ComImport]
        [Guid("b4db1657-70d7-485e-8e3e-6fcb5a5c1802")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IModalWindow
        {
            [PreserveSig]
            int Show(IntPtr parent);
        }

        // IFileDialog interface
        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IFileDialog
        {
            // IModalWindow
            [PreserveSig]
            int Show(IntPtr parent);

            // IFileDialog methods
            void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(); // not used
            void Unadvise();
            void SetOptions(uint fos);
            void GetOptions(out uint fos);
            void SetDefaultFolder(); // not used
            void SetFolder(); // not used
            void GetFolder(); // not used
            void GetCurrentSelection(); // not used
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void ClearClientData();
            void SetFilter(); // not used
            void GetResult(out IShellItem ppsi);
            void AddPlace(); // not used
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(); // not used
            void SetClientGuid(); // not used
            void ClearClientData2(); // duplicate in some defs
        }

        // IShellItem interface
        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItem
        {
            void BindToHandler(); // not used
            void GetParent(); // not used
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(); // not used
            void Compare(); // not used
        }

        // Filter spec struct
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct COMDLG_FILTERSPEC
        {
            public string pszName;
            public string pszSpec;
        }
    }
}
