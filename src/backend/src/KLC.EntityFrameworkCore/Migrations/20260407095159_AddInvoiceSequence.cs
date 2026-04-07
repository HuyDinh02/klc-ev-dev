using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the COUNT(*)+1 invoice sequence with a PostgreSQL native sequence.
            // nextval() is atomic and safe under concurrent load; COUNT(*)+1 was not.
            // Start from the current invoice count + 1 to avoid gaps with existing records.
            migrationBuilder.Sql(@"
                CREATE SEQUENCE IF NOT EXISTS klc_invoice_seq
                    START WITH 1
                    INCREMENT BY 1
                    NO MINVALUE
                    NO MAXVALUE
                    CACHE 1;

                -- Advance the sequence past any existing invoices to avoid number conflicts.
                SELECT setval('klc_invoice_seq', GREATEST(1, (SELECT COUNT(*) FROM ""AppInvoices"")));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS klc_invoice_seq;");
        }
    }
}
