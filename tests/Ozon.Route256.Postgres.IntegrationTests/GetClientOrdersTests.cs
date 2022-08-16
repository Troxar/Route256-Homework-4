using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Google.Type;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ozon.Route256.Postgres.Grpc;
using Ozon.Route256.Postgres.IntegrationTests.Common;
using Ozon.Route256.Postgres.Persistence;
using Xunit;
using Order = Ozon.Route256.Postgres.Domain.Order;
using OrderState = Ozon.Route256.Postgres.Domain.OrderState;

namespace Ozon.Route256.Postgres.IntegrationTests;

public sealed class GetClientOrdersTests : IClassFixture<StartupFixture>
{
    private readonly StartupFixture _fixture;
    private const long ClientId = 123456L;
    private readonly object _lockObject = new ();
    private static bool s_prepared;

    public GetClientOrdersTests(StartupFixture fixture)
    {
        _fixture = fixture;
        PrepareRepositoryIfItIsNecessary();
    }

    [Theory]
    [ClassData(typeof(DataForTheoryGetClientOrdersShouldReturnOrders))]
    public async Task GetClientOrders_ShouldReturnOrders(int pageSize, int offsetFromLastOrderId, ExpectedRow[] expected)
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var grpc = GetClient();
        var startFromOrderId = offsetFromLastOrderId == 0 ? 0 : GetLastOrderId(offsetFromLastOrderId);

        // Act
        var result = await grpc.GetClientOrdersAsync(
            new GetClientOrdersRequest
            {
                ClientId = ClientId,
                PageSize = pageSize,
                StartFromOrderId = startFromOrderId
            },
            cancellationToken: cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.OrderRows.Should().NotBeNull()
            .And.HaveCount(expected.Length);

        for (int i = 0; i < expected.Length; i++)
            result.OrderRows[i].Should().BeEquivalentTo(expected[i]);
    }

    [Theory]
    [ClassData(typeof(DataForTheoryGetClientOrdersShouldReturnOrders))]
    public async Task GetClientOrdersStream_ShouldReturnOrders(int pageSize, int offsetFromLastOrderId, ExpectedRow[] expected)
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var grpc = GetClient();
        var startFromOrderId = offsetFromLastOrderId == 0 ? 0 : GetLastOrderId(offsetFromLastOrderId);

        // Act
        using var call = grpc.GetClientOrdersStream(
            new GetClientOrdersRequest
            {
                ClientId = ClientId,
                PageSize = pageSize,
                StartFromOrderId = startFromOrderId
            },
            cancellationToken: cts.Token);

        var result = await call.ResponseStream
            .ReadAllAsync(cts.Token)
            .ToArrayAsync(cts.Token);

        // Assert
        result.Should().NotBeNull()
            .And.HaveCount(expected.Length);

