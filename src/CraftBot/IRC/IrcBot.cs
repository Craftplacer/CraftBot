using System.Linq;
using System.Threading.Tasks;
using Craftplacer.IRC;
using Craftplacer.IRC.Events;
using JetBrains.Annotations;
using Qmmands;

namespace CraftBot.IRC
{
	public class IrcBot : SubBot
	{
		private IrcClient _ircClient;
		private readonly CommandService _commandService;
		private IrcHostInformation _currentHost;

		public IrcBot([NotNull] BotManager manager) : base(manager)
		{
			_commandService = new CommandService();

			foreach (var module in manager.CommonCommandModules)
				_commandService.AddModule(module);
			
			_commandService.AddModule<IrcCommandModule>();
		}

		public override async Task InitializeAsync()
		{
			_currentHost = Manager.Config.IrcHosts?.SingleOrDefault();
			
			_ircClient = new IrcClient();
			_ircClient.MessageReceived += IrcClientOnMessageReceived;
			_ircClient.Welcome += IrcClientOnWelcome;
		}

		private async Task IrcClientOnWelcome(IrcEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(_currentHost.NickServPassword))
				return;

			await e.Client.SendMessageAsync("NickServ", $"IDENTIFY {_currentHost.NickServPassword}");
		}

		private async Task IrcClientOnMessageReceived(MessageReceivedEventArgs e)
		{
			if (!CommandUtilities.HasPrefix(e.Message.Message, Manager.Config.Prefix, out var output))
				return;

			var context = new IrcCommandContext(e.Message);
			var result = await _commandService.ExecuteAsync(output, context);
			
			if (result is FailedResult failedResult)
				await e.Message.RespondAsync($"{e.Message.Author.Nickname}: oh no your command failed ;w; {failedResult.Reason}");
		}

		public override async Task ConnectAsync()
		{
			if (_currentHost == null)
				return;
			
			Logger.Info("Connecting to IRC...", "CraftBot");
			await _ircClient.ConnectAsync(_currentHost.Host, _currentHost.Port, _currentHost.Nickname, _currentHost.Nickname);
		}

		public override async Task StopAsync()
		{
			await _ircClient.DisconnectAsync("IrcBot.StopAsync() told me to leave ;w;");
		}
	}
}