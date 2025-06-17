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

            if (dpDateFilter.SelectedDate.HasValue && cmbMonthFilter.SelectedIndex == 0)
            {
                filtered = filtered.Where(d => d.Date.Date == dpDateFilter.SelectedDate.Value.Date);
            }

            if (cmbMonthFilter.SelectedIndex > 0 && !dpDateFilter.SelectedDate.HasValue &&
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
            if (sender == cmbMonthFilter && cmbMonthFilter.SelectedIndex > 0)
            {
                dpDateFilter.SelectedDate = null;
            }
            else if (sender == dpDateFilter && dpDateFilter.SelectedDate.HasValue)
            {
                cmbMonthFilter.SelectedIndex = 0;
            }

            ApplyFilters();
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

            // Диалог сохранения файла
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

                        // Настройка ориентации страницы на альбомную и полей (1 см = 567 twips)
                        SectionProperties sectionProps = new SectionProperties();
                        PageSize pageSize = new PageSize
                        {
                            Width = new DocumentFormat.OpenXml.UInt32Value((uint)15840), // 11 дюймов
                            Height = new DocumentFormat.OpenXml.UInt32Value((uint)12240), // 8.5 дюймов
                            Orient = PageOrientationValues.Landscape
                        };
                        PageMargin pageMargin = new PageMargin
                        {
                            Top = 567, // 1 см
                            Bottom = 567, // 1 см
                            Left = 567, // 1 см
                            Right = 567 // 1 см
                        };
                        sectionProps.Append(pageSize);
                        sectionProps.Append(pageMargin);
                        body.Append(sectionProps);

                        // Заголовок
                        Paragraph headerPara = body.AppendChild(new Paragraph());
                        Run headerRun = headerPara.AppendChild(new Run());
                        headerRun.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "36" }, // 18 pt
                            new Bold()
                        );
                        headerRun.AppendChild(new Text("Дежурство с 22:15 до 22:45 СДАЧА ДЕЖУРСТВА СТАРОСТЕ!!!"));
                        headerPara.ParagraphProperties = new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        );

                        // Таблица
                        Table table = new Table();

                        TableProperties tableProps = new TableProperties(
                            new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct },
                            new TableBorders(
                                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                            ),
                            new TableCellMargin(
                                new TopMargin { Width = new StringValue("0"), Type = TableWidthUnitValues.Dxa }, // Интервал до 0
                                new BottomMargin { Width = new StringValue("0"), Type = TableWidthUnitValues.Dxa }, // Интервал после 0
                                new LeftMargin { Width = new StringValue("0"), Type = TableWidthUnitValues.Dxa },
                                new RightMargin { Width = new StringValue("0"), Type = TableWidthUnitValues.Dxa }
                            )
                        );
                        table.AppendChild(tableProps);

                        // Определяем диапазон комнат для этажа
                        int startRoom = floor * 100 + 1;
                        int endRoom = floor * 100 + 22; // 22 комнаты на этаж (401-422 для 4-го, 201-222 для 2-го)
                        int daysInMonth = DateTime.DaysInMonth(year, monthNumber);

                        // Заголовочная строка с датами
                        TableRow headerRow = new TableRow();
                        TableCell emptyCell = new TableCell();
                        Paragraph emptyPara = emptyCell.AppendChild(new Paragraph());
                        emptyPara.AppendChild(new Run(new Text("")));
                        emptyCell.TableCellProperties = new TableCellProperties(
                            new TableCellWidth { Width = new StringValue("50"), Type = TableWidthUnitValues.Dxa }
                        );
                        headerRow.Append(emptyCell);

                        for (int day = 1; day <= daysInMonth; day++)
                        {
                            TableCell dateCell = new TableCell();
                            Paragraph datePara = dateCell.AppendChild(new Paragraph());
                            Run dateRun = datePara.AppendChild(new Run());
                            dateRun.RunProperties = new RunProperties(
                                new RunFonts { Ascii = "Times New Roman" },
                                new FontSize { Val = "32" }, // 16 pt
                                new Bold()
                            );
                            dateRun.AppendChild(new Text(day.ToString("D2"))); // 01, 02, ...
                            dateCell.TableCellProperties = new TableCellProperties(
                                new TableCellWidth { Width = new StringValue((1000 / daysInMonth).ToString()), Type = TableWidthUnitValues.Dxa },
                                new ParagraphProperties(new Justification { Val = JustificationValues.Center })
                            );
                            headerRow.Append(dateCell);
                        }
                        table.Append(headerRow);

                        // Строки с комнатами
                        for (int room = startRoom; room <= endRoom; room++)
                        {
                            TableRow roomRow = new TableRow();
                            TableCell roomCell = new TableCell();
                            Paragraph roomPara = roomCell.AppendChild(new Paragraph());
                            Run roomRun = roomPara.AppendChild(new Run());
                            roomRun.RunProperties = new RunProperties(
                                new RunFonts { Ascii = "Times New Roman" },
                                new FontSize { Val = "32" }, // 16 pt
                                new Bold()
                            );
                            roomRun.AppendChild(new Text(room.ToString()));
                            roomCell.TableCellProperties = new TableCellProperties(
                                new TableCellWidth { Width = new StringValue("50"), Type = TableWidthUnitValues.Dxa },
                                new ParagraphProperties(new Justification { Val = JustificationValues.Center })
                            );
                            roomRow.Append(roomCell);

                            for (int day = 1; day <= daysInMonth; day++)
                            {
                                var date = new DateTime(year, monthNumber, day);
                                var duty = filteredSchedules.FirstOrDefault(ds => ds.Date.Date == date.Date && ds.Room == room);
                                TableCell dataCell = new TableCell();
                                Paragraph dataPara = dataCell.AppendChild(new Paragraph());
                                Run dataRun = dataPara.AppendChild(new Run());
                                dataRun.RunProperties = new RunProperties(
                                    new RunFonts { Ascii = "Times New Roman" },
                                    new FontSize { Val = "32" } // 16 pt
                                );

                                // Проверка на выходной (суббота или воскресенье)
                                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    dataRun.RunProperties.Append(new Color { Val = "FF0000" }); // Красный цвет для выходных
                                }

                                // Закрашивание ячейки, если есть дежурство
                                if (duty != null)
                                {
                                    dataCell.TableCellProperties = new TableCellProperties(
                                        new Shading { Val = ShadingPatternValues.Solid, Color = "auto", Fill = "808080" } // Серый фон для дежурства
                                    );
                                }

                                dataCell.TableCellProperties ??= new TableCellProperties(
                                    new ParagraphProperties(new Justification { Val = JustificationValues.Center })
                                );
                                roomRow.Append(dataCell);
                            }
                            table.Append(roomRow);
                        }

                        body.AppendChild(table);

                        // Футер с информацией о старостах (на новой строке)
                        Paragraph footerPara1 = body.AppendChild(new Paragraph());
                        Run footerRun1 = footerPara1.AppendChild(new Run());
                        footerRun1.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "28" }, // 14 pt
                            new Italic()
                        );
                        footerRun1.AppendChild(new Text(GetStarostaInfo(floor)));
                        footerPara1.ParagraphProperties = new ParagraphProperties(
                            new Justification { Val = JustificationValues.Left }
                        );

                        Paragraph footerPara2 = body.AppendChild(new Paragraph());
                        Run footerRun2 = footerPara2.AppendChild(new Run());
                        footerRun2.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "28" }, // 14 pt
                            new Italic()
                        );
                        footerRun2.AppendChild(new Text("Пакеты брать заранее у старост!"));
                        footerPara2.ParagraphProperties = new ParagraphProperties(
                            new Justification { Val = JustificationValues.Left }
                        );

                        Paragraph footerPara3 = body.AppendChild(new Paragraph());
                        Run footerRun3 = footerPara3.AppendChild(new Run());
                        footerRun3.RunProperties = new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "28" }, // 14 pt
                            new Italic()
                        );
                        footerRun3.AppendChild(new Text("Обязательно мыть мусорный бак!"));
                        footerPara3.ParagraphProperties = new ParagraphProperties(
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

        private string GetStarostaInfo(int floor)
        {
            using (var studentsContext = new StudentsContext())
            {
                // Поиск студентов с ролью "Староста этажа" или "Председатель" для данного этажа
                var starostas = studentsContext.Students
                    .Where(s => s.Floor == floor && (s.StudentRole == StudentRole.Староста_этажа || s.StudentRole == StudentRole.Председатель_Студенческого_совета_общежития))
                    .Select(s => $"{s.FIO} ({s.Room})")
                    .ToList();

                return starostas.Any() ? $"Старосты: {string.Join(", ", starostas)}" : "Старосты: не назначены";
            }
        }
    }
}

