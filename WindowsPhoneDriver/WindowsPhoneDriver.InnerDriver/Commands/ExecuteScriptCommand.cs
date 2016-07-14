namespace WindowsPhoneDriver.InnerDriver.Commands
{
	using System;
	using System.Linq;

	using Common;

	using Newtonsoft.Json.Linq;

	internal class ExecuteScriptCommand : CommandBase
	{
		public override string DoImpl()
		{
			object typeParam = null;
			if (!Parameters.TryGetValue("type", out typeParam))
			{
				return "specify fully qualified type name in 'type' parameter";
			}

			object methodParam = null;
			if (!Parameters.TryGetValue("method", out methodParam))
			{
				return "specify fully qualified type name in 'method' parameter";
			}

			object[] args = null;
			object argsParam = null;
			if (Parameters.TryGetValue("args", out argsParam))
			{
				args = ((JArray)argsParam).ToObject<object[]>();
			}
			else
			{
				args = new object[] {};
			}

			var typeName = typeParam.ToString();
			var type = FindType(typeName);
			var method = methodParam.ToString();
			type.GetMethod(method).Invoke(null, args);

			return this.JsonResponse(ResponseStatus.Success, true);
		}

		private static Type FindType(string fullName)
		{
			return
				AppDomain.CurrentDomain.GetAssemblies()
					.Where(a => !a.IsDynamic)
					.SelectMany(a => a.GetTypes())
					.FirstOrDefault(t => t.FullName.Equals(fullName));
		}
	}
}
