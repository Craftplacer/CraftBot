using System.Threading.Tasks;
using CraftBot.Common;
using Craftplacer.IRC;
using Craftplacer.IRC.Events;
using JetBrains.Annotations;
using Qmmands;

namespace CraftBot.IRC
{
	public class IrcBot : SubBot
	{
		public const string Nickname = "CraftBot";
		private IrcClient _ircClient;
		private readonly CommandService _commandService;
		
		public IrcBot([NotNull] BotManager manager, CommandService commandService) : base(manager)
		{
			_commandService = commandService;
		}

		public override async Task InitializeAsync()
		{
			_ircClient = new IrcClient();
			_ircClient.MessageReceived += IrcClientOnMessageReceived;
		}

		private async Task IrcClientOnMessageReceived(MessageReceivedEventArgs e)
		{
			if (!CommandUtilities.HasPrefix(e.Message.Message, Manager.Config.Prefix, out var output))
				return;

			var commonMessage = new CommonIrcMessage(e.Message);
			var context = new CommonCommandContext(commonMessage);
			var result = await _commandService.ExecuteAsync(output, context);
			
			//if (result is FailedResult failedResult)
			//	await message.Channel.SendMessageAsync(failedResult.Reason);
		}

		public override async Task ConnectAsync()
		{
			Logger.Info("Connecting to IRC...", "CraftBot");
			await _ircClient.ConnectAsync("irc.hash1da.com", 6969, Nickname, Nickname);
		}

		public override async Task StopAsync()
		{
			await _ircClient.DisconnectAsync("IrcBot.StopAsync() told me to leave ;w;");
		}
	}
}