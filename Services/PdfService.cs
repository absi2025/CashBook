using System;
using System.Collections.Generic;
using System.Linq;
using CashBook.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CashBook.Services
{
    public class PdfService
    {
        private static readonly string FontFamily = "Tahoma";

        static PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static void GenerateReport(string filePath, string title, string companyName,
            DateTime from, DateTime to, List<Transaction> transactions, string currency)
        {
            var ordered = transactions.OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();

            decimal totalIn = ordered.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
            decimal totalOut = ordered.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(FontFamily));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(companyName).FontSize(20).Bold().AlignCenter();
                        col.Item().PaddingTop(5).Text(title).FontSize(14).AlignCenter();
                        col.Item().PaddingTop(3).Text($"من {from:yyyy/MM/dd} إلى {to:yyyy/MM/dd}")
                            .FontSize(10).AlignCenter().FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(28);
                            c.ConstantColumn(70);
                            c.ConstantColumn(45);
                            c.ConstantColumn(70);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2f);
                            c.ConstantColumn(75);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("#");
                            h.Cell().Element(HeaderStyle).Text("التاريخ");
                            h.Cell().Element(HeaderStyle).Text("النوع");
                            h.Cell().Element(HeaderStyle).Text("الحساب");
                            h.Cell().Element(HeaderStyle).Text("الجهة");
                            h.Cell().Element(HeaderStyle).Text("البيان");
                            h.Cell().Element(HeaderStyle).Text("المبلغ");
                        });

                        int i = 1;
                        foreach (var t in ordered)
                        {
                            string accText = t.Type == TransactionType.Transfer
                                ? $"{t.AccountDisplay} <- {t.ToAccountDisplay}"
                                : t.AccountDisplay;

                            string amount = t.Type switch
                            {
                                TransactionType.Income => $"+ {t.Amount:N2}",
                                TransactionType.Expense => $"- {t.Amount:N2}",
                                _ => $"{t.Amount:N2}"
                            };

                            var bg = i % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                            table.Cell().Background(bg).Element(CellStyle).Text(i.ToString());
                            table.Cell().Background(bg).Element(CellStyle).Text(t.Date.ToString("yyyy/MM/dd"));
                            table.Cell().Background(bg).Element(CellStyle).Text(t.TypeDisplay);
                            table.Cell().Background(bg).Element(CellStyle).Text(accText);
                            table.Cell().Background(bg).Element(CellStyle).Text(t.Party);
                            table.Cell().Background(bg).Element(CellStyle).Text(t.Note);
                            table.Cell().Background(bg).Element(CellStyle).Text(amount);
                            i++;
                        }
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Medium).PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text($"إجمالي الدخول: {totalIn:N2} {currency}").Bold();
                            row.RelativeItem().Text($"إجمالي الخروج: {totalOut:N2} {currency}").Bold();
                            row.RelativeItem().Text($"الصافي: {(totalIn - totalOut):N2} {currency}").Bold();
                        });
                        col.Item().PaddingTop(15).AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber().FontSize(9);
                            x.Span(" / ").FontSize(9);
                            x.TotalPages().FontSize(9);
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        private static IContainer HeaderStyle(IContainer c) =>
            c.Background(Colors.Grey.Darken2).Padding(5)
             .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

        private static IContainer CellStyle(IContainer c) => c.Padding(4);
    }
}
