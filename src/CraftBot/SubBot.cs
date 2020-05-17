using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace CraftBot
{
	public abstract class SubBot : IStartable
	{
		protected SubBot([NotNull] BotManager manager)
		{
			Manager = manager ?? throw new ArgumentNullException(nameof(manager));
		}

		[NotNull]
		public BotManager Manager { get; }
		
		public abstract Task InitializeAsync();
		public abstract Task ConnectAsync();
		
		public async Task StartAsync()
		{
			await InitializeAsync();
			await ConnectAsync();
		}

		public abstract Task StopAsync();
	}
}