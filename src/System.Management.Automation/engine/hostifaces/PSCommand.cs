// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
    public sealed class PSCommand
    {
        #region Private Fields

        private PowerShell _owner;
        private CommandCollection _commands;
        private Command _currentCommand;

        #endregion

        #region Constructor

        
        public PSCommand()
        {
            Initialize(null, false, null);
        }

        
        /// <param name="commandToClone"></param>
        internal PSCommand(PSCommand commandToClone)
        {
            _commands = new CommandCollection();
            foreach (Command command in commandToClone.Commands)
            {
                Command clone = command.Clone();
                // Attach the cloned Command to this instance.
                _commands.Add(clone);
                _currentCommand = clone;
            }
        }

        
        /// <param name="command">Command object to use.</param>
        internal PSCommand(Command command)
        {
            _currentCommand = command;
            _commands = new CommandCollection();
            _commands.Add(_currentCommand);
        }

        #endregion

        #region Command / Parameter Construction

        
        /// <param name="command">
        /// A string representing the command.
        /// </param>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <returns>
        /// A PSCommand instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// cmdlet is null.
        /// </exception>
        public PSCommand AddCommand(string command)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand = new Command(command, false);
            _commands.Add(_currentCommand);

            return this;
        }

        
        /// <param name="cmdlet">
        /// A string representing cmdlet.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <returns>
        /// A PSCommand instance with <paramref name="cmdlet"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// cmdlet is null.
        /// </exception>
        public PSCommand AddCommand(string cmdlet, bool useLocalScope)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand = new Command(cmdlet, false, useLocalScope);
            _commands.Add(_currentCommand);

            return this;
        }

        
        /// <param name="script">
        /// A string representing the script.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="script"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        public PSCommand AddScript(string script)
        {
            if (script == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(script));
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand = new Command(script, true);
            _commands.Add(_currentCommand);

            return this;
        }

        
        /// <param name="script">
        /// A string representing the script.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="script"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        public PSCommand AddScript(string script, bool useLocalScope)
        {
            if (script == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(script));
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand = new Command(script, true, useLocalScope);
            _commands.Add(_currentCommand);

            return this;
        }

        
        /// <param name="command">
        /// Command to add.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        public PSCommand AddCommand(Command command)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand = command;
            _commands.Add(_currentCommand);

            return this;
        }

        
        /// <param name="parameterName">
        /// Name of the parameter.
        /// </param>
        /// <param name="value">
        /// Value for the parameter.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="parameterName"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        public PSCommand AddParameter(string parameterName, object value)
        {
            if (_currentCommand == null)
            {
                throw PSTraceSource.NewInvalidOperationException(PSCommandStrings.ParameterRequiresCommand,
                                                                 new object[] { "PSCommand" });
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand.Parameters.Add(parameterName, value);
            return this;
        }

        
        /// <param name="parameterName">
        /// Name of the parameter.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="parameterName"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        public PSCommand AddParameter(string parameterName)
        {
            if (_currentCommand == null)
            {
                throw PSTraceSource.NewInvalidOperationException(PSCommandStrings.ParameterRequiresCommand,
                                                                 new object[] { "PSCommand" });
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand.Parameters.Add(parameterName, true);
            return this;
        }

        
        internal PSCommand AddParameter(CommandParameter parameter)
        {
            if (_currentCommand == null)
            {
                throw PSTraceSource.NewInvalidOperationException(PSCommandStrings.ParameterRequiresCommand,
                                                                 new object[] { "PSCommand" });
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand.Parameters.Add(parameter);
            return this;
        }

        
        /// <param name="value">
        /// Value for the parameter.
        /// </param>
        /// <returns>
        /// A PSCommand instance parameter value <paramref name="value"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        public PSCommand AddArgument(object value)
        {
            if (_currentCommand == null)
            {
                throw PSTraceSource.NewInvalidOperationException(PSCommandStrings.ParameterRequiresCommand,
                                                                 new object[] { "PSCommand" });
            }

            _owner?.AssertChangesAreAccepted();

            _currentCommand.Parameters.Add(null, value);
            return this;
        }

        
        /// <returns>
        /// A PowerShell instance with the items in <paramref name="parameters"/> added
        /// to the parameter list of the last command.
        /// </returns>
        public PSCommand AddStatement()
        {
            if (_commands.Count == 0)
            {
                return this;
            }

            _commands[_commands.Count - 1].IsEndOfStatement = true;
            return this;
        }

        #endregion

        #region Properties and Methods

        
        public CommandCollection Commands
        {
            get
            {
                return _commands;
            }
        }

        
        internal PowerShell Owner
        {
            get
            {
                return _owner;
            }

            set
            {
                _owner = value;
            }
        }

        
        public void Clear()
        {
            _commands.Clear();
            _currentCommand = null;
        }

        
        /// <returns>
        /// A shallow copy of the current PSCommand
        /// </returns>
        public PSCommand Clone()
        {
            return new PSCommand(this);
        }

        #endregion

        #region Private Methods

        
        /// <param name="command">
        /// Command to initialize the instance with.
        /// </param>
        /// <param name="isScript">
        /// true if the <paramref name="command"/> is script,
        /// false otherwise.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <remarks>
        /// Caller should check the input.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        private void Initialize(string command, bool isScript, bool? useLocalScope)
        {
            _commands = new CommandCollection();

            if (command != null)
            {
                _currentCommand = new Command(command, isScript, useLocalScope);
                _commands.Add(_currentCommand);
            }
        }

        #endregion
    }
}
