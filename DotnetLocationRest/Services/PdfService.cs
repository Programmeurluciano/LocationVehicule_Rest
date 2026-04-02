using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DotnetLocationRest.DTOs;

namespace DotnetLocationRest.Services
{
    public class PdfService
    {
        public byte[] GenererFacture(ReservationDetailDto reservation)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // En-tête
                    page.Header().Element(ComposeHeader);

                    // Contenu principal
                    page.Content().Element(c => ComposeContent(c, reservation));

                    // Pied de page
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" sur ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("AUTOFLEX")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    column.Item().Text("Location de véhicules")
                        .FontSize(12);

                    column.Item().Text("Antananarivo, Madagascar")
                        .FontSize(10);
                });

                row.RelativeItem().Column(column =>
                {
                    column.Item().AlignRight().Text("FACTURE")
                        .FontSize(20)
                        .Bold();

                    column.Item().AlignRight().Text($"Date : {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(10);
                });
            });
        }

        void ComposeContent(IContainer container, ReservationDetailDto reservation)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(10);

                // Informations client
                column.Item().Element(c => ComposeClientInfo(c, reservation));

                // Ligne de séparation
                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Détails de la réservation
                column.Item().Element(c => ComposeReservationDetails(c, reservation));

                // Ligne de séparation
                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Tableau récapitulatif
                column.Item().Element(c => ComposeTable(c, reservation));

                // Total
                column.Item().Element(c => ComposeTotal(c, reservation));

                // Notes
                if (reservation.JoursRetard.HasValue && reservation.JoursRetard.Value > 0)
                {
                    column.Item().PaddingTop(20).Background(Colors.Red.Lighten4).Padding(10).Text(text =>
                    {
                        text.Span("⚠️ RETARD : ").Bold();
                        text.Span($"{reservation.JoursRetard} jour(s) de retard. ");
                        text.Span($"Pénalité : {reservation.MontantPenalite:N0} Ar");
                    });
                }
            });
        }

        void ComposeClientInfo(IContainer container, ReservationDetailDto reservation)
        {
            container.Column(column =>
            {
                column.Item().Text("INFORMATIONS CLIENT").Bold().FontSize(14);
                column.Item().PaddingTop(5).Text($"Nom : {reservation.ClientPrenom} {reservation.ClientNom}");
                column.Item().Text($"Email : {reservation.ClientEmail}");
                column.Item().Text($"Contact : {reservation.ClientContact}");
            });
        }

        void ComposeReservationDetails(IContainer container, ReservationDetailDto reservation)
        {
            container.Column(column =>
            {
                column.Item().Text("DÉTAILS DE LA RÉSERVATION").Bold().FontSize(14);
                column.Item().PaddingTop(5).Text($"Réservation N° : {reservation.Id}");
                column.Item().Text($"Véhicule : {reservation.VehiculeMarque} {reservation.VehiculeModel}");
                column.Item().Text($"Période : {reservation.DateDebut:dd/MM/yyyy} au {reservation.DateFin:dd/MM/yyyy}");
                column.Item().Text($"Durée : {reservation.NombreJours} jour(s)");
                
                if (reservation.DateRetourVehicule.HasValue)
                {
                    column.Item().Text($"Date de retour : {reservation.DateRetourVehicule:dd/MM/yyyy}");
                }
            });
        }

        void ComposeTable(IContainer container, ReservationDetailDto reservation)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                // En-tête
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Description").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Quantité").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Prix unitaire").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Total").FontColor(Colors.White).Bold();
                });

                // Location
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Location véhicule");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{reservation.NombreJours} jour(s)");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.VehiculePrixJour:N0} Ar");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.PrixTotal:N0} Ar");

                // Caution
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Caution (remboursable)");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text("1");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.Caution:N0} Ar");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.Caution:N0} Ar");

                // Pénalité (si applicable)
                if (reservation.MontantPenalite.HasValue && reservation.MontantPenalite.Value > 0)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Pénalité de retard");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{reservation.JoursRetard} jour(s)");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.PenaliteJournaliere:N0} Ar");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"- {reservation.MontantPenalite:N0} Ar").FontColor(Colors.Red.Medium);
                }
            });
        }

        void ComposeTotal(IContainer container, ReservationDetailDto reservation)
        {
            var totalAPayer = reservation.PrixTotal + reservation.Caution;
            var totalRembourse = reservation.Caution - (reservation.MontantPenalite ?? 0);

            container.AlignRight().Column(column =>
            {
                column.Spacing(5);
                column.Item().BorderTop(1).BorderColor(Colors.Grey.Medium).PaddingTop(5);

                if (!reservation.DateRetourVehicule.HasValue)
                {
                    // Réservation en cours
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL À PAYER").Bold().FontSize(14);
                        row.RelativeItem().AlignRight().Text($"{totalAPayer:N0} Ar").Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    });
                }
                else
                {
                    // Réservation terminée
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Location payée");
                        row.RelativeItem().AlignRight().Text($"{reservation.PrixTotal:N0} Ar");
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Caution remboursée").Bold();
                        row.RelativeItem().AlignRight().Text($"{totalRembourse:N0} Ar").Bold().FontColor(Colors.Green.Medium);
                    });

                    if (reservation.MontantPenalite.HasValue && reservation.MontantPenalite.Value > 0)
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Pénalité retenue");
                            row.RelativeItem().AlignRight().Text($"{reservation.MontantPenalite:N0} Ar").FontColor(Colors.Red.Medium);
                        });
                    }
                }
            });
        }
    }
}