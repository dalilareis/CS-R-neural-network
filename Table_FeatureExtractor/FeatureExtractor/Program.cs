using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;

namespace FeatureExtractor
{
    static class Program
    {
 //------------------------------------ Auxiliary Methods (file handling) ----------------------------------------------------------------------

        public static List<List<MouseEvents>> ReadFiles (string paths)
        {
            
            List<List<MouseEvents>> listaFiles = new List<List<MouseEvents>>();

            string[] files = paths.Split(new string[] { "\r\n"}, StringSplitOptions.RemoveEmptyEntries);

            foreach (string filename in files)
            {
                try
                {
                    using (StreamReader texto = new StreamReader(filename))
                    {
                        List<MouseEvents> eventos = new List<MouseEvents>();
                        string line;
                        int i = 0;
                        while ((line = texto.ReadLine()) != null)
                        {
                            if (line == String.Empty) //Fixed error: index was outside the bounds of the array
                                continue;
                            string[] colunas = line.Split(','); 
                            if (colunas[0] != "MW") //Mouse Wheel not used in any feature and has extra column
                            {
                                eventos.Add(new MouseEvents(Path.GetFileNameWithoutExtension(filename), colunas[0], Int64.Parse(colunas[1]), int.Parse(colunas[2]), int.Parse(colunas[3])));
                                i++;
                            }
                        }
                        listaFiles.Add(eventos);                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not open sample file: " + filename);
                    Console.WriteLine(e.Message);
                }                
            }
            return listaFiles;
        }

        public static List<KeyValuePair<string, double>> ReadCSV (string path)
        {
            List<KeyValuePair<string, double>> files = new List<KeyValuePair<string, double>>();
            
                using (StreamReader sr = new StreamReader(path))
                {
                    string header = sr.ReadLine(); // Skip header
                    string line;
                    int i = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] colunas = line.Split(',');
                        string name = "mouse_" + colunas[0] + "_" + colunas[1];
                        double nota = Double.Parse(colunas[2].Replace('.', ','));
                        files.Add(new KeyValuePair<string, double>(name, nota));
                        i++;                   
                    }
                }
            return files;
        }

        public static DataTable CreateTable()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("File_ID", typeof(string));
            dt.Columns.Add("Num_Clicks", typeof(int));
            dt.Columns.Add("CD", typeof(double));
            //dt.Columns.Add("StD_CD", typeof(double));
            dt.Columns.Add("Num_Double_Clicks", typeof(int));
            dt.Columns.Add("TDC", typeof(double));
            dt.Columns.Add("TBC", typeof(double));
            //dt.Columns.Add("StD_TBC", typeof(double));
            dt.Columns.Add("MV", typeof(double));
            //dt.Columns.Add("StD_MV", typeof(double));
            dt.Columns.Add("MA", typeof(double));
            //dt.Columns.Add("StD_MA", typeof(double));
            dt.Columns.Add("DBC", typeof(double));
            //dt.Columns.Add("StD_DBC", typeof(double));
            dt.Columns.Add("ED", typeof(double));
            //dt.Columns.Add("StD_ED", typeof(double));
            dt.Columns.Add("AED", typeof(double));
            //dt.Columns.Add("StD_AED", typeof(double));
            dt.Columns.Add("DMSL", typeof(double));
            //dt.Columns.Add("StD_DMSL", typeof(double));
            dt.Columns.Add("ADMSL", typeof(double));
            //dt.Columns.Add("StD_ADMSL", typeof(double));
            dt.Columns.Add("ASA", typeof(double));
            //dt.Columns.Add("StD_ASA", typeof(double));
            dt.Columns.Add("SSA", typeof(double));
            //dt.Columns.Add("StD_SSA", typeof(double));
            dt.Columns.Add("Grade", typeof(double));

            return dt;
        }

        public static void Write2CSV(DataTable dt, TextWriter writer, bool includeHeaders)
        {
            if (includeHeaders)
            {
                IEnumerable<String> headerValues = dt.Columns.OfType<DataColumn>()
                                                   .Select(column => column.ColumnName);

                writer.WriteLine(String.Join(";", headerValues));
            }

            IEnumerable<String> items = null;
            foreach (DataRow row in dt.Rows)
            {
                items = row.ItemArray.Select(field => field?.ToString() ?? String.Empty);
                writer.WriteLine(String.Join(";", items));
            }
            writer.Flush();
        }

