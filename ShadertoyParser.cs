using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

class ShadertoyParser{
    const string apiKey = "Nt8lR1"; // Replace with your API key

    public static ShaderDetails? fetchShader(string id){
        using (HttpClient client = new HttpClient()){
            try{
                // Query shaders
                string shaderUrl = "https://www.shadertoy.com/api/v1/shaders/" + id + "?key=" + apiKey;
                string shaderResponse = client.GetStringAsync(shaderUrl).Result;
				
                // Parse shader details JSON
                using JsonDocument shaderDoc = JsonDocument.Parse(shaderResponse);
                var root = shaderDoc.RootElement;

                string name = root.GetProperty("Shader").GetProperty("info").GetProperty("name").GetString() ?? "Unnamed Shader";
                string code = root.GetProperty("Shader").GetProperty("renderpass")[0].GetProperty("code").GetString() ?? "No Code Found";
                string author = root.GetProperty("Shader").GetProperty("info").GetProperty("username").GetString() ?? "No author found";
                string link = "https://www.shadertoy.com/view/" + id;

                return new ShaderDetails(name, code, author, link);
            }catch (Exception e){
                Console.WriteLine(e);
				return null;
            }
        }
    }
}

struct ShaderDetails{
	public string name;
	public string code;
	public string author;
	public string link;
	
	public ShaderDetails(string n, string c, string a, string l){
		name = n;
		code = c;
		author = a;
		link = l;
	}
}
