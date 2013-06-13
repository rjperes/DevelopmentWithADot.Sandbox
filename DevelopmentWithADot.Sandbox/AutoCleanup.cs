using System;

namespace DevelopmentWithADot.Sandbox
{
	public class AutoCleanup : IDisposable
	{
		protected internal readonly AppDomain appDomain;
		protected internal readonly Object target;

		public AutoCleanup(Object target, AppDomain appDomain)
		{
			this.appDomain = appDomain;
			this.target = target;
		}

		public Object Target
		{
			get
			{
				return (this.target);
			}
		}

		void IDisposable.Dispose()
		{
			AppDomain.Unload(this.appDomain);
		}
	}

	public class AutoCleanup<T> : AutoCleanup
	{
		public AutoCleanup(T target, AppDomain appDomain) : base(target, appDomain)
		{
		}

		public new T Target
		{
			get
			{
				return ((T) this.target);
			}
		}

		public static implicit operator T(AutoCleanup<T> auto)
		{
			return ((T) auto.target);
		}		
	}
}
