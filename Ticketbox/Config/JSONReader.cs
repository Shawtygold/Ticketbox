using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ticketbox.Config
{
    internal class JSONReader
    {
        public string Token { get; set; }

        public async Task ReadJsonAsync()
        {
            using StreamReader sr = new ("config.json");
            string json = await sr.ReadToEndAsync();
            JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);

            if (data == null)
                return;

            Token = data.Token;
        }
    }

    internal class JSONStructure
    {
        public string Token { get; set; }
    }
}