        for (int i = 0; i < expected.Length; i++)
            result[i].OrderRow.Should().BeEquivalentTo(expected[i]);
    }

    private class DataForTheoryGetClientOrdersShouldReturnOrders : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                0,
                0,
                Array.Empty<ExpectedRow>()
            };
            yield return new object[]
            {
                6,
                0,
                new ExpectedRow[]
                {
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40001, 41, new Money { Units = 401L }),
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40002, 42, new Money { Units = 402L }),
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40003, 43, new Money { Units = 403L }),
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40004, 44, new Money { Units = 404L }),
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30001, 31, new Money { Units = 301L }),
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30002, 32, new Money { Units = 302L }),
                }
            };
            yield return new object[]
            {
                20,
                0,
                new ExpectedRow[]
                {
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40001, 41, new Money { Units = 401L }),
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40002, 42, new Money { Units = 402L }),
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40003, 43, new Money { Units = 403L }),
                    new (ClientId, OrderState.InDelivery, new Money { Units = 4000L },
                        (new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero)).ToTimestamp(),
                        40004, 44, new Money { Units = 404L }),
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30001, 31, new Money { Units = 301L }),
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30002, 32, new Money { Units = 302L }),
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30003, 33, new Money { Units = 303L }),
                    new (ClientId, OrderState.Completed, new Money { Units = 2000L },
                        (new DateTimeOffset(new DateTime(2002, 2, 2, 2, 2, 2),TimeSpan.Zero)).ToTimestamp(),
                        20001, 21, new Money { Units = 201L }),
                    new (ClientId, OrderState.Completed, new Money { Units = 2000L },
                        (new DateTimeOffset(new DateTime(2002, 2, 2, 2, 2, 2),TimeSpan.Zero)).ToTimestamp(),
                        20002, 22, new Money { Units = 202L }),
                    new (ClientId, OrderState.Boxing, new Money { Units = 1000L },
                        (new DateTimeOffset(new DateTime(2001, 1, 1, 1, 1, 1),TimeSpan.Zero)).ToTimestamp(),
                        10001, 11, new Money { Units = 101L }),
                }
            };
            yield return new object[]
            {
                4,
                1,
                new ExpectedRow[]
                {
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30001, 31, new Money { Units = 301L }),
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30002, 32, new Money { Units = 302L }),
                    new (ClientId, OrderState.Paid, new Money { Units = 3000L },
                        (new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero)).ToTimestamp(),
                        30003, 33, new Money { Units = 303L }),
                    new (ClientId, OrderState.Completed, new Money { Units = 2000L },
                        (new DateTimeOffset(new DateTime(2002, 2, 2, 2, 2, 2),TimeSpan.Zero)).ToTimestamp(),
                        20001, 21, new Money { Units = 201L }),
                }
            };
            yield return new object[]
            {
                4,
                2,
                new ExpectedRow[]
                {
                    new (ClientId, OrderState.Completed, new Money { Units = 2000L },
                        (new DateTimeOffset(new DateTime(2002, 2, 2, 2, 2, 2),TimeSpan.Zero)).ToTimestamp(),
                        20001, 21, new Money { Units = 201L }),
                    new (ClientId, OrderState.Completed, new Money { Units = 2000L },
                        (new DateTimeOffset(new DateTime(2002, 2, 2, 2, 2, 2),TimeSpan.Zero)).ToTimestamp(),
                        20002, 22, new Money { Units = 202L }),
                    new (ClientId, OrderState.Boxing, new Money { Units = 1000L },
                        (new DateTimeOffset(new DateTime(2001, 1, 1, 1, 1, 1),TimeSpan.Zero)).ToTimestamp(),
                        10001, 11, new Money { Units = 101L }),
                }
            };
            yield return new object[]
            {
                4,
                3,
                new ExpectedRow[]
                {
                    new (ClientId, OrderState.Boxing, new Money { Units = 1000L },
                        (new DateTimeOffset(new DateTime(2001, 1, 1, 1, 1, 1),TimeSpan.Zero)).ToTimestamp(),
                        10001, 11, new Money { Units = 101L }),
                }
            };
            yield return new object[]
            {
                4,
                4,
                Array.Empty<ExpectedRow>()
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private OrderService.OrderServiceClient GetClient()
    {
        var client = _fixture.CreateClient();
        var channel = GrpcChannel.ForAddress(client.BaseAddress!, new() { HttpClient = client });
        return new(channel);
    }

    private  void PrepareRepositoryIfItIsNecessary()
    {
        if (s_prepared)
            return;

        lock (_lockObject)
        {
            if (!s_prepared)
            {
                PrepareRepository();
                s_prepared = true;
            }
        }
    }

    private async void PrepareRepository()
    {
        var connectionString = _fixture.Services.GetRequiredService<IConfiguration>()["ConnectionString"];
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var commandClient = new NpgsqlCommand("DELETE FROM order_items WHERE order_id IN " +
                                                    "(SELECT order_id FROM orders WHERE client_id = :clientId);" +
                                                    "DELETE FROM orders WHERE client_id = :clientId", connection)
        {
            Parameters =
            {
                { "clientId", ClientId },
            }
        };
        commandClient.ExecuteScalar();

        var orders = new Order[4];
        orders[0] = new Order(1, ClientId, OrderState.Boxing, 1000,
            new DateTimeOffset(new DateTime(2001, 1, 1, 1, 1, 1),TimeSpan.Zero), new Order.Item[1])
        {
            Items =
            {
                [0] = new Order.Item(10001, 11, 101)
            }
        };
        orders[1] = new Order(2, ClientId, OrderState.Completed, 2000,
            new DateTimeOffset(new DateTime(2002, 2, 2, 2, 2, 2),TimeSpan.Zero), new Order.Item[2])
        {
            Items =
            {
                [0] = new Order.Item(20001, 21, 201),
                [1] = new Order.Item(20002, 22, 202),
            }
        };
        orders[2] = new Order(3, ClientId, OrderState.Paid, 3000,
            new DateTimeOffset(new DateTime(2003, 3, 3, 3, 3, 3),TimeSpan.Zero), new Order.Item[3])
        {
            Items =
            {
                [0] = new Order.Item(30001, 31, 301),
                [1] = new Order.Item(30002, 32, 302),
                [2] = new Order.Item(30003, 33, 303),
            }
        };

        orders[3] = new Order(4, ClientId, OrderState.InDelivery, 4000,
            new DateTimeOffset(new DateTime(2004, 4, 4, 4, 4, 4),TimeSpan.Zero), new Order.Item[4])
        {
            Items =
            {
                [0] = new Order.Item(40001, 41, 401),
                [1] = new Order.Item(40002, 42, 402),
                [2] = new Order.Item(40003, 43, 403),
                [3] = new Order.Item(40004, 44, 404),
            }
        };

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var repository = new OrderRepository(connectionString);

        await repository.Add(orders, cts.Token);
    }

    private long GetLastOrderId(int offset)
    {
        var connectionString = _fixture.Services.GetRequiredService<IConfiguration>()["ConnectionString"];
        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand("SELECT order_id FROM orders " +
                                              $"WHERE client_id = {ClientId} ORDER BY order_id DESC LIMIT 1 OFFSET {offset}", connection);
        connection.Open();
        using var reader = command.ExecuteReader(CommandBehavior.SingleResult);

        if (!reader.Read())
            return -1;

        return reader.GetFieldValue<long>(0);
    }

    public sealed record ExpectedRow(long ClientId, OrderState State, Money Amount, Timestamp Date,
        long SkuId, int Quantity, Money Price);
}
