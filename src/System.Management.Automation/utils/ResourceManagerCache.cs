// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Reflection;
using System.Resources;

namespace System.Management.Automation
{
    
    internal static class ResourceManagerCache
    {
        
        private static readonly Dictionary<string, Dictionary<string, ResourceManager>> s_resourceManagerCache =
            new Dictionary<string, Dictionary<string, ResourceManager>>(StringComparer.OrdinalIgnoreCase);

        
        private static readonly object s_syncRoot = new object();

        
        internal static ResourceManager GetResourceManager(
            Assembly assembly,
            string baseName)
        {
            if (assembly == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(assembly));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentException(nameof(baseName));
            }

            // Check to see if the manager is already in the cache

            ResourceManager manager = null;
            Dictionary<string, ResourceManager> baseNameCache;

            string assemblyManifestFileLocation = assembly.Location;
            lock (s_syncRoot)
            {
                // First do the lookup based on the assembly location

                if (s_resourceManagerCache.TryGetValue(assemblyManifestFileLocation, out baseNameCache) && baseNameCache != null)
                {
                    // Now do the lookup based on the resource base name
                    baseNameCache.TryGetValue(baseName, out manager);
                }
            }

            // If it's not in the cache, create it an add it.
            if (manager == null)
            {
                manager = InitRMWithAssembly(baseName, assembly);

                // Add the new resource manager to the hash

                if (baseNameCache != null)
                {
                    lock (s_syncRoot)
                    {
                        // Since the assembly is already cached, we just have
                        // to cache the base name entry

                        baseNameCache[baseName] = manager;
                    }
                }
                else
                {
                    // Since the assembly wasn't cached, we have to create base name
                    // cache entry and then add it into the cache keyed by the assembly
                    // location

                    var baseNameCacheEntry = new Dictionary<string, ResourceManager>();

                    baseNameCacheEntry[baseName] = manager;

                    lock (s_syncRoot)
                    {
                        s_resourceManagerCache[assemblyManifestFileLocation] = baseNameCacheEntry;
                    }
                }
            }

            Diagnostics.Assert(
                manager != null,
                "If the manager was not already created, it should have been dynamically created or an exception should have been thrown");

            return manager;
        }

        
        private static bool s_DFT_monitorFailingResourceLookup = true;

        internal static bool DFT_DoMonitorFailingResourceLookup
        {
            get { return ResourceManagerCache.s_DFT_monitorFailingResourceLookup; }

            set { ResourceManagerCache.s_DFT_monitorFailingResourceLookup = value; }
        }

        
        internal static string GetResourceString(
            Assembly assembly,
            string baseName,
            string resourceId)
        {
            if (assembly == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(assembly));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentException(nameof(baseName));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentException(nameof(resourceId));
            }

            ResourceManager resourceManager = null;
            string text = string.Empty;

            // For a non-existing resource defined by {assembly,baseName,resourceId}
            // MissingManifestResourceException is thrown only at the time when resource retrieval method
            // such as ResourceManager.GetString or ResourceManager.GetObject is called,
            // not when you instantiate a ResourceManager object.
            try
            {
                // try with original baseName first
                // if it fails then try with alternative resource path format
                resourceManager = GetResourceManager(assembly, baseName);
                text = resourceManager.GetString(resourceId);
            }
            catch (MissingManifestResourceException)
            {
                const string resourcesSubstring = ".resources.";
                int resourcesSubstringIndex = baseName.IndexOf(resourcesSubstring);
                string newBaseName = string.Empty;
                if (resourcesSubstringIndex != -1)
                {
                    newBaseName = baseName.Substring(resourcesSubstringIndex + resourcesSubstring.Length); // e.g.  "FileSystemProviderStrings"
                }
                else
                {
                    newBaseName = string.Concat(assembly.GetName().Name, resourcesSubstring, baseName); // e.g. "System.Management.Automation.resources.FileSystemProviderStrings"
                }

                resourceManager = GetResourceManager(assembly, newBaseName);
                text = resourceManager.GetString(resourceId);
            }

            if (string.IsNullOrEmpty(text) && s_DFT_monitorFailingResourceLookup)
            {
                Diagnostics.Assert(false,
                    "Lookup failure: baseName " + baseName + " resourceId " + resourceId);
            }

            return text;
        }

        
        private static ResourceManager InitRMWithAssembly(string baseName, Assembly assemblyToUse)
        {
            ResourceManager rm = null;

            if (baseName != null && assemblyToUse != null)
            {
                rm = new ResourceManager(baseName, assemblyToUse);
            }
            else
            {
                // 2004/10/11-JonN Do we need a better error message?  I don't think so,
                // since this is private.
                throw PSTraceSource.NewArgumentException(nameof(assemblyToUse));
            }

            return rm;
        }
    }
}
