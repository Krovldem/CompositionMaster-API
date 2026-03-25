using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CompositionMaster.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nomenclature",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    introducedintouse = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dsecode = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    subsystemcode = table.Column<string>(type: "text", nullable: false),
                    nomenclaturetype = table.Column<int>(type: "integer", nullable: false),
                    unitofmeasurement = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nomenclature", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "nomenclature_change",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    introducedintouse = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dsecode = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    subsystemcode = table.Column<string>(type: "text", nullable: false),
                    changedate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false),
                    author = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nomenclature_change", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "nomenclature_type",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nomenclature_type", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "operation_card",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false),
                    linenumber = table.Column<int>(type: "integer", nullable: false),
                    department = table.Column<string>(type: "text", nullable: false),
                    section = table.Column<string>(type: "text", nullable: false),
                    operation = table.Column<string>(type: "text", nullable: false),
                    equipment = table.Column<string>(type: "text", nullable: false),
                    timenorm = table.Column<decimal>(type: "numeric", nullable: false),
                    tariff = table.Column<decimal>(type: "numeric", nullable: false),
                    cost = table.Column<decimal>(type: "numeric", nullable: false),
                    sum = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_card", x => new { x.identifier, x.linenumber });
                });

            migrationBuilder.CreateTable(
                name: "operation_card_change",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false),
                    linenumber = table.Column<int>(type: "integer", nullable: false),
                    department = table.Column<string>(type: "text", nullable: false),
                    section = table.Column<string>(type: "text", nullable: false),
                    operation = table.Column<string>(type: "text", nullable: false),
                    equipment = table.Column<string>(type: "text", nullable: false),
                    timenorm = table.Column<decimal>(type: "numeric", nullable: false),
                    tariff = table.Column<decimal>(type: "numeric", nullable: false),
                    cost = table.Column<decimal>(type: "numeric", nullable: false),
                    sum = table.Column<decimal>(type: "numeric", nullable: false),
                    author = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_card_change", x => new { x.identifier, x.linenumber });
                });

            migrationBuilder.CreateTable(
                name: "position",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_position", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "specification",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inputdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    outputdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ismain = table.Column<bool>(type: "boolean", nullable: false),
                    owner = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specification", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "specification_change",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    owner = table.Column<int>(type: "integer", nullable: false),
                    inputdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    outputdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ismain = table.Column<bool>(type: "boolean", nullable: false),
                    changedate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false),
                    author = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specification_change", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "specification_component",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false),
                    linenumber = table.Column<int>(type: "integer", nullable: false),
                    nomenclature = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    participatesincalculation = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specification_component", x => new { x.identifier, x.linenumber });
                });

            migrationBuilder.CreateTable(
                name: "specification_component_change",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false),
                    linenumber = table.Column<int>(type: "integer", nullable: false),
                    nomenclature = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    participatesincalculation = table.Column<bool>(type: "boolean", nullable: false),
                    changedate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false),
                    author = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specification_component_change", x => new { x.identifier, x.linenumber });
                });

            migrationBuilder.CreateTable(
                name: "unit_of_measurement",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    abbreviation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_of_measurement", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    identifier = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    fullname = table.Column<string>(type: "text", nullable: false),
                    login = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    roleid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.identifier);
                    table.ForeignKey(
                        name: "FK_user_role_roleid",
                        column: x => x.roleid,
                        principalTable: "role",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_roleid",
                table: "user",
                column: "roleid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nomenclature");

            migrationBuilder.DropTable(
                name: "nomenclature_change");

            migrationBuilder.DropTable(
                name: "nomenclature_type");

            migrationBuilder.DropTable(
                name: "operation_card");

            migrationBuilder.DropTable(
                name: "operation_card_change");

            migrationBuilder.DropTable(
                name: "position");

            migrationBuilder.DropTable(
                name: "specification");

            migrationBuilder.DropTable(
                name: "specification_change");

            migrationBuilder.DropTable(
                name: "specification_component");

            migrationBuilder.DropTable(
                name: "specification_component_change");

            migrationBuilder.DropTable(
                name: "unit_of_measurement");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "role");
        }
    }
}
