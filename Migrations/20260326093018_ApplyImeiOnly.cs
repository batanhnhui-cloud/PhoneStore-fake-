using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoneStore.Migrations
{
    /// <inheritdoc />
    public partial class ApplyImeiOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. CHỈ TẠO BẢNG QUẢN LÝ IMEI
            migrationBuilder.CreateTable(
                name: "DeviceImeis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Imei = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    WarrantyActivationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarrantyExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceImeis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceImeis_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceImeis_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceImeis_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 2. CHỈ TẠO BẢNG LỊCH SỬ CHUYỂN KHO
            migrationBuilder.CreateTable(
                name: "ImeiTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceImeiId = table.Column<int>(type: "int", nullable: false),
                    FromBranchId = table.Column<int>(type: "int", nullable: false),
                    ToBranchId = table.Column<int>(type: "int", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImeiTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImeiTransfers_Branches_FromBranchId",
                        column: x => x.FromBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ImeiTransfers_Branches_ToBranchId",
                        column: x => x.ToBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ImeiTransfers_DeviceImeis_DeviceImeiId",
                        column: x => x.DeviceImeiId,
                        principalTable: "DeviceImeis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. TẠO CHỈ MỤC (INDEX) ĐỂ TÌM KIẾM NHANH
            migrationBuilder.CreateIndex(name: "IX_DeviceImeis_BranchId", table: "DeviceImeis", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_DeviceImeis_Imei", table: "DeviceImeis", column: "Imei", unique: true);
            migrationBuilder.CreateIndex(name: "IX_DeviceImeis_OrderId", table: "DeviceImeis", column: "OrderId");
            migrationBuilder.CreateIndex(name: "IX_DeviceImeis_ProductId", table: "DeviceImeis", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_ImeiTransfers_DeviceImeiId", table: "ImeiTransfers", column: "DeviceImeiId");
            migrationBuilder.CreateIndex(name: "IX_ImeiTransfers_FromBranchId", table: "ImeiTransfers", column: "FromBranchId");
            migrationBuilder.CreateIndex(name: "IX_ImeiTransfers_ToBranchId", table: "ImeiTransfers", column: "ToBranchId");
        }
    }
}
