using EasyNetQ;

namespace Doppler.EasyNetQ.HosepipeWorker
{
    public interface IBusStation
    {
        IBus GetBus(string busName);
    }
}
