// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections.Generic;
using System.Transactions;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    
    public enum PSTransactionStatus
    {
        
        RolledBack = 0,

        
        Committed = 1,

        
        Active = 2
    }

    
    public sealed class PSTransaction : IDisposable
    {
        
        internal PSTransaction(RollbackSeverity rollbackPreference, TimeSpan timeout)
        {
            _transaction = new CommittableTransaction(timeout);
            RollbackPreference = rollbackPreference;
            _subscriberCount = 1;
        }

        
        internal PSTransaction(CommittableTransaction transaction, RollbackSeverity severity)
        {
            _transaction = transaction;
            RollbackPreference = severity;
            _subscriberCount = 1;
        }

        private CommittableTransaction _transaction;

        
        public RollbackSeverity RollbackPreference { get; }

        
        public int SubscriberCount
        {
            get
            {
                // Verify the transaction hasn't been rolled back beneath us
                if (this.IsRolledBack)
                {
                    this.SubscriberCount = 0;
                }

                return _subscriberCount;
            }

            set { _subscriberCount = value; }
        }

        private int _subscriberCount;

        
        public PSTransactionStatus Status
        {
            get
            {
                if (IsRolledBack)
                {
                    return PSTransactionStatus.RolledBack;
                }
                else if (IsCommitted)
                {
                    return PSTransactionStatus.Committed;
                }
                else
                {
                    return PSTransactionStatus.Active;
                }
            }
        }

        
        internal void Activate()
        {
            Transaction.Current = _transaction;
        }

        
        internal void Commit()
        {
            _transaction.Commit();
            IsCommitted = true;
        }

        
        internal void Rollback()
        {
            _transaction.Rollback();
            _isRolledBack = true;
        }

        
        internal bool IsRolledBack
        {
            get
            {
                // Check if it's been aborted underneath us
                if (
                    (!_isRolledBack) &&
                    (_transaction != null) &&
                    (_transaction.TransactionInformation.Status == TransactionStatus.Aborted))
                {
                    _isRolledBack = true;
                }

                return _isRolledBack;
            }

            set
            {
                _isRolledBack = value;
            }
        }

        private bool _isRolledBack = false;

        
        internal bool IsCommitted { get; set; } = false;

        
        ~PSTransaction()
        {
            Dispose(false);
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">
        /// Whether to actually dispose the object.
        /// </param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                }
            }
        }
    }

    
    public sealed class PSTransactionContext : IDisposable
    {
        
        internal PSTransactionContext(PSTransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
            transactionManager.SetActive();
        }

        private PSTransactionManager _transactionManager;

        
        ~PSTransactionContext()
        {
            Dispose(false);
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">
        /// Whether to actually dispose the object.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transactionManager.ResetActive();
            }
        }
    }

    
    public enum RollbackSeverity
    {
        
        Error,

        
        TerminatingError,

        
        Never
    }
}

namespace System.Management.Automation.Internal
{
    
    internal sealed class PSTransactionManager : IDisposable
    {
        
        internal PSTransactionManager()
        {
            _transactionStack = new Stack<PSTransaction>();
            _transactionStack.Push(null);
        }

        
        internal static IDisposable GetEngineProtectionScope()
        {
            if (s_engineProtectionEnabled && (Transaction.Current != null))
            {
                return new System.Transactions.TransactionScope(
                    System.Transactions.TransactionScopeOption.Suppress);
            }
            else
            {
                return null;
            }
        }

        
        internal static void EnableEngineProtection()
        {
            s_engineProtectionEnabled = true;
        }

        private static bool s_engineProtectionEnabled = false;

        
        internal RollbackSeverity RollbackPreference
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if (currentTransaction == null)
                {
                    string error = TransactionStrings.NoTransactionActive;

                    // This is not an expected condition, and is just protective
                    // coding.
#pragma warning suppress 56503
                    throw new InvalidOperationException(error);
                }

