// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Transactions;

namespace Microsoft.PowerShell.Commands.Management
{
    
    public class TransactedString : IEnlistmentNotification
    {
        private StringBuilder _value;
        private StringBuilder _temporaryValue;
        private Transaction _enlistedTransaction = null;

        
        public TransactedString() : this(string.Empty)
        {
        }

        
        /// <param name="value">
        /// The initial value of the transacted string.
        /// </param>
        public TransactedString(string value)
        {
            _value = new StringBuilder(value);
            _temporaryValue = null;
        }

        
        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            _value = new StringBuilder(_temporaryValue.ToString());
            _temporaryValue = null;
            _enlistedTransaction = null;
            enlistment.Done();
        }

        
        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            _temporaryValue = null;
            _enlistedTransaction = null;
            enlistment.Done();
        }

        
        void IEnlistmentNotification.InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        
        /// <param name="text">
        /// The text to append.
        /// </param>
        public void Append(string text)
        {
            ValidateTransactionOrEnlist();

            if (_enlistedTransaction != null)
            {
                _temporaryValue.Append(text);
            }
            else
            {
                _value.Append(text);
            }
        }

        
        /// <param name="startIndex">
        /// The position in the string from which to start removing.
        /// </param>
        /// <param name="length">
        /// The length of text to remove.
        /// </param>
        public void Remove(int startIndex, int length)
        {
            ValidateTransactionOrEnlist();

            if (_enlistedTransaction != null)
            {
                _temporaryValue.Remove(startIndex, length);
            }
            else
            {
                _value.Remove(startIndex, length);
            }
        }

        
        public int Length
        {
            get
            {
                // If we're not in a transaction, or we are in a different transaction than the one we
                // enlisted to, return the publicly visible state.
                if (
                    (Transaction.Current == null) ||
                    (_enlistedTransaction != Transaction.Current))
                {
                    return _value.Length;
                }
                else
                {
                    return _temporaryValue.Length;
                }
            }
        }

        
        public override string ToString()
        {
            // If we're not in a transaction, or we are in a different transaction than the one we
            // enlisted to, return the publicly visible state.
            if (
               (Transaction.Current == null) ||
               (_enlistedTransaction != Transaction.Current))
            {
                return _value.ToString();
            }
            else
            {
                return _temporaryValue.ToString();
            }
        }

        private void ValidateTransactionOrEnlist()
        {
            // We're in a transaction
            if (Transaction.Current != null)
            {
                // We haven't yet been called inside of a transaction. So enlist
                // in the transaction, and store our save point
                if (_enlistedTransaction == null)
                {
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                    _enlistedTransaction = Transaction.Current;

                    _temporaryValue = new StringBuilder(_value.ToString());
                }
                // We're already enlisted in a transaction
                else
                {
                    // And we're in that transaction
                    if (Transaction.Current != _enlistedTransaction)
                    {
                        throw new InvalidOperationException("Cannot modify string. It has been modified by another transaction.");
                    }
                }
            }
            // We're not in a transaction
            else
            {
                // If we're not subscribed to a transaction, modify the underlying value
                if (_enlistedTransaction != null)
                {
                    throw new InvalidOperationException("Cannot modify string. It has been modified by another transaction.");
                }
            }
        }
    }
}
