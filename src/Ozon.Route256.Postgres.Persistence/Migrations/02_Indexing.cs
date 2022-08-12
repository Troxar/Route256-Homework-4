using System;
using FluentMigrator;
using Ozon.Route256.Postgres.Persistence.Common;

namespace Ozon.Route256.Postgres.Persistence.Migrations;

[Migration(2, "Add index on order_items")]
public sealed class IndexForOrderIdOnOrderItems : SqlMigration
{
    protected override string GetUpSql(IServiceProvider services) => @"
CREATE INDEX order_items_order_id_idx ON order_items (order_id);
";

    protected override string GetDownSql(IServiceProvider services) => @"
DROP INDEX order_items_order_id_idx;
";
}
