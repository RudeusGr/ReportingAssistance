﻿using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using ReportingAssistance.Model;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReportingAssistance.View
{
    /// <summary>
    /// Lógica de interacción para MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            /*
            XLWorkbook workbook = new("C:\\Users\\sistemas\\Downloads\\TESTER VENTA RUTA 4.xlsx");
            IXLWorksheet sheet = workbook.Worksheet(1);

            var lastRow = sheet.LastRowUsed().RangeAddress.LastAddress.RowNumber;

            string connectionString = "Server=SOPORTETI\\SQLEXPRESS;Database=employees ;Trusted_Connection=SSPI;MultipleActiveResultSets=true;Trust Server Certificate=true";

            using SqlConnection DBConnection = new(connectionString);

            DBConnection.Open();
            string sqlQuery = "INSERT INTO [dbo].[bulktoroute] (cverut, fecalt, venta) values (@cverut, @fecalt, @venta);";

            for (int i = 2; i <= lastRow; i++)
            {
                IXLRow currentRow = sheet.Row(i);

                int cverut = currentRow.Cell(1).GetValue<int>();
                string fecalt = currentRow.Cell(2).GetString();
                int venta = currentRow.Cell(3).GetValue<int>();

                var registers = DBConnection.Query(sqlQuery, new { cverut,fecalt = DateTime.Parse(fecalt), venta });
            }
            workbook.Dispose();
            DBConnection.Close();
            DBConnection.Dispose();
            */
        }

        private String? FilePathBiotimer;
        private String? FilePathAssistance;
        private String? FilePathAssistanceFest;
        private string? DateInitial;
        private string? DateFinal;
        private Dictionary<int, Employee> DicEmployees = new();
        private Dictionary<int, Employee> DicEmployeesFest = new();
        private readonly String PathDir = $"C:\\Users\\{System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\')[1]}\\Documents\\ReportingAssistance\\";


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState= WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnSearhFileBiotimer_Click(object sender, RoutedEventArgs e)
        {
            if (SearchFile(1)) UploadDataBiotimer();
        }

        private void btnSearhFileAssistanceRoute_Click(object sender, RoutedEventArgs e)
        {
            if (SearchFile(2)) UploadDataAssistance();
        }

        private void btnSearhFileFestRoute_Click(object sender, RoutedEventArgs e)
        {
            if (SearchFile(3)) UploadDataAssistanceFest();
        }

        private void btnRegenerateReport_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            if (FilePathBiotimer is null || FilePathAssistance is null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("Hace falta que cargue algun archivo, ya sea el arhivo de biotimer o el de asistencia, favor de verificar", "Falta cargar algun archivo");
                return;
            }

            if (!Directory.Exists(PathDir))
            {
                Directory.CreateDirectory(PathDir);
            }


            if (txtSalaryAux.Text == "" || txtSalaryDriver.Text == "")
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("Asegurese de haber llenado los campos de salarios.", "Salarios no proporcionados");
                return;
            }

            decimal salaryDriver;
            decimal salaryAux;

            try
            {
                salaryDriver = decimal.Parse(txtSalaryDriver.Text);
                salaryAux = decimal.Parse(txtSalaryAux.Text);
            }
            catch (FormatException fex)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"El campo de salarios debe de ser un numero\n\nError: {fex}", "Error de salarios");
                return;
            }

            string PathExcel = PathDir + $"Reporte Asistencia {DateTime.Now:yyyy-MM-dd HH.mm.ss}.xlsx";

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Asistencias - Rutas");
                worksheet.ShowGridLines = false;

                //title
                worksheet.Range("A1:P2").Merge();
                worksheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.DarkBlue;
                worksheet.Cell("A1").Style.Font.FontColor = XLColor.White;
                worksheet.Cell("A1").Style.Font.FontSize = 20;
                worksheet.Cell("A1").Style.Font.FontName = "Arial Rounded MT Bold";
                worksheet.Cell("A1").Value = $"Reporte de Asistencias de personal de ruta del dia {DateInitial} al {DateFinal}";

                //columns
                worksheet.Range("A3:A4").Merge();
                worksheet.Cell("A3").Value = "ID Empleado";
                worksheet.Range("B3:D4").Merge();
                worksheet.Cell("B3").Value = "Nombre Empleado";
                worksheet.Range("E3:F4").Merge();
                worksheet.Cell("E3").Value = "Dias Trabajados";
                worksheet.Range("G3:G4").Merge();
                worksheet.Cell("G3").Value = "Retrasos";
                worksheet.Range("H3:I4").Merge();
                worksheet.Cell("H3").Value = "Bono Puntualidad";
                worksheet.Range("J3:J4").Merge();
                worksheet.Cell("J3").Value = "Bultos";
                worksheet.Range("K3:K4").Merge();
                worksheet.Cell("K3").Value = "Bono Venta";
                worksheet.Range("L3:M4").Merge();
                worksheet.Cell("L3").Value = "Salario Diario";
                worksheet.Range("N3:O4").Merge();
                worksheet.Cell("N3").Value = "Total Por Dias";
                worksheet.Range("P3:P4").Merge();
                worksheet.Cell("P3").Value = "Total";

                worksheet.Range("A3:P4").Style.Fill.BackgroundColor = XLColor.Orange;
                worksheet.Range("A3:P4").Style.Font.FontColor = XLColor.White;
                worksheet.Range("A3:P4").Style.Font.FontSize = 14;
                worksheet.Range("A3:P4").Style.Font.FontName = "Arial Rounded MT Bold";

                int row = 5;

                foreach (var employee in DicEmployees)
                {
                    worksheet.Cell("A" + row).Value = employee.Value.Id;
                    worksheet.Range("B" + row + ":D" + row).Merge();
                    worksheet.Cell("B" + row).Value = employee.Value.Name;
                    worksheet.Range("E" + row + ":F" + row).Merge();
                    worksheet.Cell("E" + row).Value = employee.Value.Assistance;
                    worksheet.Cell("G" + row).Value = employee.Value.Delays;
                    worksheet.Range("H" + row + ":I" + row).Merge();

                    decimal bonusPunctuality = 0;

                    //bono puntualidad
                    if (employee.Value.Assistance < 6 || employee.Value.Delays >= 2)
                    {
                        worksheet.Cell("H" + row).Value = bonusPunctuality;
                    }
                    else
                    {
                        bonusPunctuality = 7 * 50;
                        worksheet.Cell("H" + row).Value = bonusPunctuality;
                    }

                    //bultos cargados
                    worksheet.Cell("J" + row).Value = employee.Value.Bulk;

                    //Comision por bultos
                    decimal commission = (decimal)(employee.Value.Bulk * .15);
                    if (commission >= 300)
                    {
                        worksheet.Cell("K" + row).Value = 300;
                        commission = 300;
                    }
                    else if (commission <= 150)
                    {
                        worksheet.Cell("K" + row).Value = 150;
                        commission = 150;
                    }
                    else
                    {
                        worksheet.Cell("K" + row).Value = commission;
                    }

                    //salario Diario
                    worksheet.Range("L" + row + ":M" + row).Merge();
                    worksheet.Cell("L" + row).Value = employee.Value.isDriver ? salaryDriver : salaryAux;
                    //sub total (salario por dia trabajado)
                    worksheet.Range("N" + row + ":O" + row).Merge();
                    worksheet.Cell("N" + row).Value = ((employee.Value.Assistance == 6 ? employee.Value.Assistance + 1: employee.Value.Assistance) * (employee.Value.isDriver ? salaryDriver : salaryAux)) + (employee.Value.isWorkMonday ? (employee.Value.isDriver ? salaryDriver : salaryAux) : 0);
                    //total
                    worksheet.Cell("P" + row).Value = bonusPunctuality + commission + ((employee.Value.Assistance == 6 ? employee.Value.Assistance + 1 : employee.Value.Assistance) * (employee.Value.isDriver ? salaryDriver : salaryAux)) + (employee.Value.isWorkMonday ? (employee.Value.isDriver ? salaryDriver : salaryAux) : 0);

                    row++;
                }

                worksheet.Column("A").Width = 17;
                worksheet.Column("B").Width = 12;
                worksheet.Column("C").Width = 12;
                worksheet.Column("D").Width = 12;
                worksheet.Column("E").Width = 11;
                worksheet.Column("F").Width = 11;
                worksheet.Column("G").Width = 15;
                worksheet.Column("H").Width = 12;
                worksheet.Column("I").Width = 12;
                worksheet.Column("J").Width = 10;
                worksheet.Column("K").Width = 15;
                worksheet.Column("L").Width = 10;
                worksheet.Column("M").Width = 10;
                worksheet.Column("N").Width = 10;
                worksheet.Column("O").Width = 10;
                worksheet.Column("P").Width = 15;

                workbook.SaveAs(PathExcel);
                workbook.Dispose();
            }
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            MessageBox.Show("Se a creado satisfactoreamente el archivo de asistencias de ruta", "Archivo Generado");
            OpenReport(PathExcel);
        }

        private void btnRegenerateReportFest_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            if (FilePathBiotimer is null || FilePathAssistanceFest is null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("Hace falta que cargue algun archivo, ya sea el arhivo de biotimer o el de asistencia Fetejo, favor de verificar", "Falta cargar algun archivo");
                return;
            }

            if (!Directory.Exists(PathDir))
            {
                Directory.CreateDirectory(PathDir);
            }


            if (txtSalaryAux.Text == "" || txtSalaryDriver.Text == "")
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("Asegurese de haber llenado los campos de salarios.", "Salarios no proporcionados");
                return;
            }

            decimal salaryDriver;
            decimal salaryAux;

            try
            {
                salaryDriver = decimal.Parse(txtSalaryDriver.Text);
                salaryAux = decimal.Parse(txtSalaryAux.Text);
            }
            catch (FormatException fex)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"El campo de salarios debe de ser un numero\n\nError: {fex}", "Error de salarios");
                return;
            }

            string PathExcel = PathDir + $"Reporte Festejo {DateTime.Now:yyyy-MM-dd HH.mm.ss}.xlsx";

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Asistencias - Festejo");
                worksheet.ShowGridLines = false;

                //title
                worksheet.Range("A1:P2").Merge();
                worksheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.DarkBlue;
                worksheet.Cell("A1").Style.Font.FontColor = XLColor.White;
                worksheet.Cell("A1").Style.Font.FontSize = 20;
                worksheet.Cell("A1").Style.Font.FontName = "Arial Rounded MT Bold";
                worksheet.Cell("A1").Value = $"Reporte de Asistencias de personal de ruta del dia {DateInitial} al {DateFinal}";

                //columns
                worksheet.Range("A3:A4").Merge();
                worksheet.Cell("A3").Value = "ID Empleado";
                worksheet.Range("B3:D4").Merge();
                worksheet.Cell("B3").Value = "Nombre Empleado";
                worksheet.Range("E3:F4").Merge();
                worksheet.Cell("E3").Value = "Dias Trabajados";
                worksheet.Range("G3:G4").Merge();
                worksheet.Cell("G3").Value = "Retrasos";
                worksheet.Range("H3:I4").Merge();
                worksheet.Cell("H3").Value = "Bono Puntualidad";
                worksheet.Range("J3:J4").Merge();
                worksheet.Cell("J3").Value = "Bultos";
                worksheet.Range("K3:K4").Merge();
                worksheet.Cell("K3").Value = "Bono Venta";
                worksheet.Range("L3:M4").Merge();
                worksheet.Cell("L3").Value = "Salario Diario";
                worksheet.Range("N3:O4").Merge();
                worksheet.Cell("N3").Value = "Total Por Dias";
                worksheet.Range("P3:P4").Merge();
                worksheet.Cell("P3").Value = "Total";

                worksheet.Range("A3:P4").Style.Fill.BackgroundColor = XLColor.Orange;
                worksheet.Range("A3:P4").Style.Font.FontColor = XLColor.White;
                worksheet.Range("A3:P4").Style.Font.FontSize = 14;
                worksheet.Range("A3:P4").Style.Font.FontName = "Arial Rounded MT Bold";

                int row = 5;

                foreach (var employee in DicEmployeesFest)
                {
                    worksheet.Cell("A" + row).Value = employee.Value.Id;
                    worksheet.Range("B" + row + ":D" + row).Merge();
                    worksheet.Cell("B" + row).Value = employee.Value.Name;
                    worksheet.Range("E" + row + ":F" + row).Merge();
                    worksheet.Cell("E" + row).Value = employee.Value.Assistance;
                    worksheet.Cell("G" + row).Value = employee.Value.Delays;
                    worksheet.Range("H" + row + ":I" + row).Merge();

                    decimal bonusPunctuality = 0;

                    //bono puntualidad
                    if (employee.Value.Assistance < 6 || employee.Value.Delays >= 2)
                    {
                        worksheet.Cell("H" + row).Value = bonusPunctuality;
                    }
                    else
                    {
                        bonusPunctuality = 7 * 50;
                        worksheet.Cell("H" + row).Value = bonusPunctuality;
                    }

                    //bultos cargados
                    worksheet.Cell("J" + row).Value = employee.Value.Bulk;

                    decimal commission = 0;

                    if (employee.Value.isDriver)
                    {
                        //comision por dinero
                        commission = (decimal)(employee.Value.Money * .005);
                        if (commission >= 2500)
                        {
                            commission = 2500;
                        }
                        worksheet.Cell("K" + row).Value = commission;
                    } else
                    {
                        //Comision por bultos
                        commission = (decimal)(employee.Value.Bulk * .5);
                        if (commission >= 600)
                        {
                            commission = 600;
                        }
                        worksheet.Cell("K" + row).Value = commission;
                    }

                    //salario Diario
                    worksheet.Range("L" + row + ":M" + row).Merge();
                    worksheet.Cell("L" + row).Value = employee.Value.isDriver ? salaryDriver : salaryAux;
                    //sub total (salario por dia trabajado)
                    worksheet.Range("N" + row + ":O" + row).Merge();
                    worksheet.Cell("N" + row).Value = ((employee.Value.Assistance == 6 ? employee.Value.Assistance + 1 : employee.Value.Assistance) * (employee.Value.isDriver ? salaryDriver : salaryAux)) + (employee.Value.isWorkMonday ? (employee.Value.isDriver ? salaryDriver : salaryAux) : 0);
                    //total
                    worksheet.Cell("P" + row).Value = bonusPunctuality + commission + ((employee.Value.Assistance == 6 ? employee.Value.Assistance + 1 : employee.Value.Assistance) * (employee.Value.isDriver ? salaryDriver : salaryAux)) + (employee.Value.isWorkMonday ? (employee.Value.isDriver ? salaryDriver : salaryAux) : 0);

                    row++;
                }

                worksheet.Column("A").Width = 17;
                worksheet.Column("B").Width = 12;
                worksheet.Column("C").Width = 12;
                worksheet.Column("D").Width = 12;
                worksheet.Column("E").Width = 11;
                worksheet.Column("F").Width = 11;
                worksheet.Column("G").Width = 15;
                worksheet.Column("H").Width = 12;
                worksheet.Column("I").Width = 12;
                worksheet.Column("J").Width = 10;
                worksheet.Column("K").Width = 15;
                worksheet.Column("L").Width = 10;
                worksheet.Column("M").Width = 10;
                worksheet.Column("N").Width = 10;
                worksheet.Column("O").Width = 10;
                worksheet.Column("P").Width = 15;

                workbook.SaveAs(PathExcel);
                workbook.Dispose();
            }
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            MessageBox.Show("Se a creado satisfactoreamente el archivo de asistencias festejo", "Archivo Generado");
            OpenReport(PathExcel);
        }

        private Boolean SearchFile(int option)
        {
            Boolean fileExist = false;
            var browseableOpenFileDialog = new OpenFileDialog();
            if (browseableOpenFileDialog.ShowDialog() == true)
            {
                var fileExtension = browseableOpenFileDialog.FileName.Split('.');
                if (!fileExtension[^1].Equals("xlsx"))
                {
                    MessageBox.Show("El archivo enviado no es un archivo de Excel valido, los archivos de Excel tiene la extencion 'xlsx'", "Tipo de archivo incorrecto");
                }
                else
                {
                    switch (option)
                    {
                        case 1:
                            this.txtFileNameBiotimer.Text = browseableOpenFileDialog.FileName.Split('\\').Last();
                            FilePathBiotimer = browseableOpenFileDialog.FileName;
                            fileExist = true;
                            break;
                        case 2:
                            this.txtFileNameAssistanceRoute.Text = browseableOpenFileDialog.FileName.Split('\\').Last();
                            FilePathAssistance = browseableOpenFileDialog.FileName;
                            fileExist = true;
                            break;
                        case 3:
                            this.txtFileNameFestRoute.Text = browseableOpenFileDialog.FileName.Split('\\').Last();
                            FilePathAssistanceFest = browseableOpenFileDialog.FileName;
                            fileExist = true;
                            break;
                    }
                }
            }
            return fileExist;
        }

        private void UploadDataBiotimer()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            if (FilePathBiotimer is null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("No es posible encontrar el archivo, asegurese de haberlo seleccionado.", "Archivo no encontrado");
                return;
            } else
            {
                DicEmployees.Clear();
                FilePathAssistance = null;
                txtFileNameAssistanceRoute.Text = "";
            }

            try
            {
                using XLWorkbook workbook = new(FilePathBiotimer);
                IXLWorksheet sheet = workbook.Worksheet(1);

                var lastRow = sheet.LastRowUsed().RangeAddress.LastAddress.RowNumber;
                DateInitial = sheet.Row(2).Cell(4).GetString().Remove(10);
                DateFinal = sheet.Row(2).Cell(4).GetString().Remove(10);

                for (int i = 2; i <= lastRow; i++)
                {
                    IXLRow currentRow = sheet.Row(i);

                    int idEmployee = currentRow.Cell(1).GetValue<int>();
                    string nameEmployee = currentRow.Cell(2).GetString() + " " + currentRow.Cell(3).GetString();
                    string dateEmployee = currentRow.Cell(4).GetString().Remove(10);
                    string[] hourEmployee = currentRow.Cell(6).GetString().Split(',');
                    string currentHourCompare = dateEmployee + " " + "07:06:00";

                    if (DateTime.Parse(dateEmployee) < DateTime.Parse(DateInitial))
                    {
                        DateInitial = dateEmployee;
                    }

                    if (DateTime.Parse(dateEmployee) > DateTime.Parse(DateFinal))
                    {
                        DateFinal = dateEmployee;
                    }

                    if (hourEmployee.Length == 1)
                    {
                        hourEmployee[0] = hourEmployee[0].Replace("31/12/1899", dateEmployee);
                    }
                    else
                    {
                        Array.Sort(hourEmployee);
                        hourEmployee[0] = dateEmployee + " " + hourEmployee[0];
                        hourEmployee[^1] = dateEmployee + " " + hourEmployee[^1];
                    }

                    if (!DicEmployees.TryGetValue(idEmployee, out Employee? employee))
                    {
                        employee = new Employee(idEmployee, nameEmployee);
                        DicEmployees.Add(idEmployee, employee);
                    }

                    if (DateTime.Parse(hourEmployee[0]) >= DateTime.Parse(currentHourCompare))
                    {
                        employee.DelaysIncremente();
                    }

                    employee.AssistancesIncremente();
                    employee.isWorkMonday = (int)DateTime.Parse(dateEmployee).DayOfWeek == 0 || employee.isWorkMonday;
                }
                DicEmployeesFest = DicEmployees.ToDictionary(emp => emp.Key, emp => emp.Value);
                workbook.Dispose();
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
            catch (OpenXmlPackageException openXmlEx)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"Ocurrio un error inesperado con la libreria CloseXML al intentar abrir el archivo, por favor notificar al departamento de sistemas.\n Error: {openXmlEx.Message}", "Error de CloseXML");
            }
            catch (ArgumentNullException argNullEx)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"Ocurrio un error inesperado, existe una referencia nula, por favor notificar al departamento de sistemas.\n Error: {argNullEx.Message}", "Error de ArgumentException");
            }
            catch (IOException IOEx)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"Actualmente se esta utilizando el archivo, por favor cierrelo y vuelva a intentar cargarlo.\n Error: {IOEx.Message}", "Advertencia Archivo Ocupado");
            }
        }

        private void UploadDataAssistance()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            if (FilePathAssistance is null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("No es posible encontrar el archivo, asegurese de haberlo seleccionado.", "Archivo no encontrado");
                return;
            }

            if (FilePathBiotimer is null || DateInitial is null || DateFinal is null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("Antes de cargar el archivo de Asistencia, por favor suba el archivo de Biotimer.", "Archivo no encontrado");
                txtFileNameAssistanceRoute.Text = "";
                return;
            }

            foreach (var employee in DicEmployees)
            {
                employee.Value.DicRouteDate.Clear();
                employee.Value.Bulk = 0;
            }

            try
            {
                XLWorkbook workbook = new(FilePathAssistance);
                IXLWorksheet sheet = workbook.Worksheet(1);

                var lastRow = sheet.LastRowUsed().RangeAddress.LastAddress.RowNumber;

                string connectionString = "Data Source=DBMSATUXTLA;" +"Initial Catalog=BDSIVE;" + "User id=usrcomersat;" + "Password=Soporte2024;Trust Server Certificate=true";

                using SqlConnection DBConnection = new(connectionString);

                DBConnection.Open();
                string sqlQuery = "select ruta.cverut as cverut, venta.fecalt as fecalt, sum(detventa.canven) as bultos from tsive035 as venta inner  join tsive037 as detventa  on venta.cvevca = detventa.cvevca  inner join tsive041 as liquida on venta.cverup = liquida.cverup  inner join  tsive003 as ruta on liquida.cverut = ruta.cverut  where  liquida.fecrup between @DateInitial and @DateFinal and ruta.cvempr in (1,3) and venta.estvca  = 1   group by  ruta.cverut , venta.fecalt order by venta.fecalt asc;";
                var registers = DBConnection.Query(sqlQuery, new { DateInitial = DateTime.Parse(DateInitial), DateFinal = DateTime.Parse(DateFinal) });

                for (int i = 2; i <= lastRow; i++)
                {
                    IXLRow currentRow = sheet.Row(i);
                    sheet.Cells().Style.Fill.BackgroundColor = XLColor.White;

                    string currentDateInsert = currentRow.Cell(1).GetString().Remove(10);
                    int currentEmployeeInsert = currentRow.Cell(2).GetValue<int>();
                    int currentRouteInsert = currentRow.Cell(3).GetValue<int>();
                    string currentIsDriver = currentRow.Cell(5).GetString().Trim().ToLower();

                    if (DicEmployees.ContainsKey(currentEmployeeInsert))
                    {
                        DicEmployees[currentEmployeeInsert].DicRouteDate.Add(currentDateInsert, currentRouteInsert);
                        var rowDatabase = registers.Where(row => row.fecalt == DateTime.Parse(currentDateInsert) && row.cverut == currentRouteInsert);
                        if (!rowDatabase.IsNullOrEmpty())
                        {
                            DicEmployees[currentEmployeeInsert].Bulk += rowDatabase.First().bultos;
                        }
                        DicEmployees[currentEmployeeInsert].isDriver = currentIsDriver.Equals("si");
                    }
                }
                workbook.Dispose();
                DBConnection.Close();
                DBConnection.Dispose();
                DicEmployees = DicEmployees.Where(emp => emp.Value.DicRouteDate.IsNullOrEmpty() == false).ToDictionary(emp => emp.Key, emp => emp.Value);
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
            catch (OpenXmlPackageException openXmlEx)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"Ocurrio un error inesperado con la libreria CloseXML al intentar abrir el archivo, por favor notificar al departamento de sistemas.\n Error: {openXmlEx.Message}", "Error de CloseXML");
            }
            catch (ArgumentNullException argNullEx)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"Ocurrio un error inesperado, existe una referencia nula, por favor notificar al departamento de sistemas.\n Error: {argNullEx.Message}", "Error de ArgumentException");
            }
        }

        private void UploadDataAssistanceFest()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            if (FilePathAssistanceFest is null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("No es posible encontrar el archivo, asegurese de haberlo seleccionado.", "Archivo no encontrado");
                return;
            }

            if (FilePathBiotimer is null || DateInitial is null || DateFinal is null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show("Antes de cargar el archivo de Asistencia, por favor suba el archivo de Biotimer.", "Archivo no encontrado");
                txtFileNameFestRoute.Text = "";
                return;
            }

            foreach (var employee in DicEmployeesFest)
            {
                employee.Value.DicRouteDate.Clear();
                employee.Value.Bulk = 0;
            }

            try
            {
                XLWorkbook workbook = new(FilePathAssistanceFest);
                IXLWorksheet sheet = workbook.Worksheet(1);

                var lastRow = sheet.LastRowUsed().RangeAddress.LastAddress.RowNumber;

                string connectionString = "Data Source=DBMSATUXTLA;" + "Initial Catalog=BDSIVE;" + "User id=usrcomersat;" + "Password=Soporte2024;Trust Server Certificate=true";

                using SqlConnection DBConnection = new(connectionString);

                DBConnection.Open();
                string sqlQuery = "select liquida.cverut as cverut , venta.fecalt as fecalt , sum(detventa.canven) as bultos , sum(detventa.impven) as dinero  from tsive035 as venta inner  join tsive037 as detventa on venta.cvevca = detventa.cvevca  inner join tsive041 as liquida on venta.cverup = liquida.cverup where  liquida.fecrup between  @DateInitial  and  @DateFinal   and  liquida.cverut in (23,24) and venta.estvca  = 1  and detventa.tmoven!='B'    group by  liquida.cverut , venta.fecalt  order by venta.fecalt asc;";
                var registers = DBConnection.Query(sqlQuery, new { DateInitial = DateTime.Parse(DateInitial), DateFinal = DateTime.Parse(DateFinal) });

                for (int i = 1; i <= lastRow; i++)
                {
                    IXLRow currentRow = sheet.Row(i);
                    sheet.Cells().Style.Fill.BackgroundColor = XLColor.White;

                    string currentDateInsert = currentRow.Cell(1).GetString().Remove(10);
                    int currentEmployeeInsert = currentRow.Cell(2).GetValue<int>();
                    int currentRouteInsert = currentRow.Cell(3).GetValue<int>();
                    string currentIsDriver = currentRow.Cell(5).GetString().Trim().ToLower();

                    if (DicEmployeesFest.ContainsKey(currentEmployeeInsert))
                    {
                        DicEmployeesFest[currentEmployeeInsert].DicRouteDate.Add(currentDateInsert, currentRouteInsert);
                        var rowDatabase = registers.Where(row => row.fecalt == DateTime.Parse(currentDateInsert) && row.cverut == currentRouteInsert);
                        DicEmployeesFest[currentEmployeeInsert].isDriver = currentIsDriver.Equals("si");
                        if (!rowDatabase.IsNullOrEmpty())
                        {
                            if (DicEmployeesFest[currentEmployeeInsert].isDriver)
                            {
                                DicEmployeesFest[currentEmployeeInsert].Money += rowDatabase.First().dinero;
                            }
                            else
                            {
                                DicEmployeesFest[currentEmployeeInsert].Bulk += rowDatabase.First().bultos;
                            }
                        }
                    }
                }
                workbook.Dispose();
                DBConnection.Close();
                DBConnection.Dispose();
                DicEmployeesFest = DicEmployeesFest.Where(emp => emp.Value.DicRouteDate.IsNullOrEmpty() == false).ToDictionary(emp => emp.Key, emp => emp.Value);
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
            catch (OpenXmlPackageException openXmlEx)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"Ocurrio un error inesperado con la libreria CloseXML al intentar abrir el archivo, por favor notificar al departamento de sistemas.\n Error: {openXmlEx.Message}", "Error de CloseXML");
            }
            catch (ArgumentNullException argNullEx)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                MessageBox.Show($"Ocurrio un error inesperado, existe una referencia nula, por favor notificar al departamento de sistemas.\n Error: {argNullEx.Message}", "Error de ArgumentException");
            }
        }

        private void OpenReport(string path)
        {
            try
            {
                var info = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    FileName = path,
                    WindowStyle = ProcessWindowStyle.Maximized
                };
                Process.Start(info);

            }
            catch (Exception ex)
            {

                MessageBox.Show("Hubo al ejecutar el reporte" + ex, "Error!!", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void txtSalaryDriver_TextChanged(Object sender, TextChangedEventArgs e)
        {
            if (txtSalaryDriver.Text != "")
            {
                txtSalaryDriverPlaceholder.Visibility = Visibility.Hidden;
            }
            else
            {
                txtSalaryDriverPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void txtSalaryAux_TextChanged(Object sender, TextChangedEventArgs e)
        {
            if (txtSalaryAux.Text != "")
            {
                txtSalaryAuxPlaceholder.Visibility = Visibility.Hidden;
            }
            else
            {
                txtSalaryAuxPlaceholder.Visibility = Visibility.Visible;
            }
        }
    }
}
