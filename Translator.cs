using System;
using System.Text;

static class Translator{
	public static string toyToRoom(ShaderDetails d){
		string[] codeLines = d.code.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);

		Dictionary<string, Word> rep = new Dictionary<string, Word>{
			{"iTime", new Word("iTime", "iTime")},
			{"iResolution.z", new Word("1.0", "null")},
			{"iResolution.xy", new Word("iResolution", "null")},
			{"iResolution", new Word("vec3(iResolution, 1.0)", "null")},
			{"iTimeDelta", new Word("(1.0 / iFps)", "iFps")},
			{"iFrameRate", new Word("iFps", "iFps")},
			{"iFrame", new Word("iFrame", "iFrame")},
			{"iMouse.xy", new Word("iTRANSLATOR_TR_Mouse", "iMouse")},
			{"iMouse.zw", new Word("iTRANSLATOR_TR_Mouse", "iMouse")},
			{"iMouse.z", new Word("iTRANSLATOR_TR_Mouse.x", "iMouse")},
			{"iMouse.w", new Word("iTRANSLATOR_TR_Mouse.y", "iMouse")},
			{"iMouse", new Word("vec4(iTRANSLATOR_TR_Mouse, iTRANSLATOR_TR_Mouse)", "iMouse")},
			{"iDate", new Word("vec4(iDate.zyx, iHour.x * 3600.0 + iHour.y * 60.0 + iHour.z)", "iDate")},
			{"iDate.x", new Word("iDate.z", "iDate")},
			{"iDate.y", new Word("(iDate.y - 1.0)", "iDate")},
			{"iDate.z", new Word("iDate.x", "iDate")},
			{"iDate.w", new Word("(iHour.x * 3600.0 + iHour.y * 60.0 + iHour.z)", "iDate")},
			{"iSampleRate", new Word("44100.0", "null")}
		};

		Dictionary<string, Word> rep2 = new Dictionary<string, Word>{
			{"iMouse", new Word("iTRANSLATOR_TR_Mouse", "iMouse")},
			{"iResolution", new Word("iResolution", "iResolution")},
			{"iDate", new Word("iDate", "iDate")},
			{"fragCoord", new Word("TRANSLATOR_TR_fragCoord", "null")},
			{"fragColor", new Word("TRANSLATOR_TR_fragColor", "null")}
		};

		List<string> app = new List<string>();

		for(int i = 0; i < codeLines.Length; i++){
			codeLines[i] = replaceLine(codeLines[i], rep, rep2, app);
		}

		List<string> l = new List<string>();

		l.Add("#version 330 core");
		l.Add("");
		l.Add(getTranslationMessage());
		if(d.link != null){
			l.Add("//Parsed directly from Shadertoy. Author: " + d.author);
			l.Add("//" + d.link);
		}
		l.Add("");
		l.Add("out vec4 fragColor;");
		l.Add("in vec2 fragCoord;");

		if(app.Contains("iTime")){
			l.Add("uniform float iTime;");
		}
		if(app.Contains("iFps")){
			l.Add("uniform float iFps;");
		}
		if(app.Contains("iFrame")){
			l.Add("uniform int iFrame;");
		}
		if(app.Contains("iMouse")){
			l.Add("uniform vec2 iMouse;");
			l.Add("vec2 iTRANSLATOR_TR_Mouse;");
		}
		if(app.Contains("iDate")){
			l.Add("uniform vec3 iDate;");
			l.Add("uniform vec3 iHour;");
		}

		l.Add("uniform vec2 iResolution;");

		l.AddRange(codeLines);

		l.Add("void main(){");
		if(app.Contains("iMouse")){
			l.Add("    iTRANSLATOR_TR_Mouse = (iMouse / 2.0 + 0.5) * iResolution;");
		}
		l.Add("    vec4 TRANSLATOR_TR_fragColor;");
		l.Add("    vec2 TRANSLATOR_TR_fragCoord = (fragCoord / 2.0 + 0.5) * iResolution;");
		l.Add("    mainImage(TRANSLATOR_TR_fragColor, TRANSLATOR_TR_fragCoord);");
		l.Add("    fragColor = TRANSLATOR_TR_fragColor;");
		l.Add("}");

