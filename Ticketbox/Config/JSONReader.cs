using Newtonsoft.Json;

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
