// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

#pragma warning disable 1591

namespace Microsoft.WSMan.Management
{
    #region "public Api"

    #region WsManEnumFlags
    
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [TypeLibType((short)0)]
    public enum WSManEnumFlags
    {
        
        WSManFlagNonXmlText = 1,

        
        WSManFlagReturnObject = 0,

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        WSManFlagReturnEPR = 2,

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        WSManFlagReturnObjectAndEPR = 4,

        
        WSManFlagHierarchyDeep = 0,

        
        WSManFlagHierarchyShallow = 32,

        
        WSManFlagHierarchyDeepBasePropsOnly = 64,

        
        WSManFlagAssociationInstance = 128
    }

    #endregion WsManEnumFlags

    #region WsManSessionFlags
    
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [TypeLibType((short)0)]
    public enum WSManSessionFlags
    {
        
        WSManNone = 0,

        
        WSManFlagUtf8 = 1,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        WSManFlagCredUserNamePassword = 4096,

        
        WSManFlagSkipCACheck = 8192,

        
        WSManFlagSkipCNCheck = 16384,

        
        WSManFlagUseNoAuthentication = 32768,

        
        WSManFlagUseDigest = 65536,

        
        WSManFlagUseNegotiate = 131072,

        
        WSManFlagUseBasic = 262144,

        
        WSManFlagUseKerberos = 524288,

        
        WSManFlagNoEncryption = 1048576,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Spn")]
        WSManFlagEnableSpnServerPort = 4194304,

        
        WSManFlagUtf16 = 8388608,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ssp")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        WSManFlagUseCredSsp = 16777216,

        
        WSManFlagUseClientCertificate = 2097152,

        
        WSManFlagSkipRevocationCheck = 33554432,

        
        WSManFlagAllowNegotiateImplicitCredentials = 67108864,

        
        WSManFlagUseSsl = 134217728
    }
    #endregion WsManSessionFlags

    #region AuthenticationMechanism
    
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum AuthenticationMechanism
    {
        
        None = 0x0,
        
        Default = 0x1,
        
        Digest = 0x2,
        
        Negotiate = 0x4,
        
        Basic = 0x8,
        
        Kerberos = 0x10,
        
        ClientCertificate = 0x20,
        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Credssp")]
        Credssp = 0x80,
    }

    #endregion AuthenticationMechanism

