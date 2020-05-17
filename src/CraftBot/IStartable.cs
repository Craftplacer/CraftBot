using System.Threading.Tasks;

namespace CraftBot
{
	public interface IStartable
	{
		public Task StartAsync();
		public Task StopAsync();
	}
}