// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// PinvokeDllNames contains the DLL names to be use for PInvoke in FullCLR/CoreCLR powershell.
    ///
    /// * When adding a new DLL name here, make sure that you add both the FullCLR and CoreCLR version
    ///   of it. Add the comment '' with the new DLL name, and make sure the 'COUNT' is the
    ///   same for both FullCLR and CoreCLR DLL names.
    /// </summary>
    internal static class PinvokeDllNames
    {
        internal const string QueryDosDeviceDllName = "api-ms-win-core-file-l1-1-0.dll";                     
        internal const string CreateSymbolicLinkDllName = "api-ms-win-core-file-l2-1-0.dll";                 
        internal const string GetOEMCPDllName = "api-ms-win-core-localization-l1-2-0.dll";                   
        internal const string DeviceIoControlDllName = "api-ms-win-core-io-l1-1-0.dll";                      
        internal const string CreateFileDllName = "api-ms-win-core-file-l1-1-0.dll";                         
        internal const string DeleteFileDllName = "api-ms-win-core-file-l1-1-0.dll";                         
        internal const string FindCloseDllName = "api-ms-win-core-file-l1-1-0.dll";                          
        internal const string GetFileAttributesDllName = "api-ms-win-core-file-l1-1-0.dll";                  
        internal const string FindFirstFileDllName = "api-ms-win-core-file-l1-1-0.dll";                      
        internal const string FindNextFileDllName = "api-ms-win-core-file-l1-1-0.dll";                       
        internal const string RegEnumValueDllName = "api-ms-win-core-registry-l1-1-0.dll";                   
        internal const string RegOpenKeyExDllName = "api-ms-win-core-registry-l1-1-0.dll";                   
        internal const string RegOpenKeyTransactedDllName = "api-ms-win-core-registry-l2-1-0.dll";           
        internal const string RegQueryInfoKeyDllName = "api-ms-win-core-registry-l1-1-0.dll";                
        internal const string RegQueryValueExDllName = "api-ms-win-core-registry-l2-1-0.dll";                
        internal const string RegSetValueExDllName = "api-ms-win-core-registry-l1-1-0.dll";                  
        internal const string RegCreateKeyTransactedDllName = "api-ms-win-core-registry-l2-1-0.dll";         
        internal const string CryptGenKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";               
        internal const string CryptDestroyKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";           
        internal const string CryptAcquireContextDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";       
        internal const string CryptReleaseContextDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";       
        internal const string CryptEncryptDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";              
        internal const string CryptDecryptDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";              
        internal const string CryptExportKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";            
        internal const string CryptImportKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";            
        internal const string CryptDuplicateKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";         
        internal const string GetLastErrorDllName = "api-ms-win-core-errorhandling-l1-1-0.dll";              
        internal const string GetCPInfoDllName = "api-ms-win-core-localization-l1-2-0.dll";                  
        internal const string CommandLineToArgvDllName = "api-ms-win-downlevel-shell32-l1-1-0.dll";          
        internal const string LocalFreeDllName = "api-ms-win-core-misc-l1-1-0.dll";                          
        internal const string CloseHandleDllName = "api-ms-win-core-handle-l1-1-0.dll";                      
        internal const string GetTokenInformationDllName = "api-ms-win-security-base-l1-1-0.dll";            
        internal const string LookupAccountSidDllName = "api-ms-win-security-lsalookup-l2-1-0.dll";          
        internal const string OpenProcessTokenDllName = "api-ms-win-core-processsecurity-l1-1-0.dll";        
        internal const string DosDateTimeToFileTimeDllName = "api-ms-win-core-kernel32-legacy-l1-1-0.dll";   
        internal const string LocalFileTimeToFileTimeDllName = "api-ms-win-core-file-l1-1-0.dll";            
        internal const string SetFileTimeDllName = "api-ms-win-core-file-l1-1-0.dll";                        
        internal const string SetFileAttributesWDllName = "api-ms-win-core-file-l1-1-0.dll";                 
        internal const string CreateHardLinkDllName = "api-ms-win-core-file-l2-1-0.dll";                     
        internal const string RegCloseKeyDllName = "api-ms-win-core-registry-l1-1-0.dll";                    
        internal const string GetFileInformationByHandleDllName = "api-ms-win-core-file-l1-1-0.dll";         
        internal const string FindFirstStreamDllName = "api-ms-win-core-file-l1-2-2.dll";                    
        internal const string FindNextStreamDllName = "api-ms-win-core-file-l1-2-2.dll";                     
        internal const string GetSystemInfoDllName = "api-ms-win-core-sysinfo-l1-1-0.dll";                   
        internal const string GetCurrentThreadIdDllName = "api-ms-win-core-processthreads-l1-1-0.dll";       
        internal const string SetLocalTimeDllName = "api-ms-win-core-sysinfo-l1-1-0.dll";                    
        internal const string CryptSetProvParamDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";         
        internal const string GetNamedSecurityInfoDllName = "api-ms-win-security-provider-l1-1-0.dll";       
        internal const string SetNamedSecurityInfoDllName = "api-ms-win-security-provider-l1-1-0.dll";       
        internal const string ConvertStringSidToSidDllName = "api-ms-win-security-sddl-l1-1-0.dll";          
        internal const string IsValidSidDllName = "api-ms-win-security-base-l1-1-0.dll";                     
        internal const string GetLengthSidDllName = "api-ms-win-security-base-l1-1-0.dll";                   
        internal const string LsaFreeMemoryDllName = "api-ms-win-security-lsapolicy-l1-1-0.dll";             
        internal const string InitializeAclDllName = "api-ms-win-security-base-l1-1-0.dll";                  
        internal const string GetCurrentProcessDllName = "api-ms-win-core-processthreads-l1-1-0.dll";        
        internal const string GetCurrentThreadDllName = "api-ms-win-core-processthreads-l1-1-0.dll";         
        internal const string OpenThreadTokenDllName = "api-ms-win-core-processthreads-l1-1-0.dll";          
        internal const string LookupPrivilegeValueDllName = "api-ms-win-security-lsalookup-l2-1-0.dll";      
        internal const string AdjustTokenPrivilegesDllName = "api-ms-win-security-base-l1-1-0.dll";          
        internal const string GetStdHandleDllName = "api-ms-win-core-processenvironment-l1-1-0.dll";         
        internal const string CreateProcessWithLogonWDllName = "api-ms-win-security-cpwl-l1-1-0.dll";        
        internal const string CreateProcessDllName = "api-ms-win-core-processthreads-l1-1-0.dll";            
        internal const string ResumeThreadDllName = "api-ms-win-core-processthreads-l1-1-0.dll";             
        internal const string OpenSCManagerWDllName = "api-ms-win-service-management-l1-1-0.dll";            
        internal const string OpenServiceWDllName = "api-ms-win-service-management-l1-1-0.dll";              
        internal const string CloseServiceHandleDllName = "api-ms-win-service-management-l1-1-0.dll";        
        internal const string ChangeServiceConfigWDllName = "api-ms-win-service-management-l2-1-0.dll";      
        internal const string ChangeServiceConfig2WDllName = "api-ms-win-service-management-l2-1-0.dll";     
        internal const string CreateServiceWDllName = "api-ms-win-service-management-l1-1-0.dll";            
        internal const string CreateJobObjectDllName = "api-ms-win-core-job-l2-1-0.dll";                     
        internal const string AssignProcessToJobObjectDllName = "api-ms-win-core-job-l2-1-0.dll";            
        internal const string QueryInformationJobObjectDllName = "api-ms-win-core-job-l2-1-0.dll";           
        internal const string CreateNamedPipeDllName = "api-ms-win-core-namedpipe-l1-1-0.dll";               
        internal const string WaitNamedPipeDllName = "api-ms-win-core-namedpipe-l1-1-0.dll";                 
        internal const string PrivilegeCheckDllName = "api-ms-win-security-base-l1-1-0.dll";                 
        internal const string ImpersonateNamedPipeClientDllName = "api-ms-win-core-namedpipe-l1-1-0.dll";    
        internal const string RevertToSelfDllName = "api-ms-win-security-base-l1-1-0.dll";                   
        internal const string CreateProcessInComputeSystemDllName = "ComputeCore.dll";                       
        internal const string CLSIDFromProgIDDllName = "api-ms-win-core-com-l1-1-0.dll";                     
        internal const string LoadLibraryEx = "api-ms-win-core-libraryloader-l1-1-0.dll";                    
        internal const string FreeLibrary = "api-ms-win-core-libraryloader-l1-1-0.dll";                      
        internal const string EventActivityIdControlDllName = "api-ms-win-eventing-provider-l1-1-0.dll";     
        internal const string GetConsoleCPDllName = "api-ms-win-core-console-l1-1-0.dll";                    
        internal const string GetConsoleOutputCPDllName = "api-ms-win-core-console-l1-1-0.dll";              
        internal const string GetConsoleWindowDllName = "api-ms-win-core-kernel32-legacy-l1-1-0.dll";        
        internal const string GetDCDllName = "ext-ms-win-ntuser-dc-access-ext-l1-1-0.dll";                   
        internal const string ReleaseDCDllName = "ext-ms-win-ntuser-dc-access-ext-l1-1-0.dll";               
        internal const string TranslateCharsetInfoDllName = "ext-ms-win-gdi-font-l1-1-1.dll";                
        internal const string GetTextMetricsDllName = "ext-ms-win-gdi-font-l1-1-1.dll";                      
        internal const string GetCharWidth32DllName = "ext-ms-win-gdi-font-l1-1-1.dll";                      
        internal const string FlushConsoleInputBufferDllName = "api-ms-win-core-console-l2-1-0.dll";         
        internal const string FillConsoleOutputAttributeDllName = "api-ms-win-core-console-l2-1-0.dll";      
        internal const string FillConsoleOutputCharacterDllName = "api-ms-win-core-console-l2-1-0.dll";      
        internal const string WriteConsoleDllName = "api-ms-win-core-console-l1-1-0.dll";                    
        internal const string GetConsoleTitleDllName = "api-ms-win-core-console-l2-1-0.dll";                 
        internal const string SetConsoleTitleDllName = "api-ms-win-core-console-l2-1-0.dll";                 
        internal const string GetConsoleModeDllName = "api-ms-win-core-console-l1-1-0.dll";                  
        internal const string GetConsoleScreenBufferInfoDllName = "api-ms-win-core-console-l2-1-0.dll";      
        internal const string GetFileTypeDllName = "api-ms-win-core-file-l1-1-0.dll";                        
        internal const string GetLargestConsoleWindowSizeDllName = "api-ms-win-core-console-l2-1-0.dll";     
        internal const string ReadConsoleDllName = "api-ms-win-core-console-l1-1-0.dll";                     
        internal const string PeekConsoleInputDllName = "api-ms-win-core-console-l2-1-0.dll";                
        internal const string GetNumberOfConsoleInputEventsDllName = "api-ms-win-core-console-l1-1-0.dll";   
        internal const string SetConsoleCtrlHandlerDllName = "api-ms-win-core-console-l1-1-0.dll";           
        internal const string SetConsoleModeDllName = "api-ms-win-core-console-l1-1-0.dll";                  
        internal const string SetConsoleScreenBufferSizeDllName = "api-ms-win-core-console-l2-1-0.dll";      
        internal const string SetConsoleTextAttributeDllName = "api-ms-win-core-console-l2-1-0.dll";         
        internal const string SetConsoleWindowInfoDllName = "api-ms-win-core-console-l2-1-0.dll";            
        internal const string WriteConsoleOutputDllName = "api-ms-win-core-console-l2-1-0.dll";              
        internal const string ReadConsoleOutputDllName = "api-ms-win-core-console-l2-1-0.dll";               
        internal const string ScrollConsoleScreenBufferDllName = "api-ms-win-core-console-l2-1-0.dll";       
        internal const string SendInputDllName = "ext-ms-win-ntuser-keyboard-l1-2-1.dll";                    
        internal const string GetConsoleCursorInfoDllName = "api-ms-win-core-console-l2-1-0.dll";            
        internal const string SetConsoleCursorInfoDllName = "api-ms-win-core-console-l2-1-0.dll";            
        internal const string ReadConsoleInputDllName = "api-ms-win-core-console-l1-1-0.dll";                
        internal const string GetVersionExDllName = "api-ms-win-core-sysinfo-l1-1-0.dll";                    
        internal const string FormatMessageDllName = "api-ms-win-core-localization-l1-2-0.dll";              
        internal const string CreateToolhelp32SnapshotDllName = "api-ms-win-core-toolhelp-l1-1-0";           
        internal const string Process32FirstDllName = "api-ms-win-core-toolhelp-l1-1-0";                     
        internal const string Process32NextDllName = "api-ms-win-core-toolhelp-l1-1-0";                      
        internal const string GetACPDllName = "api-ms-win-core-localization-l1-2-0.dll";                     
        internal const string DeleteServiceDllName = "api-ms-win-service-management-l1-1-0.dll";             
        internal const string QueryServiceConfigDllName = "api-ms-win-service-management-l2-1-0.dll";        
        internal const string QueryServiceConfig2DllName = "api-ms-win-service-management-l2-1-0.dll";       
        internal const string SetServiceObjectSecurityDllName = "api-ms-win-service-management-l2-1-0.dll";  
    }
}
