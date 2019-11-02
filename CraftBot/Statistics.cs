namespace CraftBot
{
    public class Statistics
    {
        public int CommandsErrored { get; set; }
        public int CommandsExecuted { get; set; }

        public int CommandsTotal => CommandsErrored + CommandsExecuted;

        public decimal ErrorRate => CommandsErrored / (decimal)CommandsTotal;
    }
}