//------------------------------------------ Statistics Calculation -----------------------------------------------------------------------------

        public static double StdD_Int (List<int> dados)
        {
            if (dados.Count == 0)
                return 0;
            if (dados.Count == 1)
                return 0;

            float mean = (float)dados.Average();
            float sumSquaresOfDifferences = (float)dados.Select(val => (val - mean) * (val - mean)).Sum();
            double sd = Math.Sqrt(sumSquaresOfDifferences / (dados.Count - 1));

            return sd;
        }

        public static double Avg_Int (List<int> dados)
        {
            if (dados.Count == 0)
                return 0;

            double mean = dados.Average();
            return mean;
        }

        public static double StdD_Doub (List<double> dados)
        {
            if (dados.Count == 0)
                return 0;
            if (dados.Count == 1)
                return 0;

            float mean = (float)dados.Average();
            float sumSquaresOfDifferences = (float)dados.Select(val => (val - mean) * (val - mean)).Sum();
            double sd = Math.Sqrt(sumSquaresOfDifferences / (dados.Count - 1));

            return sd;
        }

        public static double Avg_Doub(List<double> dados)
        {
            if (dados.Count == 0)
                return 0;

            double mean = dados.Average();

            return mean;
        }

 //-------------------------------------- Features (x12) Extraction -----------------------------------------------------------------------------

        public static List<int> ClickDuration (List<MouseEvents> dados)
        {
            int timeDif;
            List<long> start = new List<long>();
            List<long> end = new List<long>();
            List<int> cd = new List<int>();

            for (int i = 0; i < dados.Count(); i++)
            {
                if (dados[i].getType() == "MDL" || dados[i].getType() == "MDR")
                {
                    start.Add(dados[i].getTimestamp());
                }
                else if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    end.Add(dados[i].getTimestamp());
                }
            }
            for (int j = 0; j < start.Count(); j++)
            {
                timeDif = Convert.ToInt32((end[j] - start[j]) / TimeSpan.TicksPerMillisecond);
                cd.Add(timeDif);
            }

            return cd;
        }

        public static List<int> TimeBtwClicks (List<MouseEvents> dados)
        {
            int timeDif;
            long start, end;
            List<int> tbc = new List<int>();

            for (int i = 0; i < dados.Count - 1; i++) //(-1) To prevent out of bounds index if MUL is the last event (will not count)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    start = dados[i].getTimestamp();
                    int j = 1;
                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                            j++;
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        end = dados[i + j].getTimestamp();
                        timeDif = Convert.ToInt32((end - start) / TimeSpan.TicksPerMillisecond);
                        if (timeDif > 200)
                            tbc.Add(timeDif);
                    }                    
                }
            }
            return tbc;
        }

        public static List<int> DoubleClick (List<MouseEvents> dados)
        {
            int timeDif;
            long start, end;
            List<int> dc = new List<int>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    start = dados[i].getTimestamp();
                    int j = 1;
                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                            j++;
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        end = dados[i + j].getTimestamp();
                        timeDif = Convert.ToInt32((end - start) / TimeSpan.TicksPerMillisecond);
                        if (timeDif <= 200)
                            dc.Add(timeDif);
                    }
                }
            }
            return dc;
        }

        public static List<double> DistBtwClicks (List<MouseEvents> dados)
        {
            int xi, yi, xf, yf;
            double dist;
            List<double> dbc = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MDR")
                {
                    int j = 1;                   
                    dist = 0;
                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            j++;
                        }
                        else
                            break;                        
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            dbc.Add(dist);
                        }                            
                    }
                }
            }                
            return dbc;
        }

        public static List<double> ExcessDist (List<MouseEvents> dados)
        {
            int xi, yi, xf, yf, startX, startY, endX, endY;
            double dist;
            double shortest_dist = 0;
            List<double> ed = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    dist = 0;
                    startX = dados[i].getX();
                    startY = dados[i].getY();

                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));

                            endX = dados[i + j].getX();
                            endY = dados[i + j].getY();
                            shortest_dist = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                            ed.Add(dist - shortest_dist);
                        }                            
                    }
                }
            }
            return ed;
        }

        public static List<double> AvgED (List<MouseEvents> dados)
        {
            int xi, yi, xf, yf, startX, startY, endX, endY;
            double dist;
            double shortest_dist = 0;
            List<double> avgED = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    dist = 0;
                    startX = dados[i].getX();
                    startY = dados[i].getY();

                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));

                            endX = dados[i + j].getX();
                            endY = dados[i + j].getY();
                            shortest_dist = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                            if (shortest_dist != 0) // Safeguard for infinity value
                                avgED.Add(dist / shortest_dist);
                            else
                                avgED.Add(0);
                        }                           
                    }
                }
            }
            return avgED;
        }

        public static List<double> Velocity (List<MouseEvents> dados)
        {
            int xi, yi, xf, yf, timeDif;
            long start, end;
            double dist;
            List<double> vel = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    dist = 0;
                    start = dados[i].getTimestamp();

                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            end = dados[i + j].getTimestamp();
                            timeDif = Convert.ToInt32((end - start) / TimeSpan.TicksPerMillisecond);
                            vel.Add(dist / timeDif);
                        }                            
                    }
                }
            }
            return vel;
        }

        public static List<double> Acceleration (List<MouseEvents> dados)
        {
            int xi, yi, xf, yf, timeDif;
            long start, end;
            double dist;
            List<double> acc = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    dist = 0;
                    start = dados[i].getTimestamp();

                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            xf = dados[i + j].getX();
                            yf = dados[i + j].getY();
                            dist += Math.Sqrt(Math.Pow(xf - xi, 2) + Math.Pow(yf - yi, 2));
                            end = dados[i + j].getTimestamp();
                            timeDif = Convert.ToInt32((end - start) / TimeSpan.TicksPerMillisecond);
                            acc.Add(dist / timeDif / timeDif);
                        }                           
                    }
                }
            }
            return acc;
        }

        public static List<double> DistStraightLine (List<MouseEvents> dados)
        {
            List<int> x = new List<int>();
            List<int> y = new List<int>();
            int startX, startY, endX, endY;
            double dist, test, predist;
            List<double> dsl = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    dist = 0;
                    startX = dados[i].getX();
                    startY = dados[i].getY();

                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            x.Add(dados[i].getX());
                            y.Add(dados[i].getY());
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                        {
                            endX = dados[i + j].getX();
                            endY = dados[i + j].getY();
                            test = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));

                            if (test != 0)  // Safeguard for infinity value
                            {
                                for (int k = 0; k < x.Count; k++)
                                {
                                    predist = (endX - startX) * (startY - y[k]) - (startX - x[k]) * (endY - startY);
                                    dist += (Math.Abs(predist)) / test;
                                }
                            }
                            dsl.Add(dist);
                        }
                           
                    }
                }
            }
            return dsl;
        }

        public static List<double> AvgDistSL (List<MouseEvents> dados)
        {
            List<int> x = new List<int>();
            List<int> y = new List<int>();
            int startX, startY, endX, endY;
            double dist, test, predist;
            List<double> adsl = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    dist = 0;
                    startX = dados[i].getX();
                    startY = dados[i].getY();

                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            x.Add(dados[i].getX());
                            y.Add(dados[i].getY());
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                        {
                            endX = dados[i + j].getX();
                            endY = dados[i + j].getY();
                            test = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));

                            if (test != 0)  // Safeguard for infinity value
                            {
                                for (int k = 0; k < x.Count; k++)
                                {
                                    predist = (endX - startX) * (startY - y[k]) - (startX - x[k]) * (endY - startY);
                                    dist += (Math.Abs(predist)) / test;
                                }
                            }
                            adsl.Add(dist / x.Count);
                        }                            
                    }
                }
            }
            return adsl;
        }

        public static List<double> SumAnglesAbs (List<MouseEvents> dados)
        {
            int centerX, centerY, xi, yi, xf, yf;
            double angle;
            List<double> sumAngles = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    angle = 0;
                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            centerX = dados[i + j].getX();
                            centerY = dados[i + j].getY();
                            xf = dados[i + j + 1].getX();
                            yf = dados[i + j + 1].getY();
                            angle += Math.Abs((Math.Atan2(yf - centerY, xf - centerX) - 
                                    Math.Atan2(yi - centerY, xi - centerX)) * (180 / Math.PI));
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1) // To exclude Double Click Events or No movement between clicks 
                            sumAngles.Add(angle);
                    }
                }
            }
            return sumAngles;
        }

        public static List<double> SumAnglesSign (List<MouseEvents> dados)
        {
            int centerX, centerY, xi, yi, xf, yf;
            double angle;
            List<double> sumAngles = new List<double>();

            for (int i = 0; i < dados.Count - 1; i++)
            {
                if (dados[i].getType() == "MUL" || dados[i].getType() == "MUR")
                {
                    int j = 1;
                    angle = 0;
                    while (dados[i + j].getType() != "MDL" && dados[i + j].getType() != "MDR")
                    {
                        if ((i + j) < dados.Count - 1)
                        {
                            xi = dados[i + j - 1].getX();
                            yi = dados[i + j - 1].getY();
                            centerX = dados[i + j].getX();
                            centerY = dados[i + j].getY();
                            xf = dados[i + j + 1].getX();
                            yf = dados[i + j + 1].getY();
                            angle += (Math.Atan2(yf - centerY, xf - centerX) - Math.Atan2(yi - centerY, xi - centerX)) * (180 / Math.PI);
                            j++;
                        }
                        else
                            break;
                    }
                    if (dados[i + j].getType() == "MDL" || dados[i + j].getType() == "MDR")
                    {
                        if (j != 1)  // To exclude Double Click Events or No movement between clicks
                            sumAngles.Add(angle);
                    }
                }
            }
            return sumAngles;
        }

        [STAThread]

        

        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            
        }
    }
}
