using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using ReportingAssistance.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
        }

        private String FilePathBiotimer;
        private String FilePathAssistance;
        private string DateInitial;
        private string DateFinal;
        private readonly int DaysWorked = 6;
        private Dictionary<int, Employee> DicEmployees = new();
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

        private void btnRegenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (FilePathBiotimer is null || FilePathAssistance is null)
            {
                MessageBox.Show("Hace flata que cargue algun archivo, ya sea el arhivo de biotimer o el de asistencia, favor de verificar", "Falta cargar algun archivo");
                return;
            }

            if (!Directory.Exists(PathDir))
            {
                Directory.CreateDirectory(PathDir);
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Asistencias - Ventas");
                worksheet.Cell("A1").Value = $"Reporte de Asistencias de personal de ruta del dia {DateInitial} al {DateFinal}";
                worksheet.Range("A1:F1").Row(1).Merge();
                worksheet.Cell("A2").Value = "ID Empleado";
                worksheet.Cell("B2").Value = "Nombre";
                worksheet.Cell("C2").Value = "Dias trabajados";
                worksheet.Cell("D2").Value = "Retrasos";
                worksheet.Cell("E2").Value = "Bono puntualidad";
                worksheet.Cell("F2").Value = "Bultos";
                worksheet.Cell("G2").Value = "Bono venta";

                int row = 3;

                foreach (var employee in DicEmployees)
                {
                    worksheet.Cell("A" + row).Value = employee.Value.Id;
                    worksheet.Cell("B" + row).Value = employee.Value.Name;
                    worksheet.Cell("C" + row).Value = employee.Value.Assistance;
                    worksheet.Cell("D" + row).Value = employee.Value.Delays;
                    if (employee.Value.Assistance < DaysWorked || employee.Value.Delays >= 2)
                    {
                        worksheet.Cell("E" + row).Value = 0;
                    }
                    else
                    {
                        worksheet.Cell("E" + row).Value = 7 * 50;
                    }

                    worksheet.Cell("F" + row).Value = employee.Value.Bulk;

                    decimal commission = (decimal)(employee.Value.Bulk * .15);

                    if (commission >= 300)
                    {
                        worksheet.Cell("G" + row).Value = 300;
                    }
                    else if (commission <= 150)
                    {
                        worksheet.Cell("G" + row).Value = 150;
                    }
                    else
                    {
                        worksheet.Cell("G" + row).Value = commission;
                    }

                    row++;
                }

                worksheet.Column(1).AdjustToContents();
                worksheet.Column(2).AdjustToContents();
                worksheet.Column(3).AdjustToContents();
                worksheet.Column(4).AdjustToContents();
                worksheet.Column(5).AdjustToContents();
                worksheet.Column(6).AdjustToContents();
                worksheet.Column(7).AdjustToContents();

                workbook.SaveAs(PathDir + $"Reporte Asistencia {DateTime.Now:yyyy-MM-dd HH.mm.ss}.xlsx");
                workbook.Dispose();
            }
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
                    }
                }
            }
            return fileExist;
        }

        private void UploadDataBiotimer()
        {
            if (FilePathBiotimer is null)
            {
                MessageBox.Show("No es posible encontrar el archivo, asegurese de haberlo seleccionado.", "Archivo no encontrado");
                return;
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
                }
                workbook.Dispose();
            }
            catch (OpenXmlPackageException openXmlEx)
            {
                MessageBox.Show($"Ocurrio un error inesperado con la libreria CloseXML al intentar abrir el archivo, por favor notificar al departamento de sistemas.\n Error: {openXmlEx.Message}", "Error de CloseXML");
            }
            catch (ArgumentNullException argNullEx)
            {
                MessageBox.Show($"Ocurrio un error inesperado, existe una referencia nula, por favor notificar al departamento de sistemas.\n Error: {argNullEx.Message}", "Error de ArgumentException");
            }
            catch (IOException IOEx)
            {
                MessageBox.Show($"Actualmente se esta utilizando el archivo, por favor cierrelo y vuelva a intentar cargarlo.\n Error: {IOEx.Message}", "Advertencia Archivo Ocupado");
            }
        }

        private void UploadDataAssistance()
        {
            if (FilePathAssistance is null)
            {
                MessageBox.Show("No es posible encontrar el archivo, asegurese de haberlo seleccionado.", "Archivo no encontrado");
                return;
            }

            try
            {
                XLWorkbook workbook = new(FilePathAssistance);
                IXLWorksheet sheet = workbook.Worksheet(1);

                var lastRow = sheet.LastRowUsed().RangeAddress.LastAddress.RowNumber;

                string connectionString = "Server=SOPORTETI\\SQLEXPRESS;Database=employees ;Trusted_Connection=SSPI;MultipleActiveResultSets=true;Trust Server Certificate=true";

                using SqlConnection DBConnection = new(connectionString);

                DBConnection.Open();
                string sqlQuery = "SELECT [cverut],[fecalt],[venta] FROM [employees].[dbo].[bulktoroute] WHERE (fecalt BETWEEN @DateInitial AND @DateFinal)";
                var registers = DBConnection.Query(sqlQuery, new { DateInitial = DateTime.Parse(DateInitial), DateFinal = DateTime.Parse(DateFinal) });

                for (int i = 1; i <= lastRow; i++)
                {
                    IXLRow currentRow = sheet.Row(i);

                    string currentDateInsert = currentRow.Cell(1).GetString().Remove(10);
                    int currentEmployeeInsert = currentRow.Cell(2).GetValue<int>();
                    int currentRouteInsert = currentRow.Cell(3).GetValue<int>();

                    if (DicEmployees.ContainsKey(currentEmployeeInsert))
                    {
                        DicEmployees[currentEmployeeInsert].DicRouteDate.Add(currentDateInsert, currentRouteInsert);
                        var rowDatabase = registers.Where(row => row.fecalt == DateTime.Parse(currentDateInsert) && row.cverut == currentRouteInsert);
                        if (!rowDatabase.IsNullOrEmpty())
                        {
                            DicEmployees[currentEmployeeInsert].Bulk += rowDatabase.First().venta;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Se encontro al empleado {currentEmployeeInsert}, quien no aparece en el archivo de BioTimer, no sera contempleado para la generacion del archivo final, por favor darlo de alta en el checador.", "Empleado No Encntrado");
                    }
                }
                workbook.Dispose();
                DBConnection.Close();
                DBConnection.Dispose();
                DicEmployees = DicEmployees.Where(emp => emp.Value.DicRouteDate.IsNullOrEmpty() == false).ToDictionary(emp => emp.Key, emp => emp.Value);
            }
            catch (OpenXmlPackageException openXmlEx)
            {
                MessageBox.Show($"Ocurrio un error inesperado con la libreria CloseXML al intentar abrir el archivo, por favor notificar al departamento de sistemas.\n Error: {openXmlEx.Message}", "Error de CloseXML");
            }
            catch (ArgumentNullException argNullEx)
            {
                MessageBox.Show($"Ocurrio un error inesperado, existe una referencia nula, por favor notificar al departamento de sistemas.\n Error: {argNullEx.Message}", "Error de ArgumentException");
            }
        }
    }
}
