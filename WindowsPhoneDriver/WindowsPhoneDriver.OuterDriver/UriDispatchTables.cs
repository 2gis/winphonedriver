namespace WindowsPhoneDriver.OuterDriver
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using OpenQA.Selenium.Remote;

    using DriverCommand = WindowsPhoneDriver.Common.DriverCommand;

    internal class UriDispatchTables
    {
        #region Fields

        private static Dictionary<string, CommandInfo> extendedCommands;

        private UriTemplateTable deleteDispatcherTable;

        private UriTemplateTable getDispatcherTable;

        private UriTemplateTable postDispatcherTable;

        #endregion

        #region Constructors and Destructors

        public UriDispatchTables(Uri prefix)
        {
            extendedCommands = new Dictionary<string, CommandInfo>
                                   {
                                       {
                                           DriverCommand.PullFile,
                                           new CommandInfo(
                                           "POST",
                                           "/session/{sessionId}/appium/device/pull_file")
                                       },
                                       {
                                           DriverCommand.PushFile,
                                           new CommandInfo(
                                           "POST",
                                           "/session/{sessionId}/appium/device/push_file")
                                       },
                                       {
                                           DriverCommand.GetElementRect,
                                           new CommandInfo(
                                           "GET",
                                           "/session/{sessionId}/element/{id}/rect")
                                       }
                                   };
            this.ConstructDispatcherTables(prefix);
        }

        #endregion

        #region Public Methods and Operators

        public UriTemplateMatch Match(string httpMethod, Uri uriToMatch)
        {
            return this.FindDispatcherTable(httpMethod).MatchSingle(uriToMatch);
        }

        #endregion
        
        #region Methods

        internal UriTemplateTable FindDispatcherTable(string httpMethod)
        {
            UriTemplateTable tableToReturn = null;
            switch (httpMethod)
            {
                case CommandInfo.GetCommand:
                    tableToReturn = this.getDispatcherTable;
                    break;

                case CommandInfo.PostCommand:
                    tableToReturn = this.postDispatcherTable;
                    break;

                case CommandInfo.DeleteCommand:
                    tableToReturn = this.deleteDispatcherTable;
                    break;
            }

            return tableToReturn;
        }

        private CommandInfo GetCommandInfo(string name)
        {
            var commandInformation = CommandInfoRepository.Instance.GetCommandInfo(name);
            if (commandInformation != null)
            {
                return commandInformation;
            }

            if (extendedCommands.ContainsKey(name))
            {
                commandInformation = extendedCommands[name];
            }

            return commandInformation;
        }

        private IReadOnlyDictionary<string, CommandInfo> GetCommands()
        {
            var commands = new Dictionary<string, CommandInfo>();
            var fields = typeof(DriverCommand).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                var commandName = field.GetValue(null).ToString();
                var commandInformation = this.GetCommandInfo(commandName);
                commands.Add(commandName, commandInformation);
            }

            return commands;
        }

        private void ConstructDispatcherTables(Uri prefix)
        {
            this.getDispatcherTable = new UriTemplateTable(prefix);
            this.postDispatcherTable = new UriTemplateTable(prefix);
            this.deleteDispatcherTable = new UriTemplateTable(prefix);

            var commands = this.GetCommands();

            foreach (var command in commands)
            {
                var commandUriTemplate = new UriTemplate(command.Value.ResourcePath);
                var templateTable = this.FindDispatcherTable(command.Value.Method);
                templateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(commandUriTemplate, command.Key));
            }

            this.getDispatcherTable.MakeReadOnly(false);
            this.postDispatcherTable.MakeReadOnly(false);
            this.deleteDispatcherTable.MakeReadOnly(false);
        }

        #endregion
    }
}