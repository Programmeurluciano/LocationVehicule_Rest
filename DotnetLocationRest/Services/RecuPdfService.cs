using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DotnetLocationRest.DTOs;

namespace DotnetLocationRest.Services
{
    public class RecuPdfService
    {
        public byte[] GenererRecu(ReservationDetailDto reservation)
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

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(c => ComposeContent(c, reservation));
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
                    column.Item().AlignRight().Text("REÇU DE RETOUR")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Green.Medium);

                    column.Item().AlignRight().Text($"Date d'émission : {DateTime.Now:dd/MM/yyyy}")
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

                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Détails de la réservation
                column.Item().Element(c => ComposeReservationDetails(c, reservation));

                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Tableau récapitulatif
                column.Item().Element(c => ComposeTable(c, reservation));

                // Calcul de la caution
                column.Item().Element(c => ComposeCautionCalcul(c, reservation));

                // Note de remerciement
                column.Item().PaddingTop(20).Background(Colors.Blue.Lighten4).Padding(10).Text(text =>
                {
                    text.Span("Merci pour votre confiance ! ").Bold();
                    text.Span("AutoFlex vous souhaite une bonne route.");
                });
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
                column.Item().Text("DÉTAILS DE LA LOCATION").Bold().FontSize(14);
                column.Item().PaddingTop(5).Text($"Réservation N° : {reservation.Id}");
                column.Item().Text($"Véhicule : {reservation.VehiculeMarque} {reservation.VehiculeModel}");
                column.Item().Text($"Période prévue : {reservation.DateDebut:dd/MM/yyyy} au {reservation.DateFin:dd/MM/yyyy}");
                column.Item().Text($"Durée prévue : {reservation.NombreJours} jour(s)");
                
                if (reservation.DateRetourVehicule.HasValue)
                {
                    column.Item().Text($"Date de retour effective : {reservation.DateRetourVehicule:dd/MM/yyyy}")
                        .FontColor(reservation.JoursRetard > 0 ? Colors.Red.Medium : Colors.Green.Medium)
                        .Bold();
                    
                    if (reservation.JoursRetard > 0)
                    {
                        column.Item().Text($"⚠️ Retard : {reservation.JoursRetard} jour(s)")
                            .FontColor(Colors.Red.Medium)
                            .Bold();
                    }
                    else
                    {
                        column.Item().Text("✓ Retour dans les délais")
                            .FontColor(Colors.Green.Medium)
                            .Bold();
                    }
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
                    header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Description").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Quantité").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Prix unitaire").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Total").FontColor(Colors.White).Bold();
                });

                // Location
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Location véhicule");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{reservation.NombreJours} jour(s)");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.VehiculePrixJour:N0} Ar");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.PrixTotal:N0} Ar").Bold();

                // Pénalité (si applicable)
                if (reservation.MontantPenalite.HasValue && reservation.MontantPenalite.Value > 0)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Pénalité de retard");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{reservation.JoursRetard} jour(s)");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.PenaliteJournaliere:N0} Ar");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{reservation.MontantPenalite:N0} Ar").FontColor(Colors.Red.Medium).Bold();
                }
            });
        }

        void ComposeCautionCalcul(IContainer container, ReservationDetailDto reservation)
        {
            var cautionRemboursee = reservation.Caution - (reservation.MontantPenalite ?? 0);

            container.AlignRight().Column(column =>
            {
                column.Spacing(5);
                column.Item().BorderTop(2).BorderColor(Colors.Grey.Medium).PaddingTop(10);

                // Montant total payé
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Montant total de la location").Bold();
                    row.RelativeItem().AlignRight().Text($"{reservation.PrixTotal:N0} Ar").Bold();
                });

                column.Item().PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5);

                // Caution versée
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Caution versée à la réservation");
                    row.RelativeItem().AlignRight().Text($"{reservation.Caution:N0} Ar");
                });

                // Pénalité (si applicable)
                if (reservation.MontantPenalite.HasValue && reservation.MontantPenalite.Value > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Pénalité retenue (retard)");
                        row.RelativeItem().AlignRight().Text($"- {reservation.MontantPenalite:N0} Ar").FontColor(Colors.Red.Medium);
                    });
                }

                // Total remboursé
                column.Item().PaddingTop(5).BorderTop(2).BorderColor(Colors.Green.Medium).PaddingTop(5);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("CAUTION REMBOURSÉE").Bold().FontSize(14);
                    row.RelativeItem().AlignRight().Text($"{cautionRemboursee:N0} Ar")
                        .Bold()
                        .FontSize(16)
                        .FontColor(Colors.Green.Medium);
                });

                if (reservation.MontantPenalite.HasValue && reservation.MontantPenalite.Value > 0)
                {
                    column.Item().PaddingTop(5).Text($"(Caution {reservation.Caution:N0} Ar - Pénalité {reservation.MontantPenalite:N0} Ar)")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium)
                        .AlignRight();
                }
            });
        }
    }
}