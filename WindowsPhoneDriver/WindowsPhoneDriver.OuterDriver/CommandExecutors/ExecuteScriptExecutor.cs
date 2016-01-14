namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Forms;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.Common.Exceptions;

    internal class ExecuteScriptExecutor : CommandExecutorBase
    {
        #region Methods

        public string RunExternalExe(string filename, string arguments = null)
        {
            var process = new Process();

            process.StartInfo.FileName = filename;
            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            var stdOutput = new System.Text.StringBuilder();
            process.OutputDataReceived += (sender, args) => stdOutput.Append(args.Data + "\n");

            string stdError = null;
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                stdError = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
            }

            if (process.ExitCode == 0)
            {
                return stdOutput.ToString();
            }
            else
            {
                var message = new System.Text.StringBuilder();

                if (!string.IsNullOrEmpty(stdError))
                {
                    message.AppendLine(stdError);
                }

                if (stdOutput.Length != 0)
                {
                    message.AppendLine("Std output:");
                    message.AppendLine(stdOutput.ToString());
                }

                throw new Exception(Format(filename, arguments) + " finished with exit code = " + process.ExitCode + ": " + message);
            }
        }

        private string Format(string filename, string arguments)
        {
            return "'" + filename +
                ((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) +
                "'";
        }

        protected override string DoImpl()
        {
            const string MobileScriptPrefix = "mobile:";
            var script = this.ExecutedCommand.Parameters["script"].ToString();
            if (script.StartsWith(MobileScriptPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var command = script.Split(':')[1].ToLower(CultureInfo.InvariantCulture).Trim();

                if (command.Equals("start"))
                {
                    this.Automator.EmulatorController.TypeKey(Keys.F2);
                }
                else if (command.Equals("search"))
                {
                    this.Automator.EmulatorController.TypeKey(Keys.F3);
                }
                else
                {
                    throw new AutomationException(
                        "Unknown 'mobile:' script command. See https://github.com/2gis/winphonedriver/wiki/Command-Execute-Script for supported commands.",
                        ResponseStatus.JavaScriptError);
                }
            }
            else if (script.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
            {
                var command = script.Split(new char[] { ':' }, 2)[1];
                var cmd = "";
                var args = "";
                if (command.StartsWith("\""))
                {
                    var parts = command.Split(new char[] { '"' }, 3);
                    cmd = parts[1];
                    args = parts[2];
                }
                else
                {
                    var parts = command.Split(new char[] { ' ' }, 2);
                    cmd = parts[0];
                    args = parts[1];
                }
                return this.JsonResponse(ResponseStatus.Success, RunExternalExe(cmd, args));
            }
            else
            {
                throw new NotImplementedException(
                    "execute partially implemented, supports only mobile: or cmd: prefixed commands");
            }
            return this.JsonResponse();
        }

        #endregion
    }
}
