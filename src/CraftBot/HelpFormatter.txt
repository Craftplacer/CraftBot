using System.Collections.Generic;
using System.Linq;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace CraftBot
{
    public class HelpFormatter : BaseHelpFormatter
    {
        private Command _command;
        private IEnumerable<Command> _subCommands;

        public HelpFormatter(CommandContext ctx) : base(ctx)
        {
        }

        public override CommandHelpMessage Build()
        {
            var builder = new DiscordEmbedBuilder();

            builder.WithTitle("Help");

            var description = string.Empty;

            if (_command != null)
                description += $"Command: {_command}\n";

            if (_subCommands != null)
            {
                
                var groups = _subCommands.OfType<CommandGroup>();
                if (groups.Any())
                {
                    var stringBuilder = new System.Text.StringBuilder();

                    foreach (var group in groups)
                    {
                        if (string.IsNullOrWhiteSpace(group.Description))
                        {
                            stringBuilder.AppendLine($"**{group.Name}**  `{group.Children.Count}`");
                        }
                        else
                        {
                            stringBuilder.AppendLine($"**{group.Name}**  `{group.Children.Count}`\n{group.Description}");
                        }
                    }
                        

                    builder.AddField("Groups", stringBuilder.ToString());
                    builder.Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                         Text="See help about a group by typing 'cb!help group'"
                    };
                }

                var commands = _subCommands.Except(groups);
                if (commands.Any())
                {
                    var stringBuilder = new System.Text.StringBuilder();

                    foreach (var command in commands)
                    {
                        var line = $"**{command.Name}**";

                        if (command.CustomAttributes.OfType<DSharpPlus.CommandsNext.Attributes.GroupCommandAttribute>().Any())
                            line += $" `default`";

                        if (!string.IsNullOrWhiteSpace(command.Description))
                            line += $"\n{command.Description}";

                        stringBuilder.AppendLine(line);
                    }

                    builder.AddField("Commands", stringBuilder.ToString());
                }

                //description += string.Join(", ", subCommands.Select(Connections => Connections == null ? "null" : Connections.ToString()));


            }

            builder.WithDescription(description);

            return new CommandHelpMessage(embed: builder);
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            this._command = command;
            this._subCommands = null;
            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            this._command = null;
            this._subCommands = subcommands;
            return this;
        }
    }
}