    #region IWsMan

    
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
    [Guid("190D8637-5CD3-496D-AD24-69636BB5A3B5")]
    [ComImport]
    [TypeLibType((short)4304)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    public interface IWSMan
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        
        /// <remarks><para>An original IDL definition of <c>CreateSession</c> method was the following:  <c>HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue);

        [DispId(1)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IUnknown)] object connectionOptions);
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IDispatch)] object connectionOptions);
#endif
        
        /// <remarks><para>An original IDL definition of <c>CreateConnectionOptions</c> method was the following:  <c>HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue);
        //
        [DispId(2)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateConnectionOptions();

        
        /// <remarks><para>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;</para></remarks>
        // IDL: BSTR CommandLine;
        //
        string CommandLine
        {
            // IDL: HRESULT CommandLine ([out, retval] BSTR* ReturnValue);

            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;
        //

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(4)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
    #endregion IWsMan

    #region IWSManConnectionOptions
    
    [Guid("F704E861-9E52-464F-B786-DA5EB2320FDD")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
    public interface IWSManConnectionOptions
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif
        
        /// <remarks><para>An original IDL definition of <c>UserName</c> property was the following:  <c>BSTR UserName</c>;</para></remarks>
        // IDL: BSTR UserName;

        string UserName
        {
            // IDL: HRESULT UserName ([out, retval] BSTR* ReturnValue);

            [DispId(1)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT UserName (BSTR value);

            [DispId(1)]
            set;
        }

        
        /// <remarks><para>An original IDL definition of <c>Password</c> property was the following:  <c>BSTR Password</c>;</para></remarks>
        // IDL: BSTR Password;

        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
        string Password
        {
            // IDL: HRESULT Password (BSTR value);

            [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
            [DispId(2)]
            set;
        }
    }

    
    [Guid("EF43EDF7-2A48-4d93-9526-8BD6AB6D4A6B")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IWSManConnectionOptionsEx : IWSManConnectionOptions
    {
        
        string CertificateThumbprint
        {
            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;

            [DispId(1)]
            set;
        }
    }

    
    [Guid("F500C9EC-24EE-48ab-B38D-FC9A164C658E")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface IWSManConnectionOptionsEx2 : IWSManConnectionOptionsEx
    {
        
        [DispId(4)]
        void SetProxy(int accessType,
            int authenticationMechanism,
            [In, MarshalAs(UnmanagedType.BStr)] string userName,
            [In, MarshalAs(UnmanagedType.BStr)] string password);

        
        [DispId(5)]
        int ProxyIEConfig();

        
        [DispId(6)]
        int ProxyWinHttpConfig();

        
        [DispId(7)]
        int ProxyAutoDetect();

        
        [DispId(8)]
        int ProxyNoProxyServer();

        
        [DispId(9)]
        int ProxyAuthenticationUseNegotiate();

        
        [DispId(10)]
        int ProxyAuthenticationUseBasic();

        
        [DispId(11)]
        int ProxyAuthenticationUseDigest();
    }

    #endregion IWSManConnectionOptions

    #region IWSManEnumerator
    
    [Guid("F3457CA9-ABB9-4FA5-B850-90E8CA300E7F")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    public interface IWSManEnumerator
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        
        /// <remarks><para>An original IDL definition of <c>ReadItem</c> method was the following:  <c>HRESULT ReadItem ([out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT ReadItem ([out, retval] BSTR* ReturnValue);

        [DispId(1)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string ReadItem();

        
        /// <remarks><para>An original IDL definition of <c>AtEndOfStream</c> property was the following:  <c>BOOL AtEndOfStream</c>;</para></remarks>
        // IDL: BOOL AtEndOfStream;

        bool AtEndOfStream
        {
            // IDL: HRESULT AtEndOfStream ([out, retval] BOOL* ReturnValue);

            [DispId(2)]
            [return: MarshalAs(UnmanagedType.Bool)]
            get;
        }

        
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);
            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(8)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
    #endregion IWSManEnumerator

    #region IWSManEx
    
    [Guid("2D53BDAA-798E-49E6-A1AA-74D01256F411")]
    [ComImport]
    [TypeLibType((short)4304)]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "str")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Username")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
    public interface IWSManEx
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        
        /// <remarks><para>An original IDL definition of <c>CreateSession</c> method was the following:  <c>HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue);

        [DispId(1)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IUnknown)] object connectionOptions);
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IDispatch)] object connectionOptions);
#endif

        
        /// <remarks><para>An original IDL definition of <c>CreateConnectionOptions</c> method was the following:  <c>HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue);

        [DispId(2)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateConnectionOptions();

        
        /// <returns></returns>
        string CommandLine
        {
            // IDL: HRESULT CommandLine ([out, retval] BSTR* ReturnValue);

            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(4)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        
        /// <remarks><para>An original IDL definition of <c>CreateResourceLocator</c> method was the following:  <c>HRESULT CreateResourceLocator ([optional, defaultvalue(string.Empty)] BSTR strResourceLocator, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateResourceLocator ([optional, defaultvalue(string.Empty)] BSTR strResourceLocator, [out, retval] IDispatch** ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "str")]
        [DispId(5)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateResourceLocator([MarshalAs(UnmanagedType.BStr)] string strResourceLocator);

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagUTF8</c> method was the following:  <c>HRESULT SessionFlagUTF8 ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUTF8 ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UTF")]
        [DispId(6)]
        int SessionFlagUTF8();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagCredUsernamePassword</c> method was the following:  <c>HRESULT SessionFlagCredUsernamePassword ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagCredUsernamePassword ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Username")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        [DispId(7)]
        int SessionFlagCredUsernamePassword();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagSkipCACheck</c> method was the following:  <c>HRESULT SessionFlagSkipCACheck ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagSkipCACheck ([out, retval] long* ReturnValue);

        [DispId(8)]
        int SessionFlagSkipCACheck();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagSkipCNCheck</c> method was the following:  <c>HRESULT SessionFlagSkipCNCheck ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagSkipCNCheck ([out, retval] long* ReturnValue);

        [DispId(9)]
        int SessionFlagSkipCNCheck();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseDigest</c> method was the following:  <c>HRESULT SessionFlagUseDigest ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseDigest ([out, retval] long* ReturnValue);

        [DispId(10)]
        int SessionFlagUseDigest();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseNegotiate</c> method was the following:  <c>HRESULT SessionFlagUseNegotiate ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseNegotiate ([out, retval] long* ReturnValue);

        [DispId(11)]
        int SessionFlagUseNegotiate();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseBasic</c> method was the following:  <c>HRESULT SessionFlagUseBasic ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseBasic ([out, retval] long* ReturnValue);

        [DispId(12)]
        int SessionFlagUseBasic();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseKerberos</c> method was the following:  <c>HRESULT SessionFlagUseKerberos ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseKerberos ([out, retval] long* ReturnValue);

        [DispId(13)]
        int SessionFlagUseKerberos();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagNoEncryption</c> method was the following:  <c>HRESULT SessionFlagNoEncryption ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagNoEncryption ([out, retval] long* ReturnValue);

        [DispId(14)]
        int SessionFlagNoEncryption();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagEnableSPNServerPort</c> method was the following:  <c>HRESULT SessionFlagEnableSPNServerPort ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagEnableSPNServerPort ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        [DispId(15)]
        int SessionFlagEnableSPNServerPort();

        
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseNoAuthentication</c> method was the following:  <c>HRESULT SessionFlagUseNoAuthentication ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseNoAuthentication ([out, retval] long* ReturnValue);

        [DispId(16)]
        int SessionFlagUseNoAuthentication();

        
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagNonXmlText</c> method was the following:  <c>HRESULT EnumerationFlagNonXmlText ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagNonXmlText ([out, retval] long* ReturnValue);

        [DispId(17)]
        int EnumerationFlagNonXmlText();

        
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagReturnEPR</c> method was the following:  <c>HRESULT EnumerationFlagReturnEPR ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagReturnEPR ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        [DispId(18)]
        int EnumerationFlagReturnEPR();

        
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagReturnObjectAndEPR</c> method was the following:  <c>HRESULT EnumerationFlagReturnObjectAndEPR ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagReturnObjectAndEPR ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        [DispId(19)]
        int EnumerationFlagReturnObjectAndEPR();

        
        /// <remarks><para>An original IDL definition of <c>GetErrorMessage</c> method was the following:  <c>HRESULT GetErrorMessage (unsigned long errorNumber, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT GetErrorMessage (unsigned long errorNumber, [out, retval] BSTR* ReturnValue);

        [DispId(20)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetErrorMessage(uint errorNumber);

        
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagHierarchyDeep</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyDeep ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyDeep ([out, retval] long* ReturnValue);

        [DispId(21)]
        int EnumerationFlagHierarchyDeep();

        
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagHierarchyShallow</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyShallow ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyShallow ([out, retval] long* ReturnValue);

        [DispId(22)]
        int EnumerationFlagHierarchyShallow();

        
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagHierarchyDeepBasePropsOnly</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyDeepBasePropsOnly ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyDeepBasePropsOnly ([out, retval] long* ReturnValue);

        [DispId(23)]
        int EnumerationFlagHierarchyDeepBasePropsOnly();

        
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagReturnObject</c> method was the following:  <c>HRESULT EnumerationFlagReturnObject ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagReturnObject ([out, retval] long* ReturnValue);

        [DispId(24)]
        int EnumerationFlagReturnObject();

        
        /// <remarks><para>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;</para></remarks>
        // IDL: BSTR CommandLine;
        [DispId(28)]
        int EnumerationFlagAssociationInstance();

        
        /// <remarks><para>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;</para></remarks>
        // IDL: BSTR CommandLine;
        [DispId(29)]
        int EnumerationFlagAssociatedInstance();
    }
    #endregion IWsManEx

    #region IWsManResourceLocator

    
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]

    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sel")]

    [Guid("A7A1BA28-DE41-466A-AD0A-C4059EAD7428")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    public interface IWSManResourceLocator
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif
        
        /// <remarks><para>An original IDL definition of <c>resourceUri</c> property was the following:  <c>BSTR resourceUri</c>;</para></remarks>
        // Set the resource URI. Must contain path only -- query string is not allowed here.
        // IDL: BSTR resourceUri;
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        string ResourceUri
        {
            // IDL: HRESULT resourceUri ([out, retval] BSTR* ReturnValue);
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
            [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
            [DispId(1)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;

            // IDL: HRESULT resourceUri (BSTR value);
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
            [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
            [DispId(1)]
            set;
        }

        
        /// <remarks><para>An original IDL definition of <c>AddSelector</c> method was the following:  <c>HRESULT AddSelector (BSTR resourceSelName, VARIANT selValue)</c>;</para></remarks>
        // Add selector to resource locator
        // IDL: HRESULT AddSelector (BSTR resourceSelName, VARIANT selValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "sel")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sel")]
        [DispId(2)]
        void AddSelector([MarshalAs(UnmanagedType.BStr)] string resourceSelName, object selValue);

        
        /// <remarks><para>An original IDL definition of <c>ClearSelectors</c> method was the following:  <c>HRESULT ClearSelectors (void)</c>;</para></remarks>
        // Clear all selectors
        // IDL: HRESULT ClearSelectors (void);

        [DispId(3)]
        void ClearSelectors();

        
        /// <remarks><para>An original IDL definition of <c>FragmentPath</c> property was the following:  <c>BSTR FragmentPath</c>;</para></remarks>
        // Gets the fragment path
        // IDL: BSTR FragmentPath;

        string FragmentPath
        {
            // IDL: HRESULT FragmentPath ([out, retval] BSTR* ReturnValue);

            [DispId(4)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT FragmentPath (BSTR value);

            [DispId(4)]
            set;
        }

        
        /// <remarks><para>An original IDL definition of <c>FragmentDialect</c> property was the following:  <c>BSTR FragmentDialect</c>;</para></remarks>
        // Gets the Fragment dialect
        // IDL: BSTR FragmentDialect;

        string FragmentDialect
        {
            // IDL: HRESULT FragmentDialect ([out, retval] BSTR* ReturnValue);

            [DispId(5)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT FragmentDialect (BSTR value);

            [DispId(5)]
            set;
        }

        
        /// <remarks><para>An original IDL definition of <c>AddOption</c> method was the following:  <c>HRESULT AddOption (BSTR OptionName, VARIANT OptionValue, [optional, defaultvalue(0)] long mustComply)</c>;</para></remarks>
        // Add option to resource locator
        // IDL: HRESULT AddOption (BSTR OptionName, VARIANT OptionValue, [optional, defaultvalue(0)] long mustComply);
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Option")]
        [DispId(6)]
        void AddOption([MarshalAs(UnmanagedType.BStr)] string OptionName, object OptionValue, int mustComply);

        
        /// <remarks><para>An original IDL definition of <c>MustUnderstandOptions</c> property was the following:  <c>long MustUnderstandOptions</c>;</para></remarks>
        // Sets the MustUnderstandOptions value
        // IDL: long MustUnderstandOptions;

        int MustUnderstandOptions
        {
            // IDL: HRESULT MustUnderstandOptions ([out, retval] long* ReturnValue);

            [DispId(7)]
            get;
            // IDL: HRESULT MustUnderstandOptions (long value);

            [DispId(7)]
            set;
        }

        
        /// <remarks><para>An original IDL definition of <c>ClearOptions</c> method was the following:  <c>HRESULT ClearOptions (void)</c>;</para></remarks>
        // Clear all options
        // IDL: HRESULT ClearOptions (void);

        [DispId(8)]
        void ClearOptions();

        
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(9)]
            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
    #endregion IWsManResourceLocator

    #region IWSManSession
    
    [Guid("FC84FC58-1286-40C4-9DA0-C8EF6EC241E0")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
    public interface IWSManSession
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        
        /// <remarks><para>An original IDL definition of <c>Get</c> method was the following:  <c>HRESULT Get (VARIANT resourceUri, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Get (VARIANT resourceUri, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
        [DispId(1)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Get(object resourceUri, int flags);

        
        /// <remarks><para>An original IDL definition of <c>Put</c> method was the following:  <c>HRESULT Put (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Put (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(2)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Put(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string resource, int flags);

        
        /// <remarks><para>An original IDL definition of <c>Create</c> method was the following:  <c>HRESULT Create (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Create (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(3)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Create(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string resource, int flags);

        
        /// <remarks><para>An original IDL definition of <c>Delete</c> method was the following:  <c>HRESULT Delete (VARIANT resourceUri, [optional, defaultvalue(0)] long flags)</c>;</para></remarks>
        // IDL: HRESULT Delete (VARIANT resourceUri, [optional, defaultvalue(0)] long flags);

        [DispId(4)]
        void Delete(object resourceUri, int flags);

        
        /// <param name="actionURI"></param>
        /// <param name="resourceUri"></param>
        /// <param name="parameters"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        [DispId(5)]
        string Invoke([MarshalAs(UnmanagedType.BStr)] string actionURI, [In] object resourceUri, [MarshalAs(UnmanagedType.BStr)] string parameters, [In] int flags);

        
        /// <remarks><para>An original IDL definition of <c>Enumerate</c> method was the following:  <c>HRESULT Enumerate (VARIANT resourceUri, [optional, defaultvalue(string.Empty)] BSTR filter, [optional, defaultvalue(string.Empty)] BSTR dialect, [optional, defaultvalue(0)] long flags, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Enumerate (VARIANT resourceUri, [optional, defaultvalue(string.Empty)] BSTR filter, [optional, defaultvalue(string.Empty)] BSTR dialect, [optional, defaultvalue(0)] long flags, [out, retval] IDispatch** ReturnValue);

        [DispId(6)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object Enumerate(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string filter, [MarshalAs(UnmanagedType.BStr)] string dialect, int flags);

        
        /// <remarks><para>An original IDL definition of <c>Identify</c> method was the following:  <c>HRESULT Identify ([optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Identify ([optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(7)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Identify(int flags);

        
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(8)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        
        /// <remarks><para>An original IDL definition of <c>BatchItems</c> property was the following:  <c>long BatchItems</c>;</para></remarks>
        // IDL: long BatchItems;

        int BatchItems
        {
            // IDL: HRESULT BatchItems ([out, retval] long* ReturnValue);

            [DispId(9)]
            get;
            // IDL: HRESULT BatchItems (long value);

            [DispId(9)]
            set;
        }

        
        /// <remarks><para>An original IDL definition of <c>Timeout</c> property was the following:  <c>long Timeout</c>;</para></remarks>
        // IDL: long Timeout;

        int Timeout
        {
            // IDL: HRESULT Timeout ([out, retval] long* ReturnValue);

            [DispId(10)]
            get;
            // IDL: HRESULT Timeout (long value);

            [DispId(10)]
            set;
        }
    }

    #endregion IWSManSession

    #region IWSManResourceLocatorInternal
    
    [Guid("EFFAEAD7-7EC8-4716-B9BE-F2E7E9FB4ADB")]
    [ComImport]
    [TypeLibType((short)400)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IWSManResourceLocatorInternal
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif
    }

    #endregion IWSManResourceLocatorInternal

    
    [Guid("BCED617B-EC03-420b-8508-977DC7A686BD")]
    [ComImport]
#if CORECLR
    [ClassInterface(ClassInterfaceType.None)]
#else
    [ClassInterface(ClassInterfaceType.AutoDual)]
#endif
    public class WSManClass
    {
    }

    #region IGroupPolicyObject

    
    [Guid("EA502722-A23D-11d1-A7D3-0000F87571E3")]
    [ComImport]
    [ClassInterface(ClassInterfaceType.None)]
    public class GPClass
    {
    }

    [ComImport, Guid("EA502723-A23D-11d1-A7D3-0000F87571E3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IGroupPolicyObject
    {
        void New(
          [MarshalAs(UnmanagedType.LPWStr)] string pszDomainName,
          [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
          uint dwFlags);

        void OpenDSGPO(
          [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
          uint dwFlags);

        void OpenLocalMachineGPO(uint dwFlags);

        void OpenRemoteMachineGPO(
          [MarshalAs(UnmanagedType.LPWStr)] string pszComputerName,
          uint dwFlags);

        void Save(
          [MarshalAs(UnmanagedType.Bool)] bool bMachine,
          [MarshalAs(UnmanagedType.Bool)] bool bAdd,
          [MarshalAs(UnmanagedType.LPStruct)] Guid pGuidExtension,
          [MarshalAs(UnmanagedType.LPStruct)] Guid pGuid);

        void Delete();

        void GetName(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
          int cchMaxLength);

        void GetDisplayName(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
          int cchMaxLength);

        void SetDisplayName(
          [MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetPath(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
          int cchMaxPath);

        void GetDSPath(
          uint dwSection,
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
          int cchMaxPath);

        void GetFileSysPath(
          uint dwSection,
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
          int cchMaxPath);

        IntPtr GetRegistryKey(uint dwSection);

        uint GetOptions();

        void SetOptions(uint dwOptions, uint dwMask);

        void GetMachineName(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
          int cchMaxLength);

        uint GetPropertySheetPages(out IntPtr hPages);
    }

    #endregion IGroupPolicyObject

    
    public sealed class GpoNativeApi
    {
        private GpoNativeApi() { }

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern System.IntPtr EnterCriticalPolicySection(
             [In, MarshalAs(UnmanagedType.Bool)] bool bMachine);

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LeaveCriticalPolicySection(
             [In] System.IntPtr hSection);
    }
    #endregion
}

#pragma warning restore 1591
