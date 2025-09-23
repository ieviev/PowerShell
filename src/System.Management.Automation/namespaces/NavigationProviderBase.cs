// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Provider
{
    #region NavigationCmdletProvider

    
    public abstract class NavigationCmdletProvider : ContainerCmdletProvider
    {
        #region Internal methods

        
        internal string MakePath(
            string parent,
            string child,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return MakePath(parent, child);
        }

        
        internal string GetParentPath(
            string path,
            string root,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return GetParentPath(path, root);
        }

        
        internal string NormalizeRelativePath(
            string path,
            string basePath,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return NormalizeRelativePath(path, basePath);
        }

        
        internal string GetChildName(
            string path,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return GetChildName(path);
        }

        
        internal bool IsItemContainer(
            string path,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return IsItemContainer(path);
        }

        
        internal void MoveItem(
            string path,
            string destination,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            MoveItem(path, destination);
        }

        
        internal object MoveItemDynamicParameters(
            string path,
            string destination,
            CmdletProviderContext context)
        {
            Context = context;
            return MoveItemDynamicParameters(path, destination);
        }

        #endregion Internal methods

        #region protected methods

        
        protected virtual string MakePath(string parent, string child)
        {
            return MakePath(parent, child, childIsLeaf: false);
        }

        
        protected string MakePath(string parent, string child, bool childIsLeaf)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                string result = null;

                if (parent == null &&
                    child == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(parent));
                }

                if (string.IsNullOrEmpty(parent) &&
                    string.IsNullOrEmpty(child))
                {
                    result = string.Empty;
                }
                else if (string.IsNullOrEmpty(parent) &&
                         !string.IsNullOrEmpty(child))
                {
                    result = NormalizePath(child);
                }
                else if (!string.IsNullOrEmpty(parent) &&
                         (string.IsNullOrEmpty(child) ||
                          child.Equals(StringLiterals.DefaultPathSeparatorString, StringComparison.Ordinal) ||
                          child.Equals(StringLiterals.AlternatePathSeparatorString, StringComparison.Ordinal)))
                {
                    if (parent.EndsWith(StringLiterals.DefaultPathSeparator))
                    {
                        result = parent;
                    }
                    else
                    {
                        result = parent + StringLiterals.DefaultPathSeparator;
                    }
                }
                else
                {
                    // Both parts are not empty so join them
                    // 'childIsLeaf == true' indicates that 'child' is actually the name of a child item and
                    // guaranteed to exist. In this case, we don't normalize the child path.
                    if (childIsLeaf)
                    {
                        parent = NormalizePath(parent);
                    }
                    else
                    {
                        // Normalize the path so that only the default path separator is used as a
                        // separator even if the user types the alternate slash.
                        parent = NormalizePath(parent);
                        child = NormalizePath(child);
                    }

                    ReadOnlySpan<char> appendChild = child.AsSpan();
                    if (child.StartsWith(StringLiterals.DefaultPathSeparator))
                    {
                        appendChild = appendChild.Slice(1);
                    }

                    result = IO.Path.Join(parent.AsSpan(), appendChild);
                }

                return result;
            }
        }

        
        protected virtual string GetParentPath(string path, string root)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                string parentPath = null;

                // Verify the parameters

                if (string.IsNullOrEmpty(path))
                {
                    throw PSTraceSource.NewArgumentException(nameof(path));
                }

                if (root == null)
                {
                    if (PSDriveInfo != null)
                    {
                        root = PSDriveInfo.Root;
                    }
                }

                // Normalize the path

                path = NormalizePath(path);
                path = path.TrimEnd(StringLiterals.DefaultPathSeparator);
                string rootPath = string.Empty;

                if (root != null)
                {
                    rootPath = NormalizePath(root);
                }

                // Check to see if the path is equal to the root
                // of the virtual drive

                if (string.Equals(
                    path,
                    rootPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    parentPath = string.Empty;
                }
                else
                {
                    int lastIndex = path.LastIndexOf(StringLiterals.DefaultPathSeparator);

                    if (lastIndex != -1)
                    {
                        if (lastIndex == 0)
                        {
                            ++lastIndex;
                        }
                        // Get the parent directory

                        parentPath = path.Substring(0, lastIndex);
                    }
                    else
                    {
                        parentPath = string.Empty;
                    }
                }

                return parentPath;
            }
        }

        
        protected virtual string NormalizeRelativePath(
            string path,
            string basePath)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return ContractRelativePath(path, basePath, false, Context);
            }
        }

        internal string ContractRelativePath(
            string path,
            string basePath,
            bool allowNonExistingPaths,
            CmdletProviderContext context)
        {
            Context = context;

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (path.Length == 0)
            {
                return string.Empty;
            }

            basePath ??= string.Empty;

            providerBaseTracer.WriteLine("basePath = {0}", basePath);

            string result = path;
            bool originalPathHadTrailingSlash = false;

            string normalizedPath = path;
            string normalizedBasePath = basePath;

            // NTRAID#Windows 7-697922-2009/06/29-leeholm
            // WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND
            //
            // This path normalization got moved here from the MakePath override in V2 to prevent
            // over-normalization of paths. This was a net-improvement for providers that use the default
            // implementations, but now incorrectly replaces forward slashes with back slashes during the call to
            // GetParentPath and GetChildName. This breaks providers that are sensitive to slash direction, the only
            // one we are aware of being the Active Directory provider. This change prevents this over-normalization
            // from being done on AD paths.
            //
            // For more information, see Win7:695292. Do not change this code without closely working with the
            // Active Directory team.
            //
            // WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND
            if (!string.Equals(context.ProviderInstance.ProviderInfo.FullName,
                @"Microsoft.ActiveDirectory.Management\ActiveDirectory", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = NormalizePath(path);
                normalizedBasePath = NormalizePath(basePath);
            }

            do // false loop
            {
                // Convert to the correct path separators and trim trailing separators
                string originalPath = path;
                Stack<string> tokenizedPathStack = null;

                if (path.EndsWith(StringLiterals.DefaultPathSeparator))
                {
                    path = path.TrimEnd(StringLiterals.DefaultPathSeparator);
                    originalPathHadTrailingSlash = true;
                }

                basePath = basePath.TrimEnd(StringLiterals.DefaultPathSeparator);

                // See if the base and the path are already the same. We resolve this to
                // ..\Leaf, since resolving "." to "." doesn't offer much information.
                if (string.Equals(normalizedPath, normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
                    (!originalPath.EndsWith(StringLiterals.DefaultPathSeparator)))
                {
                    string childName = GetChildName(path);
                    result = MakePath("..", childName);
                    break;
                }

                // If the base path isn't really a base, then we resolve to a parent
                // path (such as ../../foo)
                if (!normalizedPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
                    (basePath.Length > 0))
                {
                    result = string.Empty;
                    string commonBase = GetCommonBase(normalizedPath, normalizedBasePath);

                    Stack<string> parentNavigationStack = TokenizePathToStack(normalizedBasePath, commonBase);
                    int parentPopCount = parentNavigationStack.Count;

                    if (string.IsNullOrEmpty(commonBase))
                    {
                        parentPopCount--;
                    }

                    for (int leafCounter = 0; leafCounter < parentPopCount; leafCounter++)
                    {
                        result = MakePath("..", result);
                    }

                    // This is true if we get passed a base path like:
                    //    c:\directory1\directory2
                    // and an actual path of
                    //    c:\directory1
                    // Which happens when the user is in c:\directory1\directory2
                    // and wants to resolve something like:
                    // ..\..\dir*
                    // In that case (as above,) we keep the ..\..\directory1
                    // instead of ".." as would usually be returned
                    if (!string.IsNullOrEmpty(commonBase))
                    {
                        if (string.Equals(normalizedPath, commonBase, StringComparison.OrdinalIgnoreCase) &&
                            (!normalizedPath.EndsWith(StringLiterals.DefaultPathSeparator)))
                        {
                            string childName = GetChildName(path);
                            result = MakePath("..", result);
                            result = MakePath(result, childName);
                        }
                        else
                        {
                            string[] childNavigationItems = TokenizePathToStack(normalizedPath, commonBase).ToArray();

                            for (int leafCounter = 0; leafCounter < childNavigationItems.Length; leafCounter++)
                            {
                                result = MakePath(result, childNavigationItems[leafCounter]);
                            }
                        }
                    }
                }
                // Otherwise, we resolve to a child path (such as foo/bar)
                else
                {
                    tokenizedPathStack = TokenizePathToStack(path, basePath);

                    // Now we have to normalize the path
                    // by processing each token on the stack
                    Stack<string> normalizedPathStack;

                    try
                    {
                        normalizedPathStack = NormalizeThePath(tokenizedPathStack, path, basePath, allowNonExistingPaths);
                    }
                    catch (ArgumentException argumentException)
                    {
                        WriteError(new ErrorRecord(argumentException, argumentException.GetType().FullName, ErrorCategory.InvalidArgument, null));
                        result = null;
                        break;
                    }

                    // Now that the path has been normalized, create the relative path
                    result = CreateNormalizedRelativePathFromStack(normalizedPathStack);
                }
            } while (false);

            if (originalPathHadTrailingSlash)
            {
                result += StringLiterals.DefaultPathSeparator;
            }

            return result;
        }

        
        private string GetCommonBase(string path1, string path2)
        {
            // Always see if the shorter path is a substring of the
            // longer path. If it is not, take the child off of the longer
            // path and compare again.

            while (!string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase))
            {
                if (path2.Length > path1.Length)
                {
                    path2 = GetParentPath(path2, null);
                }
                else
                {
                    path1 = GetParentPath(path1, null);
                }
            }

            return path1;
        }

        
        protected virtual string GetChildName(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                // Verify the parameters

                if (string.IsNullOrEmpty(path))
                {
                    throw PSTraceSource.NewArgumentException(nameof(path));
                }

                // Normalize the path
                path = NormalizePath(path);
                // Trim trailing back slashes
                path = path.TrimEnd(StringLiterals.DefaultPathSeparator);
                string result = null;

                int separatorIndex = path.LastIndexOf(StringLiterals.DefaultPathSeparator);

                // Since there was no path separator return the entire path
                if (separatorIndex == -1)
                {
                    result = path;
                }
                // If the full path existed, we must semantically evaluate the parent path
                else if (ItemExists(path, Context))
                {
                    string parentPath = GetParentPath(path, null);

                    // No parent, return the entire path
                    if (string.IsNullOrEmpty(parentPath))
                        result = path;
                    // If the parent path ends with the path separator, we can't split
                    // the path based on that
                    else if (parentPath.IndexOf(StringLiterals.DefaultPathSeparator) == (parentPath.Length - 1))
                    {
                        separatorIndex = path.IndexOf(parentPath, StringComparison.OrdinalIgnoreCase) + parentPath.Length;
                        result = path.Substring(separatorIndex);
                    }
                    else
                    {
                        separatorIndex = path.IndexOf(parentPath, StringComparison.OrdinalIgnoreCase) + parentPath.Length;
                        result = path.Substring(separatorIndex + 1);
                    }
                }
                // Otherwise, use lexical parsing
                else
                {
                    result = path.Substring(separatorIndex + 1);
                }

                return result;
            }
        }

        
        protected virtual bool IsItemContainer(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual void MoveItem(
            string path,
            string destination)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object MoveItemDynamicParameters(
            string path,
            string destination)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        #endregion Protected methods

        #region private members

        
        private string NormalizePath(string path)
        {
            // If we have a mix of slashes, then we may introduce an error by normalizing the path.
            // For example: path HKCU:\Test\/ is pointing to a subkey '/' of 'HKCU:\Test', if we
            // normalize it, then we will get a wrong path.
            //
            // Fast return if nothing to normalize.
            if (!path.Contains(StringLiterals.AlternatePathSeparator))
            {
                return path;
            }

            bool pathHasBackSlash = path.Contains(StringLiterals.DefaultPathSeparator);
            string normalizedPath;

            // There is a mix of slashes & the path is rooted & the path exists without normalization.
            // In this case, we might want to skip the normalization to the path.
            if (pathHasBackSlash && IsAbsolutePath(path) && ItemExists(path))
            {
                // 1. The path exists and ends with a forward slash, in this case, it's very possible the ending forward slash
                //    make sense to the underlying provider, so we skip normalization
                // 2. The path exists, but not anymore after normalization, then we skip normalization
                if (path.EndsWith(StringLiterals.AlternatePathSeparator))
                {
                    return path;
                }

                normalizedPath = path.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);

                if (!ItemExists(normalizedPath))
                {
                    return path;
                }
                else
                {
                    return normalizedPath;
                }
            }

            normalizedPath = path.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);

            return normalizedPath;
        }

        
        private bool IsAbsolutePath(string path)
        {
            bool result = false;

            if (LocationGlobber.IsAbsolutePath(path))
            {
                result = true;
            }
            else if (this.PSDriveInfo != null && !string.IsNullOrEmpty(this.PSDriveInfo.Root) &&
                     path.StartsWith(this.PSDriveInfo.Root, StringComparison.OrdinalIgnoreCase))
            {
                result = true;
            }

            return result;
        }

        
        private Stack<string> TokenizePathToStack(string path, string basePath)
        {
            Stack<string> tokenizedPathStack = new Stack<string>();
            string tempPath = path;
            string previousParent = path;

            while (tempPath.Length > basePath.Length)
            {
                // Get the child name and push it onto the stack
                // if its valid

                string childName = GetChildName(tempPath);
                if (string.IsNullOrEmpty(childName))
                {
                    // Push the parent on and then stop
                    tokenizedPathStack.Push(tempPath);
                    break;
                }

                providerBaseTracer.WriteLine("tokenizedPathStack.Push({0})", childName);
                tokenizedPathStack.Push(childName);

                // Get the parent path and verify if we have to continue
                // tokenizing

                tempPath = GetParentPath(tempPath, basePath);
                if (tempPath.Length >= previousParent.Length)
                {
                    break;
                }

                previousParent = tempPath;
            }

            return tokenizedPathStack;
        }

        
        private static Stack<string> NormalizeThePath(
            Stack<string> tokenizedPathStack, string path,
            string basePath, bool allowNonExistingPaths)
        {
            Stack<string> normalizedPathStack = new Stack<string>();

            while (tokenizedPathStack.Count > 0)
            {
                string childName = tokenizedPathStack.Pop();

                providerBaseTracer.WriteLine("childName = {0}", childName);

                // Ignore the current directory token
                if (childName.Equals(".", StringComparison.OrdinalIgnoreCase))
                {
                    // Just ignore it and move on.
                    continue;
                }

                // Make sure we don't have
                if (childName.Equals("..", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedPathStack.Count > 0)
                    {
                        // Pop the result and continue processing
                        string poppedName = normalizedPathStack.Pop();
                        providerBaseTracer.WriteLine("normalizedPathStack.Pop() : {0}", poppedName);
                        continue;
                    }
                    else
                    {
                        if (!allowNonExistingPaths)
                        {
                            PSArgumentException e =
                                (PSArgumentException)PSTraceSource.NewArgumentException(
                                    nameof(path),
                                    SessionStateStrings.NormalizeRelativePathOutsideBase,
                                    path,
                                    basePath);
                            throw e;
                        }
                    }
                }

                providerBaseTracer.WriteLine("normalizedPathStack.Push({0})", childName);
                normalizedPathStack.Push(childName);
            }

            return normalizedPathStack;
        }

        
        private string CreateNormalizedRelativePathFromStack(Stack<string> normalizedPathStack)
        {
            string leafElement = string.Empty;

            while (normalizedPathStack.Count > 0)
            {
                if (string.IsNullOrEmpty(leafElement))
                {
                    leafElement = normalizedPathStack.Pop();
                }
                else
                {
                    string parentElement = normalizedPathStack.Pop();
                    leafElement = MakePath(parentElement, leafElement);
                }
            }

            return leafElement;
        }

        #endregion private members
    }

    #endregion NavigationCmdletProvider
}