		return string.Join('\n', l.ToArray());
	}

	public static string roomToToy(string code){
		string[] codeLines = code.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);

		Dictionary<string, Word> rep = new Dictionary<string, Word>{
			{"iResolution", new Word("iResolution.xy", "null")},
			{"iFps", new Word("iFrameRate", "null")},
			{"iDate", new Word("iDate.zyx", "null")},
			{"iDate.x", new Word("iDate.z", "null")},
			{"iDate.y", new Word("(iDate.y + 1.0)", "null")},
			{"iDate.z", new Word("iDate.x", "null")},
			{"iHour", new Word("vec3(floor(iDate.w / 3600.0), floor(mod(iDate.w, 3600.0) / 60.0), mod(iDate.w, 60.0))", "null")},
			{"iHour.x", new Word("floor(iDate.w / 3600.0)", "null")},
			{"iHour.y", new Word("floor(mod(iDate.w, 3600.0) / 60.0)", "null")},
			{"iHour.z", new Word("mod(iDate.w, 60.0)", "null")},
			{"main", new Word("mainMethod", "null")}
		};

		Dictionary<string, Word> rep2 = new Dictionary<string, Word>{
			{"iMouse", new Word("iTRANSLATOR_RT_Mouse", "iMouse")},

			{"fragCoord", new Word("TRANSLATOR_RT_fragCoord", "null")},
			{"fragColor", new Word("TRANSLATOR_RT_fragColor", "null")}
		};

		List<string> app = new List<string>();
		
		List<string> l = new List<string>();
		l.AddRange(codeLines);

		for(int i = 0; i < l.Count; i++){
			if(string.IsNullOrEmpty(l[i])){
				continue;
			}
			
			if(l[i].StartsWith("#version") || l[i].StartsWith("@") || (divideLine(l[i]).Length > 0 && (divideLine(l[i])[0] == "uniform" || divideLine(l[i])[0] == "out" || divideLine(l[i])[0] == "in"))){
				l.RemoveAt(i);
				i--;
				continue;
			}
			l[i] = replaceLine(l[i], rep, rep2, app);
		}
		
		codeLines = l.ToArray();

		l = new List<string>();

		l.Add(getTranslationMessage());
		l.Add("");
		l.Add("vec4 TRANSLATOR_RT_fragColor;");
		l.Add("vec2 TRANSLATOR_RT_fragCoord;");

		if(app.Contains("iMouse")){
			l.Add("vec2 iTRANSLATOR_RT_Mouse;");
		}

		l.AddRange(codeLines);

		l.Add("void mainImage(out vec4 fragColor, in vec2 fragCoord){");
		if(app.Contains("iMouse")){
			l.Add("    iTRANSLATOR_RT_Mouse = (iMouse.xy / iResolution.xy) * 2.0 - 1.0;");
		}
		l.Add("    TRANSLATOR_RT_fragCoord = (fragCoord / iResolution.xy) * 2.0 - 1.0;");
		l.Add("    mainMethod();");
		l.Add("    fragColor = TRANSLATOR_RT_fragColor;");
		l.Add("}");

		return string.Join('\n', l.ToArray());
	}
	
	public static string androidToRoom(string code){
		string[] codeLines = code.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);

		Dictionary<string, Word> rep = new Dictionary<string, Word>{
			{"time", new Word("iTime", "iTime")},
			{"backbuffer", new Word("iBackBuffer", "iBackBuffer")},
			{"date", new Word("vec4(iDate.zyx, iHour.x * 3600.0 + iHour.y * 60.0 + iHour.z)", "iDate")},
			{"date.x", new Word("iDate.z", "iDate")},
			{"date.y", new Word("iDate.y", "iDate")},
			{"date.z", new Word("iDate.x", "iDate")},
			{"date.w", new Word("(iHour.x * 3600.0 + iHour.y * 60.0 + iHour.z)", "iDate")},
			{"frame", new Word("iFrame", "iFrame")},
			{"subsecond", new Word("fract(iTime)", "iTime")},
			{"pointers[0]", new Word("iTRANSLATOR_AR_Mouse", "iMouse")},
			{"pointerCount", new Word("1", "null")},
			{"main", new Word("mainMethod", "null")}
		};

		Dictionary<string, Word> rep2 = new Dictionary<string, Word>{
			{"touch", new Word("iTRANSLATOR_AR_Mouse", "iMouse")},
			{"daytime", new Word("iHour", "iDate")},
			{"resolution", new Word("iResolution", "null")},
			{"gl_FragCoord", new Word("TRANSLATOR_AR_fragCoord", "null")},
			{"gl_FragColor", new Word("TRANSLATOR_AR_fragColor", "null")},
		};

		List<string> app = new List<string>();

		List<string> l = new List<string>();
		l.AddRange(codeLines);

		for(int i = 0; i < l.Count; i++){
			if(string.IsNullOrEmpty(l[i])){
				continue;
			}
			
			if(l[i].StartsWith("#version") || (divideLine(l[i]).Length > 0 && (divideLine(l[i])[0] == "uniform" || divideLine(l[i])[0] == "out" || divideLine(l[i])[0] == "in"))){
				l.RemoveAt(i);
				i--;
				continue;
			}
			l[i] = replaceLine(l[i], rep, rep2, app);
		}
		
		codeLines = l.ToArray();

		l = new List<string>();

		l.Add("#version 330 core");
		l.Add("");
		l.Add(getTranslationMessage());
		l.Add("");
		l.Add("out vec4 fragColor;");
		l.Add("in vec2 fragCoord;");
		l.Add("");
		l.Add("vec4 TRANSLATOR_AR_fragColor;");
		l.Add("vec2 TRANSLATOR_AR_fragCoord;");

		if(app.Contains("iTime")){
			l.Add("uniform float iTime;");
		}
		if(app.Contains("iFrame")){
			l.Add("uniform int iFrame;");
		}
		if(app.Contains("iMouse")){
			l.Add("uniform vec2 iMouse;");
			l.Add("vec2 iTRANSLATOR_AR_Mouse;");
		}
		if(app.Contains("iDate")){
			l.Add("uniform vec3 iDate;");
			l.Add("uniform vec3 iHour;");
		}
		if(app.Contains("iBackBuffer")){
			l.Add("uniform sampler2D iBackBuffer;");
		}

		l.Add("uniform vec2 iResolution;");

		l.AddRange(codeLines);

		l.Add("void main(){");
		if(app.Contains("iMouse")){
			l.Add("    iTRANSLATOR_AR_Mouse = (iMouse / 2.0 + 0.5) * iResolution;");
		}
		l.Add("    TRANSLATOR_AR_fragCoord = (fragCoord / 2.0 + 0.5) * iResolution;");
		l.Add("    mainMethod();");
		l.Add("    fragColor = TRANSLATOR_AR_fragColor;");
		l.Add("}");

		return string.Join('\n', l.ToArray());
	}
	
	public static string roomToAndroid(string code){
		string[] codeLines = code.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);

		Dictionary<string, Word> rep = new Dictionary<string, Word>{
			{"iTime", new Word("time", "iTime")},
			{"iBackBuffer", new Word("backbuffer", "iBackBuffer")},
			{"iDate", new Word("date.zyx", "iDate")},
			{"iDate.x", new Word("date.z", "iDate")},
			{"iDate.y", new Word("date.y", "iDate")},
			{"iDate.z", new Word("date.x", "iDate")},
			{"iFrame", new Word("frame", "iFrame")},
			{"main", new Word("mainMethod", "null")}
		};

		Dictionary<string, Word> rep2 = new Dictionary<string, Word>{
			{"iMouse", new Word("iTRANSLATOR_RA_Mouse", "iMouse")},
			{"iHour", new Word("daytime", "iDate")},
			{"iResolution", new Word("resolution", "null")},
			{"fragCoord", new Word("TRANSLATOR_RA_fragCoord", "null")},
		};

		List<string> app = new List<string>();

		List<string> l = new List<string>();
		l.AddRange(codeLines);

		for(int i = 0; i < l.Count; i++){
			if(string.IsNullOrEmpty(l[i])){
				continue;
			}
			
			if(l[i].StartsWith("#version") || l[i].StartsWith("@") || (divideLine(l[i]).Length > 0 && (divideLine(l[i])[0] == "uniform" || divideLine(l[i])[0] == "out" || divideLine(l[i])[0] == "in"))){
				l.RemoveAt(i);
				i--;
				continue;
			}
			l[i] = replaceLine(l[i], rep, rep2, app);
		}
		
		codeLines = l.ToArray();

		l = new List<string>();

		l.Add("#version 300 es");
		l.Add("");
		l.Add(getTranslationMessage());
		l.Add("");
		l.Add("#ifdef GL_FRAGMENT_PRECISION_HIGH");
		l.Add("precision highp float;");
		l.Add("#else");
		l.Add("precision mediump float;");
		l.Add("#endif");
		l.Add("");
		l.Add("out vec4 fragColor;");
		l.Add("");
		l.Add("vec2 TRANSLATOR_RA_fragCoord;");

		if(app.Contains("iTime")){
			l.Add("uniform float time;");
		}
		if(app.Contains("iFrame")){
			l.Add("uniform int frame;");
		}
		if(app.Contains("iMouse")){
			l.Add("uniform vec2 touch;");
			l.Add("vec2 iTRANSLATOR_RA_Mouse;");
		}
		if(app.Contains("iDate")){
			l.Add("uniform vec4 date;");
			l.Add("uniform vec3 daytime;");
		}
		if(app.Contains("iBackBuffer")){
			l.Add("uniform sampler2D backbuffer;");
		}

		l.Add("uniform vec2 resolution;");

		l.AddRange(codeLines);

		l.Add("void main(){");
		if(app.Contains("iMouse")){
			l.Add("    iTRANSLATOR_RA_Mouse = (touch / resolution) * 2.0 - 1.0;");
		}
		l.Add("    TRANSLATOR_RA_fragCoord = (gl_FragCoord.xy / resolution) * 2.0 - 1.0;");
		l.Add("    mainMethod();");
		l.Add("}");

		return string.Join('\n', l.ToArray());
	}
	
	public static string roomToWeb(string code){
		string[] codeLines = code.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
		
		List<string> l = new List<string>();
		l.AddRange(codeLines);
		
		for(int i = 0; i < l.Count; i++){
			if(string.IsNullOrEmpty(l[i])){
				continue;
			}
			
			if(l[i].StartsWith("#version") || l[i].StartsWith("@")){
				l.RemoveAt(i);
				i--;
				continue;
			}
		}
		
		codeLines = l.ToArray();
		
		l = new List<string>();
		
		l.Add("#version 300 es");
		l.Add("");
		l.Add(getTranslationMessage());
		l.Add("");
		l.Add("precision highp float;");
		l.Add("");

		l.AddRange(codeLines);

		return string.Join('\n', l.ToArray());
	}
	
	
	
	
	
	static string replaceLine(string s, Dictionary<string, Word> replacements, Dictionary<string, Word> replacementsDot, List<string> appear){
		StringBuilder result = new StringBuilder();
		StringBuilder currentWord = new StringBuilder();

		for(int i = 0; i < s.Length; i++){
			char ch = s[i];

			// Split words based on spaces or special symbols
			if(char.IsWhiteSpace(ch) || (char.IsPunctuation(ch) && ch != '.' && ch != '_' && ch != '[' && ch != ']') || char.IsSymbol(ch)){
				if(currentWord.Length > 0){
					string word = currentWord.ToString();

					// Replace the word if it exists in the dictionary
					if(replacements.ContainsKey(word)){
						result.Append(replacements[word].replacement);
						appear.Add(replacements[word].appear);
					}else{
						result.Append(word);
					}

					currentWord.Clear();
				}

				// Append the current delimiter (space, punctuation, etc.)
				result.Append(ch);
			}else{
				currentWord.Append(ch);
			}
		}

		// Add the last word if present
		if(currentWord.Length > 0){
			string word = currentWord.ToString();
			if(replacements.ContainsKey(word)){
				result.Append(replacements[word].replacement);
				appear.Add(replacements[word].appear);
			}else{
				result.Append(word);
			}
		}

		//Second pass

		s = result.ToString();

		result = new StringBuilder();
		currentWord = new StringBuilder();

		for(int i = 0; i < s.Length; i++){
			char ch = s[i];

			// Split words based on spaces or special symbols
			if(char.IsWhiteSpace(ch) || (char.IsPunctuation(ch) && ch != '_') || char.IsSymbol(ch)){
				if(currentWord.Length > 0){
					string word = currentWord.ToString();

					// Replace the word if it exists in the dictionary
					if(replacementsDot.ContainsKey(word)){
						result.Append(replacementsDot[word].replacement);
						appear.Add(replacementsDot[word].appear);
					}else{
						result.Append(word);
					}

					currentWord.Clear();
				}

				// Append the current delimiter (space, punctuation, etc.)
				result.Append(ch);
			}else{
				currentWord.Append(ch);
			}
		}

		// Add the last word if present
		if(currentWord.Length > 0){
			string word = currentWord.ToString();
			if(replacementsDot.ContainsKey(word)){
				result.Append(replacementsDot[word].replacement);
				appear.Add(replacementsDot[word].appear);
			}else{
				result.Append(word);
			}
		}

		return result.ToString();
	}
	
	static string getTranslationMessage(){
		return "//Translated using FragRoom version " + Room.version + " - https://github.com/Dumbelfo08/FragRoom";
	}
	
	static string[] divideLine(string s){
		List<string> r = new List<string>();
		StringBuilder c = new StringBuilder();
		
		for(int i = 0; i < s.Length; i++){
			if(char.IsWhiteSpace(s[i])){
				if(c.Length > 0){
					r.Add(c.ToString());
					c.Clear();
				}
			}else{
				c.Append(s[i]);
			}
		}
		
		if(c.Length > 0){
			r.Add(c.ToString());
		}
		
		return r.ToArray();
	}
}

struct Word{
	public string replacement;
	public string appear;

	public Word(string r, string a){
		replacement = r;
		appear = a;
	}
}