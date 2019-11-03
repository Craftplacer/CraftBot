namespace CraftBot
{
    public class Statistics
    {
        public long CommandsErrored { get; set; }
        public long CommandsExecuted { get; set; }

        public long CommandsMistyped { get; set; }

        public long CommandsTotal => CommandsErrored + CommandsMistyped + CommandsExecuted;

        public decimal ErrorRate
        {
            get
            {
                if (CommandsTotal == 0)
                    return 0;

                return CommandsErrored / (decimal)CommandsTotal;
            }
        }

        public decimal MistypeRate
        {
            get
            {
                if (CommandsTotal == 0)
                    return 0;

                return CommandsMistyped / (decimal)CommandsTotal;
            }
        }
    }
}