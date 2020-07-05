﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text;
using System.Windows;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    internal static class NativeMethods
    {
        private const int MAX_PATH = 256;

        private const int S_FALSE = 1;

        private const int S_OK = 0;

        private const uint SERVICE_NO_CHANGE = 0xffffffff;

        private const string CFSTR_DSOP_DS_SELECTION_LIST = "CFSTR_DSOP_DS_SELECTION_LIST";

        private const int CRYPTUI_WIZ_IMPORT_ALLOW_CERT = 0x00020000;

        private const int CRYPTUI_WIZ_IMPORT_NO_CHANGE_DEST_STORE = 0x00010000;

        private const int CRYPTUI_WIZ_IMPORT_TO_LOCALMACHINE = 0x00100000;
      
        [DllImport("dsuiext.dll", CharSet = CharSet.Unicode)]
        private static extern DsBrowseResult DsBrowseForContainer(IntPtr pInfo);

        [DllImport("ole32.dll")]
        private static extern void ReleaseStgMedium([In] ref STGMEDIUM stgmedium);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr ptr);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr ptr);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ChangeServiceConfig(IntPtr hService, uint nServiceType, uint nStartType, uint nErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string pDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

        [DllImport("cryptui.dll", SetLastError = true)]
        private static extern bool CryptUIWizImport(int dwFlags, IntPtr hwndParent, [MarshalAs(UnmanagedType.LPWStr)] string pwszWizardTitle, IntPtr pImportSrc, IntPtr hDestCertStore);

        public static string ShowContainerDialog(IntPtr hwnd, string dialogTitle = null, string treeViewTitle = null, AdsFormat pathFormat = AdsFormat.X500Dn)
        {
            IntPtr pInfo = IntPtr.Zero;

            try
            {
                DSBrowseInfo info = new DSBrowseInfo();

                info.StructSize = Marshal.SizeOf(info);
                info.DialogCaption = dialogTitle;
                info.TreeViewTitle = treeViewTitle;
                info.DialogOwner = hwnd;
                info.Path = new string(new char[MAX_PATH]);
                info.PathSize = info.Path.Length;
                info.Flags = DsBrowseInfoFlags.EntireDirectory | DsBrowseInfoFlags.ReturnFormat | DsBrowseInfoFlags.ReturnObjectClass;
                info.ReturnFormat = pathFormat;
                info.ObjectClass = new string(new char[MAX_PATH]);
                info.ObjectClassSize = MAX_PATH;

                pInfo = Marshal.AllocHGlobal(Marshal.SizeOf<DSBrowseInfo>());
                Marshal.StructureToPtr(info, pInfo, false);

                DsBrowseResult status = DsBrowseForContainer(pInfo);

                if (status == DsBrowseResult.Ok)
                {
                    DSBrowseInfo result = Marshal.PtrToStructure<DSBrowseInfo>(pInfo);
                    return result.Path;
                }

                return null;
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pInfo);
                }
            }
        }

        public static IEnumerable<DsopResult> ShowObjectPickerDialog(IntPtr hwnd, DsopScopeInitInfo scope, params string[] attributesToGet)
        {
            return ShowObjectPickerDialog(hwnd, new List<DsopScopeInitInfo> { scope }, attributesToGet);
        }

        public static IEnumerable<DsopResult> ShowObjectPickerDialog(IntPtr hwnd, IList<DsopScopeInitInfo> scopes, params string[] attributesToGet)
        {
            IDsObjectPicker idsObjectPicker = (IDsObjectPicker)new DSObjectPicker();

            try
            {
                using LpStructArrayMarshaller<DsopScopeInitInfo> scopeInitInfoArray = CreateScopes(scopes);
                using LpStringArrayConverter attributes = new LpStringArrayConverter(attributesToGet);
                DsopDialogInitializationInfo initInfo = CreateInitInfo(scopeInitInfoArray.Ptr, scopeInitInfoArray.Count, attributes.Ptr, attributes.Count);

                int hresult = idsObjectPicker.Initialize(ref initInfo);

                if (hresult != S_OK)
                {
                    throw new COMException("Directory object picker initialization failed", hresult);
                }

                hresult = idsObjectPicker.InvokeDialog(hwnd, out IDataObject data);

                if (hresult == S_FALSE)
                {
                    return null;
                }

                if (hresult != S_OK)
                {
                    throw new COMException("Directory object picker dialog activation failed", hresult);
                }

                return GetResultsFromDataObject(data, attributesToGet);
            }
            finally
            {
                if (idsObjectPicker != null)
                {
                    Marshal.ReleaseComObject(idsObjectPicker);
                }
            }
        }

        public static void ChangeServiceCredentials(string serviceName, string username, string password)
        {
            ServiceController controller = new ServiceController(serviceName);
            try
            {
                bool success = false;
                controller.ServiceHandle.DangerousAddRef(ref success);

                if (!success)
                {
                    throw new InvalidOperationException("Could not increment handle");
                }

                if (!ChangeServiceConfig(controller.ServiceHandle.DangerousGetHandle(), SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, username, password, null))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                controller.ServiceHandle.DangerousRelease();
            }
        }

        public static string GetKeyLocation(X509Certificate2 cert)
        {
            var cng = cert.PrivateKey as RSACng;
            var crypto = cert.PrivateKey as RSACryptoServiceProvider;

            string name = cng.Key.UniqueName ?? crypto.CspKeyContainerInfo.UniqueKeyContainerName;

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Application Data\Microsoft\Crypto\RSA\MachineKeys", name);

            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Application Data\Microsoft\Crypto\Keys", name);


            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }

        public static X509Certificate2 ShowCertificateImportDialog(IntPtr hwnd, string title, StoreLocation location, StoreName name)
        {
            List<string> thumbprints = new List<string>();

            using (X509Store store = new X509Store(name, StoreLocation.LocalMachine, OpenFlags.ReadWrite))
            {
                thumbprints = store.Certificates.OfType<X509Certificate2>().Select(t => t.Thumbprint).ToList();

                if (!CryptUIWizImport(CRYPTUI_WIZ_IMPORT_ALLOW_CERT | CRYPTUI_WIZ_IMPORT_NO_CHANGE_DEST_STORE | CRYPTUI_WIZ_IMPORT_TO_LOCALMACHINE,
                    hwnd, title, IntPtr.Zero, store.StoreHandle))
                {
                    int result = Marshal.GetLastWin32Error();
                    throw new Win32Exception(result);
                }
            }

            using (X509Store store = new X509Store(name, StoreLocation.LocalMachine, OpenFlags.ReadWrite))
            {
                var newCertificateList = store.Certificates.OfType<X509Certificate2>().ToList();

                var newItems = newCertificateList.Where(t => !thumbprints.Any(u => u == t.Thumbprint));

                foreach (var newItem in newItems)
                {
                    if (newItem.HasPrivateKey)
                    {
                        return newItem;
                    }
                }
            }

            return null;
        }

        private static DsopDialogInitializationInfo CreateInitInfo(IntPtr pScopeInitInfo, int scopeCount, IntPtr attrributesToGet, int attributesToGetCount)
        {
            var initInfo = new DsopDialogInitializationInfo
            {
                Size = Marshal.SizeOf<DsopDialogInitializationInfo>(),
                TargetComputer = null,// "extdev1.local",
                ScopeInfoCount = scopeCount,
                ScopeInfo = pScopeInitInfo,
                Options = 0
            };

            initInfo.AttributesToFetchCount = attributesToGetCount;
            initInfo.AttributesToFetch = attrributesToGet;

            return initInfo;
        }

        private static LpStructArrayMarshaller<DsopScopeInitInfo> CreateScopes(IList<DsopScopeInitInfo> scopes)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                var s = scopes[i];
                s.Size = Marshal.SizeOf<DsopScopeInitInfo>();
            }

            return new LpStructArrayMarshaller<DsopScopeInitInfo>(scopes);
        }

        private static IEnumerable<DsopResult> GetResultsFromDataObject(IDataObject data, string[] requestedAttributes)
        {
            IntPtr pSelectionList;

            STGMEDIUM storageMedium = new STGMEDIUM
            {
                tymed = TYMED.TYMED_HGLOBAL,
                unionmember = IntPtr.Zero,
                pUnkForRelease = IntPtr.Zero
            };

            FORMATETC formatEtc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(CFSTR_DSOP_DS_SELECTION_LIST).Id,
                ptd = IntPtr.Zero,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = TYMED.TYMED_HGLOBAL
            };

            int result = data.GetData(ref formatEtc, ref storageMedium);

            if (result != S_OK)
            {
                throw new COMException("GetData failed", result);
            }

            pSelectionList = GlobalLock(storageMedium.unionmember);
            if (pSelectionList == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                var selectionList = Marshal.PtrToStructure<DsSelectionList>(pSelectionList);
                IntPtr current = new IntPtr(pSelectionList.ToInt64() + Marshal.SizeOf(selectionList.FetchedAttributeCount.GetType()) + Marshal.SizeOf(selectionList.Count.GetType()));

                if (selectionList.Count > 0)
                {
                    for (int i = 0; i < selectionList.Count; i++)
                    {
                        var f = Marshal.PtrToStructure<DsSelection>(current);
                        yield return new DsopResult(f, requestedAttributes, (int)selectionList.FetchedAttributeCount);
                        current = new IntPtr(current.ToInt64() + Marshal.SizeOf<DsSelection>());
                    }
                }
            }
            finally
            {
                if (pSelectionList != IntPtr.Zero)
                {
                    GlobalUnlock(pSelectionList);
                }

                ReleaseStgMedium(ref storageMedium);
            }
        }
    }
}
