using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DormitoryPATDesktop.Pages.DutySchedule
{
    public partial class Item : UserControl
    {
        private readonly DutyScheduleContext _context = new DutyScheduleContext();
        private List<Models.DutySchedule> _allSchedules;

        public Item()
        {
            InitializeComponent();
            _allSchedules = new List<Models.DutySchedule>();
            ScheduleDataGrid.BeginningEdit += (s, e) => e.Cancel = true;
            LoadSchedules();
            PopulateMonthComboBox();
        }

        private void LoadSchedules()
        {
            try
            {
                _allSchedules = _context.DutySchedule
                    .ToList() ?? new List<Models.DutySchedule>();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расписаний: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateMonthComboBox()
        {
            var currentMonth = DateTime.Now.ToString("MMMM");
            foreach (ComboBoxItem item in cmbMonthFilter.Items)
            {
                if (item.Content.ToString() == currentMonth)
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }

        private void ApplyFilters()
        {
            if (_allSchedules == null) _allSchedules = new List<Models.DutySchedule>();
            if (ScheduleDataGrid == null) return;

            IQueryable<Models.DutySchedule> filtered = _allSchedules.AsQueryable();

            if (cmbFloorFilter.SelectedIndex > 0 &&
                cmbFloorFilter.SelectedItem is ComboBoxItem selectedFloor &&
                int.TryParse(selectedFloor.Content.ToString(), out var floor))
            {
                filtered = filtered.Where(d => d.Floor == floor);
            }

            if (!string.IsNullOrWhiteSpace(txtRoomFilter.Text) &&
                int.TryParse(txtRoomFilter.Text, out var room))
            {
                filtered = filtered.Where(d => d.Room == room);
            }

            if (dpDateFilter.SelectedDate.HasValue)
            {
                filtered = filtered.Where(d => d.Date.Date == dpDateFilter.SelectedDate.Value.Date);
            }

            if (cmbMonthFilter.SelectedIndex > 0 &&
                cmbMonthFilter.SelectedItem is ComboBoxItem selectedMonth)
            {
                var monthName = selectedMonth.Content.ToString();
                var monthNumber = DateTime.ParseExact(monthName, "MMMM", System.Globalization.CultureInfo.CurrentCulture).Month;
                filtered = filtered.Where(d => d.Date.Month == monthNumber);
            }

            var currentMonth = DateTime.Now.Month;
            var sorted = filtered.OrderBy(d => d.Date.Month != currentMonth)
                                .ThenBy(d => d.Floor)
                                .ThenBy(d => d.Date);

            ScheduleDataGrid.ItemsSource = sorted.ToList();
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            LoadSchedules();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSchedules();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Models.DutySchedule schedule)
            {
                var confirm = MessageBox.Show($"Удалить дежурство {schedule.ScheduleId}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.DutySchedule.Remove(schedule);
                        _context.SaveChanges();
                        LoadSchedules();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ScheduleDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ScheduleDataGrid.SelectedItem is Models.DutySchedule selectedSchedule)
            {
                var editPage = new Add(selectedSchedule);
                MainWindow.init.OpenPages(editPage);
            }
        }

        private void ExportToDocx_Click(object sender, RoutedEventArgs e)
        {
            var selectedFloorItem = cmbFloorFilter.SelectedItem as ComboBoxItem;
            var selectedMonthItem = cmbMonthFilter.SelectedItem as ComboBoxItem;

            if (selectedFloorItem == null || selectedFloorItem.Content.ToString() == "Все этажи" || selectedMonthItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите конкретный этаж и месяц.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int floor = int.Parse(selectedFloorItem.Content.ToString());
            string monthName = selectedMonthItem.Content.ToString();
            int monthNumber = DateTime.ParseExact(monthName, "MMMM", System.Globalization.CultureInfo.CurrentCulture).Month;
            int year = DateTime.Now.Year; // Текущий год: 2025

            var filteredSchedules = _allSchedules.AsQueryable()
                .Where(d => d.Floor == floor && d.Date.Month == monthNumber && d.Date.Year == year)
                .ToList();

            var month = new DateTime(year, monthNumber, 1);

            // Открываем диалог сохранения файла
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Word Document (*.docx)|*.docx",
                Title = "Сохранить расписание дежурств",
                FileName = $"DutySchedule_Floor{floor}_{monthName}_{year}.docx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (WordprocessingDocument doc = WordprocessingDocument.Create(saveFileDialog.FileName, WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = doc.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        Body body = mainPart.Document.AppendChild(new Body());

                        // Заголовок
                        Paragraph titlePara = body.AppendChild(new Paragraph());
                        Run titleRun = titlePara.AppendChild(new Run());
                        titleRun.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "36" }, // Шрифт 18 pt
                            new Bold()
                        );
                        titleRun.AppendChild(new Text($"Расписание дежурств - Этаж {floor}"));
                        titlePara.ParagraphProperties = new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Center }
                        );

                        // Добавляем отступ после заголовка
                        Paragraph spacingPara = body.AppendChild(new Paragraph());
                        spacingPara.ParagraphProperties = new ParagraphProperties(
                            new SpacingBetweenLines { After = "200" } // Отступ 200 twips (примерно 0.1 дюйма)
                        );

                        // Таблица
                        Table table = new Table();

                        TableProperties tableProps = new TableProperties(
                            new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct }, // Таблица на всю ширину
                            new TableBorders(
                                new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                            )
                        );
                        table.AppendChild(tableProps);

                        // Заголовки таблицы
                        TableRow headerRow = new TableRow();
                        TableCell dateCell = new TableCell();
                        Paragraph datePara = dateCell.AppendChild(new Paragraph());
                        Run dateRun = datePara.AppendChild(new Run());
                        dateRun.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "32" }, // Шрифт 16 pt
                            new Bold()
                        );
                        dateRun.AppendChild(new Text("Дата"));
                        dateCell.TableCellProperties = new TableCellProperties(
                            new TableCellWidth { Width = "15%", Type = TableWidthUnitValues.Pct },
                            new ParagraphProperties(new Justification { Val = JustificationValues.Center }) // Выравнивание по центру
                        );
                        headerRow.Append(dateCell);

                        TableCell roomCell = new TableCell();
                        Paragraph roomPara = roomCell.AppendChild(new Paragraph());
                        Run roomRun = roomPara.AppendChild(new Run());
                        roomRun.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "32" }, // Шрифт 16 pt
                            new Bold()
                        );
                        roomRun.AppendChild(new Text("Комната"));
                        roomCell.TableCellProperties = new TableCellProperties(
                            new TableCellWidth { Width = "85%", Type = TableWidthUnitValues.Pct },
                            new ParagraphProperties(new Justification { Val = JustificationValues.Center }) // Выравнивание по центру
                        );
                        headerRow.Append(roomCell);

                        table.Append(headerRow);

                        // Данные
                        for (int day = 1; day <= DateTime.DaysInMonth(month.Year, month.Month); day++)
                        {
                            var date = new DateTime(month.Year, month.Month, day);
                            var duty = filteredSchedules.FirstOrDefault(ds => ds.Date.Date == date.Date);
                            var room = duty?.Room.ToString() ?? "Не назначено";

                            TableRow dataRow = new TableRow();
                            TableCell dateDataCell = new TableCell();
                            Paragraph dateDataPara = dateDataCell.AppendChild(new Paragraph());
                            Run dateDataRun = dateDataPara.AppendChild(new Run());
                            dateDataRun.RunProperties = new RunProperties(
                                new RunFonts { Ascii = "Times New Roman" },
                                new FontSize { Val = "32" } // Шрифт 16 pt
                            );
                            dateDataRun.AppendChild(new Text(date.ToString("dd.MM")));
                            dateDataCell.TableCellProperties = new TableCellProperties(
                                new TableCellWidth { Width = "15%", Type = TableWidthUnitValues.Pct },
                                new ParagraphProperties(new Justification { Val = JustificationValues.Center }) // Выравнивание по центру
                            );
                            dataRow.Append(dateDataCell);

                            TableCell roomDataCell = new TableCell();
                            Paragraph roomDataPara = roomDataCell.AppendChild(new Paragraph());
                            Run roomDataRun = roomDataPara.AppendChild(new Run());
                            roomDataRun.RunProperties = new RunProperties(
                                new RunFonts { Ascii = "Times New Roman" },
                                new FontSize { Val = "32" } // Шрифт 16 pt
                            );
                            roomDataRun.AppendChild(new Text(room));
                            roomDataCell.TableCellProperties = new TableCellProperties(
                                new TableCellWidth { Width = "85%", Type = TableWidthUnitValues.Pct },
                                new ParagraphProperties(new Justification { Val = JustificationValues.Center }) // Выравнивание по центру
                            );
                            dataRow.Append(roomDataCell);

                            table.Append(dataRow);
                        }

                        body.AppendChild(table);

                        // Футер
                        Paragraph footerPara = body.AppendChild(new Paragraph());
                        Run footerRun = footerPara.AppendChild(new Run());
                        footerRun.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "28" }, // Шрифт 14 pt
                            new Italic()
                        );
                        footerRun.AppendChild(new Text($"Этаж {floor} - {month:MMMM yyyy}"));
                        footerPara.ParagraphProperties = new ParagraphProperties(
                            new Justification { Val = JustificationValues.Left }
                        );

                        doc.Save();
                    }
                    MessageBox.Show("Файл успешно сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}