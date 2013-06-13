using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Policy;

namespace DevelopmentWithADot.Sandbox
{
	public static class SandboxExtensions
	{
		#region Type methods
		public static Object CreateRestrictedTypeWithTrustedAssemblies(this Type type, SecurityZone zone, params Assembly[] fullTrustAssemblies)
		{
			return (CreateRestrictedType(type, zone, fullTrustAssemblies, new IPermission[0]));
		}

		public static T CreateRestrictedTypeWithTrustedAssemblies<T>(SecurityZone zone, params Assembly[] fullTrustAssemblies) where T : MarshalByRefObject, new()
		{
			return ((T)CreateRestrictedTypeWithTrustedAssemblies(typeof(T), zone, fullTrustAssemblies));
		}

		public static Object CreateRestrictedTypeWithPermissions(this Type type, SecurityZone zone, params IPermission[] additionalPermissions)
		{
			return (CreateRestrictedType(type, zone, new Assembly[0], additionalPermissions));
		}

		public static T CreateRestrictedTypeWithPermissions<T>(SecurityZone zone, params IPermission[] additionalPermissions) where T : MarshalByRefObject, new()
		{
			return ((T) CreateRestrictedTypeWithPermissions(typeof(T), zone, additionalPermissions));
		}

		public static Object CreateRestrictedType(this Type type, SecurityZone zone, Assembly[] fullTrustAssemblies, IPermission[] additionalPermissions)
		{
			Evidence evidence = new Evidence();
			evidence.AddHostEvidence(new Zone(zone));

			PermissionSet evidencePermissionSet = SecurityManager.GetStandardSandbox(evidence);

			foreach (IPermission permission in additionalPermissions ?? new IPermission[0])
			{
				evidencePermissionSet.AddPermission(permission);
			}

			StrongName[] strongNames = (fullTrustAssemblies ?? new Assembly[0]).Select(a => a.Evidence.GetHostEvidence<StrongName>()).ToArray();

			AppDomainSetup adSetup = new AppDomainSetup();
			adSetup.ApplicationBase = Path.GetDirectoryName(type.Assembly.Location);

			AppDomain newDomain = AppDomain.CreateDomain("Sandbox", evidence, adSetup, evidencePermissionSet, strongNames);

			ObjectHandle handle = Activator.CreateInstanceFrom(newDomain, type.Assembly.ManifestModule.FullyQualifiedName, type.FullName);
			
			return (handle.Unwrap());
		}

		public static T CreateRestrictedType<T>(SecurityZone zone, Assembly[] fullTrustAssemblies, IPermission[] additionalPermissions) where T : MarshalByRefObject, new()
		{
			return ((T) CreateRestrictedType(typeof(T), zone, fullTrustAssemblies, additionalPermissions));
		}
		#endregion

		#region Execute methods
		public static TResult Execute<T, TResult>(Expression<Func<T, TResult>> expression, SecurityZone zone, Assembly[] fullTrustAssemblies, IPermission[] additionalPermissions) where T : MarshalByRefObject, new()
		{
			using (AutoCleanup<T> auto = CreateRestrictedDomain<T>(zone, fullTrustAssemblies, additionalPermissions))
			{
				Func<T, TResult> func = expression.Compile();
				return (func(auto.Target));
			}
		}

		public static void Execute<T>(Expression<Action<T>> expression, SecurityZone zone, Assembly[] fullTrustAssemblies, IPermission[] additionalPermissions) where T : MarshalByRefObject, new()
		{
			using (AutoCleanup<T> auto = CreateRestrictedDomain<T>(zone, fullTrustAssemblies, additionalPermissions))
			{
				Action<T> func = expression.Compile();
				func(auto.Target);
			}
		}
		#endregion

		#region Domain methods
		public static AutoCleanup<T> CreateRestrictedDomain<T>(SecurityZone zone, Assembly[] fullTrustAssemblies, IPermission[] additionalPermissions) where T : MarshalByRefObject, new()
		{
			Evidence evidence = new Evidence();
			evidence.AddHostEvidence(new Zone(zone));

			PermissionSet evidencePermissionSet = SecurityManager.GetStandardSandbox(evidence);

			foreach (IPermission permission in additionalPermissions ?? new IPermission[0])
			{
				evidencePermissionSet.AddPermission(permission);
			}

			StrongName[] strongNames = (fullTrustAssemblies ?? new Assembly[0]).Select(a => a.Evidence.GetHostEvidence<StrongName>()).ToArray();

			AppDomainSetup adSetup = new AppDomainSetup();
			adSetup.ApplicationBase = Path.GetDirectoryName(typeof(T).Assembly.Location);

			AppDomain newDomain = AppDomain.CreateDomain("Sandbox", evidence, adSetup, evidencePermissionSet, strongNames);

			ObjectHandle handle = Activator.CreateInstanceFrom(newDomain, typeof(T).Assembly.ManifestModule.FullyQualifiedName, typeof(T).FullName);

			T obj = handle.Unwrap() as T;

			return (new AutoCleanup<T>(obj, newDomain));
		}

		public static AutoCleanup CreateRestrictedDomainWithTrustedAssemblies(this Type type, SecurityZone zone, params Assembly[] fullTrustAssemblies)
		{
			return(CreateRestrictedDomain(type, zone, fullTrustAssemblies, new IPermission[0]));
		}

		public static AutoCleanup<T> CreateRestrictedDomainWithTrustedAssemblies<T>(SecurityZone zone, params Assembly[] fullTrustAssemblies) where T : MarshalByRefObject, new()
		{
			AutoCleanup auto = CreateRestrictedDomainWithTrustedAssemblies(typeof(T), zone, fullTrustAssemblies);

			return (new AutoCleanup<T>((T) auto.target, auto.appDomain));
		}

		public static AutoCleanup CreateRestrictedDomainWithPermissions(this Type type, SecurityZone zone, params IPermission[] additionalPermissions)
		{
			return(CreateRestrictedDomain(type, zone, new Assembly[0], additionalPermissions));
		}

		public static AutoCleanup<T> CreateRestrictedDomainWithPermissions<T>(SecurityZone zone, params IPermission[] additionalPermissions) where T : MarshalByRefObject, new()
		{
			AutoCleanup auto = CreateRestrictedDomainWithPermissions(typeof(T), zone, additionalPermissions);

			return (new AutoCleanup<T>((T)auto.target, auto.appDomain));
		}

		public static AutoCleanup CreateRestrictedDomain(this Type type, SecurityZone zone, Assembly[] fullTrustAssemblies, IPermission[] additionalPermissions)
		{
			Evidence evidence = new Evidence();
			evidence.AddHostEvidence(new Zone(zone));

			PermissionSet evidencePermissionSet = SecurityManager.GetStandardSandbox(evidence);

			foreach (IPermission permission in additionalPermissions ?? new IPermission[0])
			{
				evidencePermissionSet.AddPermission(permission);
			}

			StrongName[] strongNames = (fullTrustAssemblies ?? new Assembly[0]).Select(a => a.Evidence.GetHostEvidence<StrongName>()).ToArray();

			AppDomainSetup adSetup = new AppDomainSetup();
			adSetup.ApplicationBase = Path.GetDirectoryName(type.Assembly.Location);

			AppDomain newDomain = AppDomain.CreateDomain("Sandbox", evidence, adSetup, evidencePermissionSet, strongNames);

			ObjectHandle handle = Activator.CreateInstanceFrom(newDomain, type.Assembly.ManifestModule.FullyQualifiedName, type.FullName);

			Object obj = handle.Unwrap();

			return (new AutoCleanup(obj, newDomain));		
		}
		#endregion
	}
}
