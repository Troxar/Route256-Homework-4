using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Ozon.Route256.Postgres.Grpc;

namespace Ozon.Route256.Postgres.Api.Mapping;

internal static class OrderMapping
{
    public static Order Map(this Domain.Order order) =>
        new Order
        {
            Id = order.Id,
            Amount = order.Amount.ToMoney(),
            State = order.State.ToGrpc(),
            Items = { order.Items.Select(Map) }
        };

    private static Order.Types.Item Map(this Domain.Order.Item item) =>
        new Order.Types.Item
        {
            SkuId = item.SkuId,
            Quantity = item.Quantity,
            Price = item.Price.ToMoney()
        };

    public static OrderRow Map(this Domain.OrderRow orderRow) =>
        new OrderRow
        {
            OrderId = orderRow.OrderId,
            ClientId = orderRow.ClientId,
            State = orderRow.State.ToGrpc(),
            Amount = orderRow.Amount.ToMoney(),
            Date = orderRow.Date.ToTimestamp(),
            SkuId = orderRow.SkuId,
            Quantity = orderRow.Quantity,
            Price = orderRow.Price.ToMoney()
        };

    private static OrderState ToGrpc(this Domain.OrderState state) =>
        state switch {
            Domain.OrderState.Unknown => OrderState.Unknown,
            Domain.OrderState.Created => OrderState.Created,
            Domain.OrderState.Paid => OrderState.Paid,
            Domain.OrderState.Boxing => OrderState.Boxing,
            Domain.OrderState.WaitForPickup => OrderState.WaitForPickup,
            Domain.OrderState.InDelivery => OrderState.InDelivery,
            Domain.OrderState.WaitForClient => OrderState.WaitForClient,
            Domain.OrderState.Completed => OrderState.Completed,
            Domain.OrderState.Cancelled => OrderState.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
}
