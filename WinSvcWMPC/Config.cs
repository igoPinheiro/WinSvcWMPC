using Newtonsoft.Json;
using System;
using System.IO;

namespace WinSvcWMPC
{
    public class Config
    {
        [JsonProperty("emailemi")]
        public string EmailEmi { get; set; }

        [JsonProperty("emailrem")]
        public string EmailRem { get; set; }

        [JsonProperty("pass")]
        public string Pass { get; set; }

        [JsonProperty("pcname")]
        public string PcName { get; set; }

        [JsonProperty("urilocal")]
        public string UriLocal { get; set; }

        [JsonProperty("uriext")]
        public string UriExt { get; set; }

        /// <summary>
        /// Em milisegundos
        /// </summary>
        [JsonProperty("intervalwinsvc")]
        public long Intervalwinsvc { get; set; }

        [JsonProperty("Port")]
        public int Port { get; set; }


        public static Config GetConfig()
        {
            try
            {
                Config obj;
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\config.json";

                using (StreamReader r = new StreamReader(path))
                {
                    string json = r.ReadToEnd();
                    obj = JsonConvert.DeserializeObject<Config>(json);
                }

                return obj;

            }
            catch (Exception e)
            {
                throw new Exception("GetConfig - " + e.Message);
            }
        }
    }
}
