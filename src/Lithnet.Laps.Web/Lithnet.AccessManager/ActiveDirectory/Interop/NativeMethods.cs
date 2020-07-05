﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Security.Principal;
using NLog;

namespace Lithnet.AccessManager.Interop
{
    internal static class NativeMethods
    {
        public static int DirectoryReferralLimit { get; set; } = 10;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int InsufficientBuffer = 122;

        private const int ErrorMoreData = 234;


        private const string AuthzObjectUuidWithcap = "9a81c2bd-a525-471d-a4ed-49907c0b23da";

        private const string RcpOverTcpProtocol = "ncacn_ip_tcp";

        private static SecurityIdentifier currentDomainSid;

        private static SecurityIdentifier CurrentDomainSid
        {
            get
            {
                if (NativeMethods.currentDomainSid == null)
                {
                    Domain domain = Domain.GetComputerDomain();
                    NativeMethods.currentDomainSid = new SecurityIdentifier((byte[])(domain.GetDirectoryEntry().Properties["objectSid"][0]), 0);
                }

                return NativeMethods.currentDomainSid;
            }
        }

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsBind(string domainControllerName, string dnsDomainName, out IntPtr hds);

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsUnBind(IntPtr hds);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DsCrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, uint cNames, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPTStr, SizeParamIndex = 4)] string[] rpNames, out IntPtr ppResult);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void DsFreeNameResult(IntPtr pResult);

        [DllImport("NetApi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DsGetDcName(string computerName, string domainName, IntPtr domainGuid, string siteName, DsGetDcNameFlags flags, out IntPtr domainControllerInfo);

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetServerGetInfo(string serverName, int level, out IntPtr pServerInfo);

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetWkstaGetInfo(string serverName, int level, out IntPtr pWorkstationInfo);

        [DllImport("NetApi32.dll")]
        private static extern int NetApiBufferFree(IntPtr buffer);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzInitializeRemoteResourceManager(IntPtr rpcInitInfo, out SafeAuthzResourceManagerHandle authRm);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzInitializeContextFromSid(AuthzInitFlags flags, byte[] rawUserSid, SafeAuthzResourceManagerHandle authRm, IntPtr expirationTime, Luid identifier, IntPtr dynamicGroupArgs, out SafeAuthzContextHandle authzClientContext);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzInitializeResourceManager(AuthzResourceManagerFlags flags, IntPtr pfnAccessCheck, IntPtr pfnComputeDynamicGroups, IntPtr pfnFreeDynamicGroups,
            string szResourceManagerName, out SafeAuthzResourceManagerHandle phAuthzResourceManager);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzFreeContext(SafeAuthzResourceManagerHandle authzClientContext);

        [DllImport("authz.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzFreeResourceManager(SafeAuthzResourceManagerHandle authRm);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzGetInformationFromContext(SafeAuthzContextHandle hAuthzClientContext, AuthzContextInformationClass infoClass, uint bufferSize, out uint pSizeRequired, IntPtr buffer);

        [DllImport("authz.dll", SetLastError = true)]
        private static extern bool AuthzAccessCheck(AuthzAccessCheckFlags flags, SafeAuthzContextHandle hAuthzClientContext, ref AuthzAccessRequest pRequest, IntPtr AuditEvent, [MarshalAs(UnmanagedType.LPArray)]byte[] pSecurityDescriptor, IntPtr OptionalSecurityDescriptorArray, int OptionalSecurityDescriptorCount, ref AuthzAccessReply pReply, IntPtr phAccessCheckResults);

        [DllImport("NetAPI32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private extern static int NetLocalGroupGetMembers([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string localgroupname, int level, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries, IntPtr resume_handle);

        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private extern static int NetLocalGroupAddMember(string server, string groupName, IntPtr sid);

        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private extern static int NetLocalGroupDelMember(string server, string groupName, IntPtr sid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private extern static bool CreateWellKnownSid(WellKnownSidType wellKnownSidType, IntPtr domainSid, IntPtr pSid, ref int cbSid);

        [DllImport("Advapi32.dll", SetLastError = true, PreserveSig = true)]
        private static extern int LsaQueryInformationPolicy(IntPtr pPolicyHandle, PolicyInformationClass informationClass, out IntPtr pData);

        [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
        private static extern int LsaOpenPolicy(IntPtr pSystemName, ref LsaObjectAttributes objectAttributes, LsaAccessPolicy desiredAccess, out IntPtr pPolicyHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int LsaClose(IntPtr hPolicy);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int LsaNtStatusToWinError(int status);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int LsaFreeMemory(IntPtr buffer);

        public static SecurityIdentifier GetLocalMachineAuthoritySid()
        {
            IntPtr pPolicyHandle = IntPtr.Zero;
            IntPtr pPolicyData = IntPtr.Zero;

            try
            {
                LsaObjectAttributes lsaObjectAttributes = new LsaObjectAttributes();

                var result = LsaOpenPolicy(IntPtr.Zero, ref lsaObjectAttributes, LsaAccessPolicy.PolicyViewLocalInformation, out pPolicyHandle);

                if (result != 0)
                {
                    result = LsaNtStatusToWinError(result);
                    throw new DirectoryException("LsaOpenPolicy failed", new Win32Exception(result));
                }

                result = LsaQueryInformationPolicy(pPolicyHandle, PolicyInformationClass.PolicyAccountDomainInformation, out pPolicyData);

                if (result != 0)
                {
                    result = LsaNtStatusToWinError(result);
                    throw new DirectoryException("LsaQueryInformationPolicy failed", new Win32Exception(result));
                }

                PolicyAccountDomainInfo info = Marshal.PtrToStructure<PolicyAccountDomainInfo>(pPolicyData);

                return new SecurityIdentifier(info.DomainSid);
            }
            finally
            {
                if (pPolicyData != IntPtr.Zero)
                {
                    LsaFreeMemory(pPolicyData);
                }

                if (pPolicyHandle != IntPtr.Zero)
                {
                    LsaClose(pPolicyHandle);
                }
            }
        }

        public static SecurityIdentifier CreateWellKnownSid(WellKnownSidType sidType)
        {
            return CreateWellKnownSid(sidType, GetLocalMachineAuthoritySid());
        }

        public static SecurityIdentifier CreateWellKnownSid(WellKnownSidType sidType, SecurityIdentifier domainSid)
        {
            IntPtr pSid = IntPtr.Zero;
            IntPtr pDomainSid = IntPtr.Zero;

            try
            {
                int pSidLength = 0;
                string sidString = string.Empty;

                pDomainSid = Marshal.AllocHGlobal(domainSid.BinaryLength);
                byte[] bDomainSid = new byte[domainSid.BinaryLength];
                domainSid.GetBinaryForm(bDomainSid, 0);
                Marshal.Copy(bDomainSid, 0, pDomainSid, bDomainSid.Length);

                if (!CreateWellKnownSid(sidType, pDomainSid, pSid, ref pSidLength))
                {
                    var result = Marshal.GetLastWin32Error();

                    if (result != InsufficientBuffer)
                    {
                        throw new DirectoryException("CreateWellKnownSid failed", new Win32Exception(result));
                    }
                }
                else
                {
                    throw new DirectoryException("CreateWellKnownSid should have failed");
                }

                pSid = Marshal.AllocHGlobal(pSidLength);
                if (!NativeMethods.CreateWellKnownSid(sidType, pDomainSid, pSid, ref pSidLength))
                {
                    throw new DirectoryException("CreateWellKnownSid failed", new Win32Exception(Marshal.GetLastWin32Error()));
                }

                return new SecurityIdentifier(pSid);
            }
            finally
            {
                if (pDomainSid != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pDomainSid);
                }

                if (pSid != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pSid);
                }
            }

        }

        public static void AddLocalGroupMember(string groupName, SecurityIdentifier sid)
        {
            var sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);

            IntPtr pSid = Marshal.AllocHGlobal(sidBytes.Length);

            try
            {
                Marshal.Copy(sidBytes, 0, pSid, sidBytes.Length);

                var result = NetLocalGroupAddMember(null, groupName, pSid);

                if (result != 0)
                {
                    throw new DirectoryException("NetLocalGroupAddMember failed", new Win32Exception(result));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pSid);
            }
        }

        public static void RemoveLocalGroupMember(string groupName, SecurityIdentifier sid)
        {
            var sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);

            IntPtr pSid = Marshal.AllocHGlobal(sidBytes.Length);

            try
            {
                Marshal.Copy(sidBytes, 0, pSid, sidBytes.Length);

                var result = NetLocalGroupDelMember(null, groupName, pSid);

                if (result != 0)
                {
                    throw new DirectoryException("NetLocalGroupDelMember failed", new Win32Exception(result));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pSid);
            }
        }


        public static IList<SecurityIdentifier> GetLocalGroupMembers(string groupName)
        {
            int result;

            List<SecurityIdentifier> list = new List<SecurityIdentifier>();

            do
            {
                int entriesRead;
                int totalEntries;
                IntPtr resume = IntPtr.Zero;
                IntPtr pLocalGroupMemberInfo = IntPtr.Zero;

                try
                {
                    result = NetLocalGroupGetMembers(null, groupName, 0, out pLocalGroupMemberInfo, -1, out entriesRead, out totalEntries, resume);

                    if (result != 0 && result != InsufficientBuffer)
                    {
                        throw new DirectoryException("NetLocalGroupGetMembers returned an error", new Win32Exception(result));
                    }

                    IntPtr currentPosition = pLocalGroupMemberInfo;

                    for (int i = 0; i < entriesRead; i++)
                    {
                        var item = (LocalGroupMembersInfo0)Marshal.PtrToStructure<LocalGroupMembersInfo0>(currentPosition);
                        list.Add(new SecurityIdentifier(item.pSID));
                        currentPosition = IntPtr.Add(currentPosition, Marshal.SizeOf(typeof(LocalGroupMembersInfo0)));
                    }
                }
                finally
                {
                    if (pLocalGroupMemberInfo != IntPtr.Zero)
                    {
                        NetApiBufferFree(pLocalGroupMemberInfo);
                    }
                }
            }
            while (result == ErrorMoreData);

            return list;
        }

        public static ServerInfo101 GetServerInfo(string server)
        {
            IntPtr pServerInfo = IntPtr.Zero;

            try
            {
                int result = NetServerGetInfo(server, 101, out pServerInfo);

                if (result != 0)
                {
                    throw new DirectoryException("NetServerGetInfo failed", new Win32Exception(result));
                }

                var info = Marshal.PtrToStructure<ServerInfo101>(pServerInfo);

                return info;
            }
            finally
            {
                if (pServerInfo != IntPtr.Zero)
                {
                    NetApiBufferFree(pServerInfo);
                }
            }
        }

        public static WorkstationInfo100 GetWorkstationInfo(string server)
        {
            IntPtr pServerInfo = IntPtr.Zero;

            try
            {
                int result = NetWkstaGetInfo(server, 100, out pServerInfo);

                if (result != 0)
                {
                    throw new DirectoryException("NetWkstaGetInfo failed", new Win32Exception(result));
                }

                var info = Marshal.PtrToStructure<WorkstationInfo100>(pServerInfo);

                return info;
            }
            finally
            {
                if (pServerInfo != IntPtr.Zero)
                {
                    NetApiBufferFree(pServerInfo);
                }
            }
        }

        public static string GetDn(string nameToFind)
        {
            return GetDn(nameToFind, DsNameFormat.DS_UNKNOWN_NAME);
        }

        public static string GetDn(string nameToFind, DsNameFormat nameFormat)
        {
            var result = CrackNames(nameFormat, DsNameFormat.DS_FQDN_1779_NAME, nameToFind);
            return result.Name;
        }

        public static bool CheckForSidInToken(SecurityIdentifier principalSid, SecurityIdentifier sidToCheck, SecurityIdentifier requestContext = null)
        {
            if (principalSid == null)
            {
                throw new ArgumentNullException(nameof(principalSid));
            }

            if (sidToCheck == null)
            {
                throw new ArgumentNullException(nameof(sidToCheck));
            }

            string server;

            if (requestContext == null || requestContext.IsEqualDomainSid(NativeMethods.CurrentDomainSid))
            {
                server = null;
            }
            else
            {
                string dnsDomain = NativeMethods.GetDnsDomainNameFromSid(requestContext.AccountDomainSid);
                server = NativeMethods.GetDomainControllerForDnsDomain(dnsDomain);
            }

            return NativeMethods.CheckForSidInToken(principalSid, sidToCheck, server);
        }

        private static string GetDomainControllerForDnsDomain(string dnsDomain, bool forceRediscovery = false)
        {
            IntPtr pdcInfo = IntPtr.Zero;

            try
            {
                int result = DsGetDcName(
                    null,
                    dnsDomain,
                    IntPtr.Zero,
                    null,
                    DsGetDcNameFlags.DS_DIRECTORY_SERVICE_8_REQUIRED | (forceRediscovery ? DsGetDcNameFlags.DS_FORCE_REDISCOVERY : 0),
                    out pdcInfo);

                if (result != 0)
                {
                    throw new DirectoryException("DsGetDcName failed", new Win32Exception(result));
                }

                DomainControllerInfo info = Marshal.PtrToStructure<DomainControllerInfo>(pdcInfo);

                return info.DomainControllerName.TrimStart('\\');
            }
            finally
            {
                if (pdcInfo != IntPtr.Zero)
                {
                    NetApiBufferFree(pdcInfo);
                }
            }
        }

        public static string GetDnsDomainNameFromSid(SecurityIdentifier sid)
        {
            var result = CrackNames(DsNameFormat.DS_SID_OR_SID_HISTORY_NAME, DsNameFormat.DS_NT4_ACCOUNT_NAME, sid.Value);
            return result.Domain;
        }

        private static DsNameResultItem CrackNames(DsNameFormat formatOffered, DsNameFormat formatDesired, string name, string dnsDomainName = null, int referralLevel = 0)
        {
            IntPtr hds = IntPtr.Zero;

            try
            {
                int result = NativeMethods.DsBind(null, dnsDomainName, out hds);
                if (result != 0)
                {
                    throw new DirectoryException("DsBind failed", new Win32Exception(result));
                }

                DsNameResultItem nameResult = NativeMethods.CrackNames(hds, DsNameFlags.DS_NAME_FLAG_TRUST_REFERRAL, formatOffered, formatDesired, name);

                switch (nameResult.Status)
                {
                    case DsNameError.None:
                        return nameResult;

                    case DsNameError.NoMapping:
                        throw new NameMappingException($"The object name {name} was found in the global catalog, but could not be mapped to a DN. DsCrackNames returned NO_MAPPING");

                    case DsNameError.TrustReferral:
                    case DsNameError.DomainOnly:
                        if (!string.IsNullOrWhiteSpace(nameResult.Domain))
                        {
                            if (referralLevel < NativeMethods.DirectoryReferralLimit)
                            {
                                return NativeMethods.CrackNames(formatOffered, formatDesired, name, nameResult.Domain, ++referralLevel);
                            }

                            throw new ReferralLimitExceededException("The referral limit exceeded the maximum configured value");
                        }

                        throw new ReferralFailedException($"A referral to the object name {name} was received from the global catalog, but no referral information was provided. DsNameError: {nameResult.Status}");

                    case DsNameError.NotFound:
                        throw new ObjectNotFoundException($"The object name {name} was not found in the global catalog");

                    case DsNameError.NotUnique:
                        throw new AmbiguousNameException($"There was more than one object with the name {name} in the global catalog");

                    case DsNameError.Resolving:
                        throw new NameMappingException($"The object name {name} was not able to be resolved in the global catalog. DsCrackNames returned RESOLVING");

                    case DsNameError.NoSyntacticalMapping:
                        throw new NameMappingException($"DsCrackNames unexpectedly returned DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING for name {name}");

                    default:
                        throw new NameMappingException($"An unexpected status was returned from DsCrackNames {nameResult.Status}");
                }
            }
            finally
            {
                if (hds != IntPtr.Zero)
                {
                    NativeMethods.DsUnBind(hds);
                }
            }
        }

        private static DsNameResultItem CrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, string name)
        {
            DsNameResultItem[] resultItems = NativeMethods.CrackNames(hds, flags, formatOffered, formatDesired, new[] { name });
            return resultItems[0];
        }

        private static DsNameResultItem[] CrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, string[] namesToCrack)
        {
            IntPtr pDsNameResult = IntPtr.Zero;
            DsNameResultItem[] resultItems;

            try
            {
                uint namesToCrackCount = (uint)namesToCrack.Length;

                int result = NativeMethods.DsCrackNames(hds, flags, formatOffered, formatDesired, namesToCrackCount, namesToCrack, out pDsNameResult);

                if (result != 0)
                {
                    throw new DirectoryException("DsCrackNames failed", new Win32Exception(result));
                }

                DsNameResult dsNameResult = (DsNameResult)Marshal.PtrToStructure(pDsNameResult, typeof(DsNameResult));

                if (dsNameResult.cItems == 0)
                {
                    throw new DirectoryException("DsCrackNames returned an unexpected result");
                }

                resultItems = new DsNameResultItem[dsNameResult.cItems];
                IntPtr pItem = dsNameResult.rItems;

                for (int idx = 0; idx < dsNameResult.cItems; idx++)
                {
                    resultItems[idx] = (DsNameResultItem)Marshal.PtrToStructure(pItem, typeof(DsNameResultItem));
                    pItem = IntPtr.Add(pItem, Marshal.SizeOf(resultItems[idx]));
                }
            }
            finally
            {
                if (pDsNameResult != IntPtr.Zero)
                {
                    NativeMethods.DsFreeNameResult(pDsNameResult);
                }
            }

            return resultItems;
        }

        private static bool CheckForSidInToken(SecurityIdentifier principalSid, SecurityIdentifier sidToCheck, string serverName = null)
        {
            if (principalSid == sidToCheck)
            {
                return true;
            }

            foreach (SecurityIdentifier sid in NativeMethods.GetTokenGroups(principalSid, serverName))
            {
                if (sid == sidToCheck)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<SecurityIdentifier> GetTokenGroups(SecurityIdentifier principalSid, string authzServerName = null)
        {
            SafeAuthzResourceManagerHandle authzRm = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(authzServerName))
                {
                    AuthzRpcInitInfoClient client = new AuthzRpcInitInfoClient
                    {
                        Version = AuthzRpcClientVersion.V1,
                        ObjectUuid = NativeMethods.AuthzObjectUuidWithcap,
                        Protocol = NativeMethods.RcpOverTcpProtocol,
                        Server = authzServerName
                    };


                    SafeAllocHGlobalHandle clientInfo = new SafeAllocHGlobalHandle(Marshal.SizeOf(typeof(AuthzRpcInitInfoClient)));
                    IntPtr pClientInfo = clientInfo.DangerousGetHandle();
                    Marshal.StructureToPtr(client, pClientInfo, false);

                    if (!NativeMethods.AuthzInitializeRemoteResourceManager(pClientInfo, out authzRm))
                    {
                        throw new DirectoryException("AuthzInitializeRemoteResourceManager failed", new Win32Exception(Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (Exception ex)
            {
                NativeMethods.logger.Warn(ex, $"Unable to connect to the remote server {authzServerName} to generate the authorization token for principal {principalSid}. The local server will be used instead, however the token generated may not contain authorization groups from other domains");
            }

            if (authzRm == null || authzRm.IsInvalid)
            {
                if (!NativeMethods.AuthzInitializeResourceManager(AuthzResourceManagerFlags.NO_AUDIT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, null, out authzRm))
                {
                    throw new DirectoryException("AuthzInitializeResourceManager failed", new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }

            byte[] sidBytes = new byte[principalSid.BinaryLength];
            principalSid.GetBinaryForm(sidBytes, 0);

            SafeAuthzContextHandle userClientCtxt = null;
            if (!NativeMethods.AuthzInitializeContextFromSid(AuthzInitFlags.Default, sidBytes, authzRm, IntPtr.Zero, Luid.NullLuid, IntPtr.Zero, out userClientCtxt))
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == 5)
                {
                    throw new DirectoryException("AuthzInitializeContextFromSid failed", new Win32Exception(errorCode, "Access was denied. Please ensure that \r\n1) The service account is a member of the built-in group called 'Windows Authorization Access Group' in the domain where the computer object is located\r\n2) The service account is a member of the built-in group called 'Access Control Assistance Operators' in the domain where the computer object is located"));
                }

                throw new DirectoryException("AuthzInitializeContextFromSid failed", new Win32Exception(errorCode));
            }

            uint sizeRequired = 0;

            if (!NativeMethods.AuthzGetInformationFromContext(userClientCtxt, AuthzContextInformationClass.AuthzContextInfoGroupsSids, sizeRequired, out sizeRequired, IntPtr.Zero))
            {
                Win32Exception e = new Win32Exception(Marshal.GetLastWin32Error());

                if (e.NativeErrorCode != NativeMethods.InsufficientBuffer)
                {
                    throw new DirectoryException("AuthzGetInformationFromContext failed", e);
                }
            }

            SafeAllocHGlobalHandle structure = new SafeAllocHGlobalHandle(sizeRequired);
            IntPtr pstructure = structure.DangerousGetHandle();

            if (!NativeMethods.AuthzGetInformationFromContext(userClientCtxt, AuthzContextInformationClass.AuthzContextInfoGroupsSids, sizeRequired, out sizeRequired, pstructure))
            {
                throw new DirectoryException("AuthzGetInformationFromContext failed", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            TokenGroups groups = Marshal.PtrToStructure<TokenGroups>(pstructure);

            // Set the pointer to the first Groups array item in the structure
            IntPtr current = IntPtr.Add(pstructure, Marshal.OffsetOf<TokenGroups>(nameof(groups.Groups)).ToInt32());

            for (int i = 0; i < groups.GroupCount; i++)
            {
                SidAndAttributes sidAndAttributes = (SidAndAttributes)Marshal.PtrToStructure(current, typeof(SidAndAttributes));
                yield return new SecurityIdentifier(sidAndAttributes.Sid);
                current = IntPtr.Add(current, Marshal.SizeOf(typeof(SidAndAttributes)));
            }
        }

        public static bool AccessCheck(SecurityIdentifier sid, GenericSecurityDescriptor securityDescriptor, int requestedAccess, IList<GenericSecurityDescriptor> otherSecurityDescriptors = null,  string authzServerName = null)
        {
            SafeAuthzResourceManagerHandle authzRm = InitializeResourceManager(authzServerName);
            SafeAuthzContextHandle userClientCtxt = InitializeAuthorizationContextFromSid(authzRm, sid);

            byte[] securityDescriptorBytes = securityDescriptor.ToBytes();

            AuthzAccessRequest request = new AuthzAccessRequest();
            request.PrincipalSelfSid = sid.ToBytes();
            request.DesiredAccess = requestedAccess;

            AuthzAccessReply reply = new AuthzAccessReply();
            SafeAllocHGlobalHandle accessMaskReply = new SafeAllocHGlobalHandle(Marshal.SizeOf<uint>());
            SafeAllocHGlobalHandle errorReply = new SafeAllocHGlobalHandle(Marshal.SizeOf<uint>());
            SafeAllocHGlobalHandle saclReply = new SafeAllocHGlobalHandle(Marshal.SizeOf<uint>());

            reply.ResultListLength = 1;
            reply.GrantedAccessMask = accessMaskReply.DangerousGetHandle();
            reply.SaclEvaluationResults = saclReply.DangerousGetHandle();
            reply.Error = errorReply.DangerousGetHandle();

            IntPtr pOthers = IntPtr.Zero;
            int othersCount = otherSecurityDescriptors?.Count ?? 0;
            if (othersCount > 0)
            {
                List<byte[]> list = new List<byte[]>();
                foreach(var item in otherSecurityDescriptors)
                {
                    list.Add(item.ToBytes());
                }

                LpArrayOfByteArrayConverter r = new LpArrayOfByteArrayConverter(list);
                pOthers = r.Ptr;
            }

            if (!NativeMethods.AuthzAccessCheck(AuthzAccessCheckFlags.None, userClientCtxt, ref request, IntPtr.Zero, securityDescriptorBytes, pOthers, othersCount, ref reply, IntPtr.Zero))
            {
                throw new DirectoryException("AuthzAccessCheck failed", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            int maskResult = Marshal.ReadInt32(reply.GrantedAccessMask);
            int error = Marshal.ReadInt32(reply.Error);

            if (error == 0)
            {
                return (requestedAccess & maskResult) == requestedAccess;
            }

            return false;
        }

        private static SafeAuthzContextHandle InitializeAuthorizationContextFromSid(SafeAuthzResourceManagerHandle authzRm, SecurityIdentifier sid)
        {
            byte[] sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);

            if (!NativeMethods.AuthzInitializeContextFromSid(AuthzInitFlags.Default, sidBytes, authzRm, IntPtr.Zero, Luid.NullLuid, IntPtr.Zero, out SafeAuthzContextHandle userClientCtxt))
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == 5)
                {
                    throw new DirectoryException("AuthzInitializeContextFromSid failed", new Win32Exception(errorCode, "Access was denied. Please ensure that \r\n1) The service account is a member of the built-in group called 'Windows Authorization Access Group' in the domain where the computer object is located\r\n2) The service account is a member of the built-in group called 'Access Control Assistance Operators' in the domain where the computer object is located"));
                }

                throw new DirectoryException("AuthzInitializeContextFromSid failed", new Win32Exception(errorCode));
            }

            return userClientCtxt;
        }

        private static SafeAuthzResourceManagerHandle InitializeResourceManager(string authzServerName)
        {
            SafeAuthzResourceManagerHandle authzRm = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(authzServerName))
                {
                    AuthzRpcInitInfoClient client = new AuthzRpcInitInfoClient
                    {
                        Version = AuthzRpcClientVersion.V1,
                        ObjectUuid = NativeMethods.AuthzObjectUuidWithcap,
                        Protocol = NativeMethods.RcpOverTcpProtocol,
                        Server = authzServerName
                    };


                    SafeAllocHGlobalHandle clientInfo = new SafeAllocHGlobalHandle(Marshal.SizeOf(typeof(AuthzRpcInitInfoClient)));
                    IntPtr pClientInfo = clientInfo.DangerousGetHandle();
                    Marshal.StructureToPtr(client, pClientInfo, false);

                    if (!NativeMethods.AuthzInitializeRemoteResourceManager(pClientInfo, out authzRm))
                    {
                        throw new DirectoryException("AuthzInitializeRemoteResourceManager failed", new Win32Exception(Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (Exception ex)
            {
                NativeMethods.logger.Warn(ex, $"Unable to connect to create an authorization context against the remote server {authzServerName}. The local server will be used instead, however calculated group membership from other domains may not be correct");
            }

            if (authzRm == null || authzRm.IsInvalid)
            {
                if (!NativeMethods.AuthzInitializeResourceManager(AuthzResourceManagerFlags.NO_AUDIT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, null, out authzRm))
                {
                    throw new DirectoryException("AuthzInitializeResourceManager failed", new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }

            return authzRm;
        }
    }
}