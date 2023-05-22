using System;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using System.Device.Location;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WinSvcWMPC
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer = new Timer();
        private GeoCoordinateWatcher watcher;

        private bool sendinf = false;

        public Service1()
        {
            InitializeComponent();
        }

        // 1 000  miisegundos = 1 segundo 
        // 1 minuto = 60 segundos
        
        protected override void OnStart(string[] args)
        {
            try
            {
                Config obj = Config.GetConfig();
                WriteToFile("Serviço iniciou em " + DateTime.Now.ToLocalTime());
                timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
                timer.Interval = obj.Intervalwinsvc;
                timer.Enabled = true;

                OnGetLoc();
            }
            catch (Exception e)
            {
                WriteToFile("OnStart - Reinicie o serviço ou verifique o código do serviço para solucionar o erro: "+e.Message);
            }
        }

        protected override void OnStop()
        {
            WriteToFile("Servico parado em " + DateTime.Now.ToLocalTime());
        }

        public void WriteToFile(string Message)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";

                if (!File.Exists(filepath))
                {
                    // Create a file to write to.   
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceWriteToFileErrorLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";

                    if (!File.Exists(filepath))
                    {
                        // Create a file to write to.   
                        using (StreamWriter sw = File.CreateText(filepath))
                        {
                            var msg = string.Format("{0}. {1}",ex.Message,DateTime.Now.ToLocalTime());
                            sw.WriteLine(msg);
                        }
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(filepath))
                        {
                            var msg = string.Format("{0}. {1}", ex.Message, DateTime.Now.ToLocalTime());
                            sw.WriteLine(msg);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            OnGetLoc();
        }

        private void OnGetLoc()
        {
            sendinf = false;

            WriteToFile("Localização do PC em " + DateTime.Now.ToLocalTime());

            GetLocationEvent();
        }


        public void GetLocationEvent()
        {
            this.watcher = new GeoCoordinateWatcher();
            this.watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);

            bool started = this.watcher.TryStart(false,TimeSpan.FromMilliseconds(5000));

            if (!started)
            {
                WriteToFile("GeoCoordinateWatcher timed out on start.");
            }            
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            try
            {
                if (!sendinf)
                {
                    sendinf = true;
                    WriteToFile(string.Format("GeoCoordinateWatcher - (Latitude,Logintude): {0}, {1}", e.Position.Location.Latitude.ToString().Replace(",", "."), e.Position.Location.Longitude.ToString().Replace(",", ".")));
                    Task.Run(() => SendInfoGeoLocPc(e.Position.Location.Latitude, e.Position.Location.Longitude)).Wait();
                }

            }
            catch (Exception ex)
            {
                WriteToFile("watcher_PositionChanged: " + ex.Message);
            }
        }

        private void SendEmail(double lat, double lon)
        {
            try
            {
                Config obj = Config.GetConfig();

                StringBuilder str = new StringBuilder();
                str.AppendLine("Olá " + obj.EmailRem + ",<br>");
                str.AppendLine(string.Format("Dados da última localização do PC {0}<br>", obj.PcName));
                str.AppendLine(string.Format("(Latitude, Longitude) : ({0}, {1})<br>", lat.ToString().Replace(",", "."), lon.ToString().Replace(",", ".")));
                str.AppendLine(string.Format("Data: {0}<br>", DateTime.Now.ToLocalTime()));
                str.AppendLine("Favor não responder esse email");

                new EmailUtil().EnviarViaSMTP(string.Format("Localização PC", obj.PcName), str.ToString(), obj.EmailEmi, obj.Port,
                                                      obj.Pass, "smtp.gmail.com", new List<string>() { obj.EmailRem }, null, null, null, true);
            }
            catch (Exception e)
            {
                throw new Exception("SendEmail: " + e.Message);
            }
        }

        private async Task SendInfoGeoLocPc(double lat, double lon)
        {
            Config obj = Config.GetConfig();

            // Tentar com urlLocal
            bool result = await SendResultInfoGeoLocPc(lat, lon, obj.UriLocal, obj.PcName);

            if(!result)
                result = await SendResultInfoGeoLocPc(lat, lon, obj.UriExt, obj.PcName);

            if (!result)
                SendEmail(lat, lon);
        }

        private async Task<bool> SendResultInfoGeoLocPc(double lat, double lon, string Uri, string pcname)
        {
            try
            {
                var data = new { pcname = pcname, latitude = lat.ToString().Replace(",","."), longitude = lon.ToString().Replace(",",".") };

                String json = string.Empty;
                using (var c = new HttpClient())
                {
                    var result = await c.PostAsJsonAsync(Uri + "GeoLocPC", data);

                    if (!result.IsSuccessStatusCode)
                    {
                        if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                            throw new Exception(result.Content.ReadAsStringAsync().Result);
                        else if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
                            throw new Exception(result.Content.ReadAsStringAsync().Result);
                        else
                            throw new Exception(string.Format("Error - StatusCode {0}. {1}", result.StatusCode, result.Content.ReadAsStringAsync().Result));
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                WriteToFile(string.Format("SendResultInfoGeoLocPc: {0}. Uri: {1}", e.Message,Uri));

                return false;
            }
            
        }
    }
}
