using System;

namespace Ozon.Route256.Postgres.Domain;

public sealed record OrderRow(long OrderId, long ClientId, OrderState State, decimal Amount, DateTimeOffset Date,
    long SkuId, int Quantity, decimal Price);