                return currentTransaction.RollbackPreference;
            }
        }

        
        internal void CreateOrJoin()
        {
            CreateOrJoin(RollbackSeverity.Error, TimeSpan.FromMinutes(1));
        }

        
        internal void CreateOrJoin(RollbackSeverity rollbackPreference, TimeSpan timeout)
        {
            PSTransaction currentTransaction = _transactionStack.Peek();

            // There is a transaction on the stack
            if (currentTransaction != null)
            {
                // If you are already in a transaction that has been aborted, or committed,
                // create it.
                if (currentTransaction.IsRolledBack || currentTransaction.IsCommitted)
                {
                    // Clean up the "used" one
                    _transactionStack.Pop().Dispose();

                    // And add a new one to the stack
                    _transactionStack.Push(new PSTransaction(rollbackPreference, timeout));
                }
                else
                {
                    // This is a usable one. Add a subscriber to it.
                    currentTransaction.SubscriberCount++;
                }
            }
            else
            {
                // Add a new transaction to the stack
                _transactionStack.Push(new PSTransaction(rollbackPreference, timeout));
            }
        }

        
        internal void CreateNew()
        {
            CreateNew(RollbackSeverity.Error, TimeSpan.FromMinutes(1));
        }

        
        internal void CreateNew(RollbackSeverity rollbackPreference, TimeSpan timeout)
        {
            _transactionStack.Push(new PSTransaction(rollbackPreference, timeout));
        }

        
        internal void Commit()
        {
            PSTransaction currentTransaction = _transactionStack.Peek();

            // Should not be able to commit a transaction that is not active
            if (currentTransaction == null)
            {
                string error = TransactionStrings.NoTransactionActiveForCommit;
                throw new InvalidOperationException(error);
            }

            // If you are already in a transaction that has been aborted
            if (currentTransaction.IsRolledBack)
            {
                string error = TransactionStrings.TransactionRolledBackForCommit;
                throw new TransactionAbortedException(error);
            }

            // If you are already in a transaction that has been committed
            if (currentTransaction.IsCommitted)
            {
                string error = TransactionStrings.CommittedTransactionForCommit;
                throw new InvalidOperationException(error);
            }

            if (currentTransaction.SubscriberCount == 1)
            {
                currentTransaction.Commit();
                currentTransaction.SubscriberCount = 0;
            }
            else
            {
                currentTransaction.SubscriberCount--;
            }

            // Now that we've committed, go back to the last available transaction
            while ((_transactionStack.Count > 2) &&
                (_transactionStack.Peek().IsRolledBack || _transactionStack.Peek().IsCommitted))
            {
                _transactionStack.Pop().Dispose();
            }
        }

        
        internal void Rollback()
        {
            Rollback(false);
        }

        
        internal void Rollback(bool suppressErrors)
        {
            PSTransaction currentTransaction = _transactionStack.Peek();

            // Should not be able to roll back a transaction that is not active
            if (currentTransaction == null)
            {
                string error = TransactionStrings.NoTransactionActiveForRollback;
                throw new InvalidOperationException(error);
            }

            // If you are already in a transaction that has been aborted
            if (currentTransaction.IsRolledBack)
            {
                if (!suppressErrors)
                {
                    // Otherwise, you should not be able to roll it back.
                    string error = TransactionStrings.TransactionRolledBackForRollback;
                    throw new TransactionAbortedException(error);
                }
            }

            // See if they've already committed the transaction
            if (currentTransaction.IsCommitted)
            {
                if (!suppressErrors)
                {
                    string error = TransactionStrings.CommittedTransactionForRollback;
                    throw new InvalidOperationException(error);
                }
            }

            // Roll back the transaction if it hasn't been rolled back
            currentTransaction.SubscriberCount = 0;
            currentTransaction.Rollback();

            // Now that we've rolled back, go back to the last available transaction
            while ((_transactionStack.Count > 2) &&
                (_transactionStack.Peek().IsRolledBack || _transactionStack.Peek().IsCommitted))
            {
                _transactionStack.Pop().Dispose();
            }
        }

        
        internal void SetBaseTransaction(CommittableTransaction transaction, RollbackSeverity severity)
        {
            if (this.HasTransaction)
            {
                throw new InvalidOperationException(TransactionStrings.BaseTransactionMustBeFirst);
            }

            PSTransaction currentTransaction = _transactionStack.Peek();

            // If there is a "used" transaction at the top of the stack, clean it up
            while (_transactionStack.Peek() != null &&
                (_transactionStack.Peek().IsRolledBack || _transactionStack.Peek().IsCommitted))
            {
                _transactionStack.Pop().Dispose();
            }

            _baseTransaction = new PSTransaction(transaction, severity);
            _transactionStack.Push(_baseTransaction);
        }

        
        internal void ClearBaseTransaction()
        {
            if (_baseTransaction == null)
            {
                throw new InvalidOperationException(TransactionStrings.BaseTransactionNotSet);
            }

            if (_transactionStack.Peek() != _baseTransaction)
            {
                throw new InvalidOperationException(TransactionStrings.BaseTransactionNotActive);
            }

            _transactionStack.Pop().Dispose();
            _baseTransaction = null;
        }

        private Stack<PSTransaction> _transactionStack;
        private PSTransaction _baseTransaction;

        
        internal PSTransaction GetCurrent()
        {
            return _transactionStack.Peek();
        }

        
        internal void SetActive()
        {
            PSTransactionManager.EnableEngineProtection();

            PSTransaction currentTransaction = _transactionStack.Peek();

            // Should not be able to activate a transaction that is not active
            if (currentTransaction == null)
            {
                string error = TransactionStrings.NoTransactionForActivation;
                throw new InvalidOperationException(error);
            }

            // If you are already in a transaction that has been aborted, you should
            // not be able to activate it.
            if (currentTransaction.IsRolledBack)
            {
                string error = TransactionStrings.NoTransactionForActivationBecauseRollback;
                throw new TransactionAbortedException(error);
            }

            _previousActiveTransaction = Transaction.Current;
            currentTransaction.Activate();
        }

        private Transaction _previousActiveTransaction;

        
        internal void ResetActive()
        {
            // Even if you are in a transaction that has been aborted, you
            // should still be able to restore the current transaction.

            Transaction.Current = _previousActiveTransaction;
            _previousActiveTransaction = null;
        }

        
        internal bool HasTransaction
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if ((currentTransaction != null) &&
                    (!currentTransaction.IsCommitted) &&
                    (!currentTransaction.IsRolledBack))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        
        internal bool IsLastTransactionCommitted
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if (currentTransaction != null)
                {
                    return currentTransaction.IsCommitted;
                }
                else
                {
                    return false;
                }
            }
        }

        
        internal bool IsLastTransactionRolledBack
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if (currentTransaction != null)
                {
                    return currentTransaction.IsRolledBack;
                }
                else
                {
                    return false;
                }
            }
        }

        
        ~PSTransactionManager()
        {
            Dispose(false);
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">
        /// Whether to actually dispose the object.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "baseTransaction", Justification = "baseTransaction should not be disposed since we do not own it - it belongs to the caller")]
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResetActive();

                while (_transactionStack.Peek() != null)
                {
                    PSTransaction currentTransaction = _transactionStack.Pop();

                    if (currentTransaction != _baseTransaction)
                    {
                        currentTransaction.Dispose();
                    }
                }
            }
        }
    }
}
