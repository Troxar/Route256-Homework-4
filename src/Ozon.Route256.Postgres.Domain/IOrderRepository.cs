using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ozon.Route256.Postgres.Domain;

public interface IOrderRepository
{
    IAsyncEnumerable<Order> Get(long[] orderIds, CancellationToken cancellationToken);
    IAsyncEnumerable<OrderRow> GetClientOrders(long clientId, int pageSize, long startFromOrderId, CancellationToken cancellationToken);

    ValueTask Add(Order[] orders, CancellationToken cancellationToken);
}
