﻿namespace WindowsPhoneDriver.OuterDriver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.CommandExecutors;

    internal class CommandExecutorDispatchTable
    {
        #region Fields

        private Dictionary<string, Type> commandExecutorsRepository;

        #endregion

        #region Constructors and Destructors

        public CommandExecutorDispatchTable()
        {
            this.ConstructDispatcherTable();
        }

        #endregion

        #region Public Methods and Operators

        public CommandExecutorBase GetExecutor(string commandName)
        {
            Type executorType;
            if (this.commandExecutorsRepository.TryGetValue(commandName, out executorType))
            {
            }
            else
            {
                executorType = typeof(CommandExecutorForward);
            }

            return (CommandExecutorBase)Activator.CreateInstance(executorType);
        }

        #endregion

        #region Methods

        private void ConstructDispatcherTable()
        {
            this.commandExecutorsRepository = new Dictionary<string, Type>();

            const string ExecutorsNamespace = "WindowsPhoneDriver.OuterDriver.CommandExecutors";

            var q =
                (from t in Assembly.GetExecutingAssembly().GetTypes()
                 where t.IsClass && t.Namespace == ExecutorsNamespace
                 select t).ToArray();

            var fields = typeof(DriverCommand).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(null).ToString();
                var executor = q.FirstOrDefault(x => x.Name.Equals(field.Name + "Executor"));
                if (executor != null)
                {
                    this.commandExecutorsRepository.Add(fieldValue, executor);
                }
            }
        }

        #endregion
    }
}
