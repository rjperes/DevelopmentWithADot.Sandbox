using System;
using System.IO;
using System.Reflection;
using System.Security;

namespace DevelopmentWithADot.Sandbox.Tests
{
	class Sandboxed : MarshalByRefObject
	{
		public String Test()
		{
			return(File.ReadAllText("c:\\windows\\win.ini"));
		}
	}

	class Program
	{
		static void Main(String[] args)
		{
			//Sandboxed s1 = SandboxExtensions.CreateRestrictedTypeWithPermissions<Sandboxed>(SecurityZone.Internet);
			//s1.Test();

			using (var x = SandboxExtensions.CreateRestrictedDomainWithTrustedAssemblies<Sandboxed>(SecurityZone.Internet))
			{
				Sandboxed s2 = x.Target;
				s2.Test();
			}

			var result = SandboxExtensions.Execute<Sandboxed, String>(y => y.Test(), SecurityZone.Internet, new Assembly[0], new IPermission[0]);
			SandboxExtensions.Execute<Sandboxed>(y => y.Test(), SecurityZone.Internet, new Assembly[0], new IPermission[0]);
		}
	}
}
