using System.Threading.Tasks;

namespace Jpp.Etl.Service
{
    internal interface IScheduledTask
    {
        Task Start();
    }
}
