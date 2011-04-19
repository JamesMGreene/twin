using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using Twin.Logging;

namespace Twin {// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.


    // Represents a file on disk
    // On creation, uses win32 calls to open the file and get its volume and serial number
    // this is used for equality testing. This seems to be the only reliable way to determine whether 
    // two paths are to the same file, and there's no C# API.
    // http://stackoverflow.com/questions/410705/best-way-to-determine-if-two-path-reference-to-same-file-in-c
    // new FileRef("path1").equals(new FileRef("path2"));
    // 
    // Note that if the file must be held open by some process between creation of the filerefs for this check to be valid
    // we use it for attaching to running processes, so this is no problem.
    // otherwise, use FileRef.PathsAreEqual which ensures this.
    class FileRef {
        private uint fileIndexLow;
        private uint fileIndexHigh;
        private uint volumeIndex;

        private FileRef(IntPtr handle) {
            populate(handle);
        }

        public FileRef(Process process) : this(GetProcessPath(process)) {
        }

        public override bool Equals(Object other) {
            if (!(other is FileRef))
                return false;
            FileRef otherRef = (FileRef)other;
            return otherRef.fileIndexLow == fileIndexLow && otherRef.fileIndexHigh == fileIndexHigh && otherRef.volumeIndex == volumeIndex;
        }
        public override int GetHashCode() {
            return (int)((fileIndexHigh * 31 + fileIndexLow) * 31 + volumeIndex);
        }

        public static bool PathsAreEqual(string path1, string path2) {
            IntPtr handle1 = GetFileHandle(path1);
            try {
                IntPtr handle2 = GetFileHandle(path2);
                try {
                    return new FileRef(handle1).Equals(new FileRef(handle2));
                } finally {
                    CloseHandle(handle2);
                }
            } finally {
                CloseHandle(handle1);
            }
        }

        public FileRef(string path) {
            IntPtr handle = GetFileHandle(path);
            try {
                populate(handle);
            } finally {
                CloseHandle(handle);
            }
        }

        private void populate(IntPtr fh) {
            BY_HANDLE_FILE_INFORMATION info;
            if (!GetFileInformationByHandle(fh, out info))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            this.fileIndexHigh = info.FileIndexHigh;
            this.fileIndexLow = info.FileIndexLow;
            this.volumeIndex = info.VolumeSerialNumber;
        }

        private static string GetProcessPath(Process process) {
            IntPtr handle = OpenProcess(0x410, 0, (uint)process.Id); // PROCESS_QUERY_INFORMATION | PROCESS_VM_READ 
            if (handle == IntPtr.Zero) {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_ACCESS_DENIED)
                    throw new UnauthorizedAccessException("Couldn't read process: "+process, new Win32Exception(lastError));
                throw new Exception("Cannot open process: " + process, new Win32Exception(lastError));
            }
            try {
                StringBuilder result = new StringBuilder(1024);
                if (GetModuleFileNameEx(handle, IntPtr.Zero, result, result.Capacity) == 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return result.ToString();
            } finally {
                CloseHandle(handle);
            }
        }

        public static IntPtr GetFileHandle(string path) {
            if (path == null)
                throw new ArgumentNullException("path");

            IntPtr handle = CreateFile(
                path, 
                EFileAccess.GenericNone,
                EFileShare.All, 
                IntPtr.Zero, 
                ECreationDisposition.OpenExisting, 
                EFileAttributes.Normal, 
                IntPtr.Zero);
            if (handle == INVALID_HANDLE)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get handle for " + path);

            return handle;
        }

        // pinvoke cruft

        [DllImport("psapi.dll", SetLastError = true)]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwProcessId);

        private const int ERROR_ACCESS_DENIED = 5;
        private static readonly IntPtr INVALID_HANDLE = (IntPtr)(-1);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct BY_HANDLE_FILE_INFORMATION {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME {
            public UInt32 dwLowDateTime;
            public UInt32 dwHighDateTime;
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
           string fileName,
           EFileAccess fileAccess,
           EFileShare fileShare,
           IntPtr securityAttributes,
           ECreationDisposition creationDisposition,
           EFileAttributes flags,
           IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        private enum EFileAccess : uint {
            GenericNone = 0,
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000
        }

        [Flags]
        private enum EFileShare : uint {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
            All = 0x00000007
        }

        private enum ECreationDisposition : uint {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }

        [Flags]
        private enum EFileAttributes : uint {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }
    }
}
