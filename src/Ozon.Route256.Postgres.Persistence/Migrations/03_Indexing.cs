using System;
using FluentMigrator;
using Ozon.Route256.Postgres.Persistence.Common;

namespace Ozon.Route256.Postgres.Persistence.Migrations;

[Migration(3, "Add index on orders")]
public sealed class CompoundIndexForOrders : SqlMigration
{
    protected override string GetUpSql(IServiceProvider services) => @"
DROP INDEX orders_client_id_idx;
CREATE INDEX orders_client_id_order_id_desc_idx ON orders (client_id, order_id DESC);
";

    protected override string GetDownSql(IServiceProvider services) => @"
DROP INDEX orders_client_id_order_id_desc_idx;
CREATE INDEX orders_client_id_idx ON orders (client_id);
";
}
