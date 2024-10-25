using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common.Input;
using StbImageSharp;
using AshLib;

class Room : GameWindow{
	
	private int VAO;
	private Shader mainShader; //We'll keep the shader class because why not Im too lazy
	private int height;
	private int width;
	private DeltaHelper dh;
	
	private Keys closeKey = Keys.Escape;
	private Keys fullscreenKey = Keys.F11;
	private bool allowClose = true;
	private int secondsToClose = -1;
	private int maxFps = 144;
	private bool fullScreened = false;
	private bool allowToggleFullscreen = true;
	private bool fullscreenKeyPressed = false;
	private string? iconPath = null;
	
	private string? filePath;
	
	const string vertexShader = "#version 330 core\nlayout (location = 0) in vec3 aPos; out vec2 fragCoord;void main(){gl_Position = vec4(aPos, 1.0); fragCoord = gl_Position.xy;}";
	const string warningShader = "#version 330 core\nout vec4 fragColor;in vec2 fragCoord;uniform float iTime;uniform vec2 iResolution;float sdEquilateralTriangle(vec2 p,float r){float k=sqrt(3.0);p.x=abs(p.x)-r;p.y+=r/k;if(p.x+k*p.y>0.0)p=vec2(p.x-k*p.y,-k*p.x-p.y)/2.0;p.x-=clamp(p.x,-2.0*r,0.0);return-length(p)*sign(p.y);}float sdSegment(vec2 p,vec2 a,vec2 b){vec2 pa=p-a,ba=b-a;float h=clamp(dot(pa,ba)/dot(ba,ba),0.0,1.0);return length(pa-ba*h);}vec3 palette(float n){float g=sin(n)/3.0+0.5;return vec3(0.9,g,0.0);}void main(){vec2 center=fragCoord;center.x*=iResolution.x/iResolution.y*0.9;center.y+=0.2;float dist=smoothstep(0.04,0.03,sdEquilateralTriangle(center,0.8))-smoothstep(0.10,0.09,distance(center,vec2(0.0,-0.3)))-smoothstep(0.1,0.09,sdSegment(center,vec2(0.0,-0.02),vec2(0.0,0.63)));vec3 color=palette(iTime*2.0)*dist;fragColor=vec4(color,0.0);}";
	const string noFileShader = "#version 330 core\nout vec4 fragColor;in vec2 fragCoord;uniform float iTime;uniform vec2 iResolution;float sdArc(vec2 p,vec2 sc,float ra,float rb){p=vec2(cos(1.25)*p.x-sin(1.25)*p.y,sin(1.25)*p.x+cos(1.25)*p.y);p.x=abs(p.x);return((sc.y*p.x>sc.x*p.y)?length(p-sc*ra):abs(length(p)-ra))-rb;}float sdSegment(vec2 p,vec2 a,vec2 b){vec2 pa=p-a,ba=b-a;float h=clamp(dot(pa,ba)/dot(ba,ba),0.0,1.0);return length(pa-ba*h);}vec3 palette(float n){float g=sin(n)/3.0+0.8;return vec3(0.1,g,0.5);}void main(){vec2 center=fragCoord;center.x*=iResolution.x/iResolution.y;vec2 semicircle=center;semicircle.y-=0.39;semicircle.x-=0.12;float dist=smoothstep(0.03,0.01,sdArc(semicircle,vec2(sin(2.1),cos(2.1)),0.4,0.09))+smoothstep(0.11,0.09,sdSegment(center,vec2(0.0,-0.5),vec2(0.0,0.0)))+smoothstep(0.11,0.09,distance(center,vec2(0.0,-0.8)));dist=min(dist,1.0);vec3 color=palette(iTime*2.0)*dist;fragColor=vec4(color,0.0);}";
	
	public Room(string? s) : base(GameWindowSettings.Default, NativeWindowSettings.Default){
		this.width = 640;
		this.height = 480;
		this.CenterWindow(new Vector2i(640, 480));
		this.Title = "FragRoom";
		this.filePath = s;
	}
	
	public static void Main(string[] args){
		string path = null;
		if(args.Length > 0){
			path = removeQuotes(args[0]);
		}
		
		using(Room ro = new Room(path)){
			ro.Run();
		}
	}
	
	public static string removeQuotes(string p){
		if(p.Length < 1){
			return p;
		}
		char[] c = p.ToCharArray();
		if(c[0] == '\"' && c[c.Length - 1] == '\"'){
			if(c.Length < 2){
				return "";
			}
			return removeQuotes(p.Substring(1, p.Length - 2));
		}
		return p;
	}
	
	private void chooseFileName(){
		if(filePath != null){
			return;
		}
		if(File.Exists("fragment.fgrom")){
			filePath = "fragment.fgrom";
		}else if(File.Exists("fragment.glsl")){
			filePath = "fragment.glsl";
		}else if(File.Exists("shader.fgrom")){
			filePath = "shader.fgrom";
		}else if(File.Exists("shader.glsl")){
			filePath = "shader.glsl";
		}
	}
	
	private string loadFile(){ //load the code and all the special properties
		chooseFileName();
		if(!File.Exists(filePath)){
			Console.WriteLine("File not found!");
			Console.WriteLine("Path: " + filePath);
			
			this.Title = "FragRoom - File not found";
			secondsToClose = 15;
			showMessage("The file \"" + filePath + "\" couldn't be found");
			return noFileShader;
		}
		
		Uri uri = new Uri(Path.GetFullPath(filePath));
		this.Title = "FragRoom - " + Path.GetFileNameWithoutExtension(uri.LocalPath); //Without extension
		
		string shaderCode = File.ReadAllText(filePath);
		
		string[] codeLines = shaderCode.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
		int index = 0;
		while(true){
			if(codeLines[index].Length > 0 && codeLines[index][0] == '@'){
				loadProperty(codeLines[index]);
				index++;
			}else{
				break;
			}
		}
		
		codeLines = codeLines.Skip(index).ToArray();
		
		shaderCode = string.Join('\n', codeLines);
		
		shaderCode = shaderCode.TrimStart();
		
		return shaderCode;
	}
	
	private void loadProperty(string line){
		try{
			line = line.Split("//")[0];
			string[] words = line.Split(":");
			string arg = string.Join(":", words.Skip(1)).TrimStart();
			switch(words[0].TrimEnd()){
				case "@title":
				this.Title = replaceValues(arg);
				break;
				
				case "@width":
				this.Size = new Vector2i((int)UInt32.Parse(words[1]), height);
				break;
				
				case "@height":
				this.Size = new Vector2i(width, (int)UInt32.Parse(words[1]));
				break;
				
				case "@allowClose":
				this.allowClose = (words[1] == "0" ? false : true);
				break;
				
				case "@closeKey":
				if(words[1] == "esc"){
					this.closeKey = Keys.Escape;
					break;
				}
				this.closeKey = (Keys) Int32.Parse(words[1]);
				break;
				
				case "@secsToAutoClose":
				this.secondsToClose = (int)UInt32.Parse(words[1]);
				break;
				
				case "@fullscreen":
				setFullScreen((words[1] == "1" ? true : false));
				break;
				
				case "@allowToggleFullscreen":
				this.allowToggleFullscreen = (words[1] == "0" ? false : true);
				break;
				
				case "@fullscreenKey":
				if(words[1] == "f11"){
					this.fullscreenKey = Keys.F11;
					break;
				}
				this.fullscreenKey = (Keys) Int32.Parse(words[1]);
				break;
				
				case "@grabCursor":
				if(words[1] == "1"){
					CursorState = CursorState.Grabbed;
				}
				break;
				
				case "@maxFps":
				this.maxFps = (int) UInt32.Parse(words[1]);
				break;
				
				case "@icon":
				this.iconPath = arg;
				break;
			}
		} catch(Exception e){
			Console.WriteLine(e.Message);
			Console.WriteLine(e.StackTrace);
		}
	}
	
	private string replaceValues(string s){
		Uri uri = new Uri(Path.GetFullPath(filePath));
		
		s = s.Replace("%p", Path.GetFullPath(uri.LocalPath)); //Full path
		
		s = s.Replace("%f", Path.GetFileName(uri.LocalPath)); //With extension
		
		s = s.Replace("%n", Path.GetFileNameWithoutExtension(uri.LocalPath)); //Without extension
		
		s = s.Replace("%d", DateTime.Now.ToString());
		
		return s;
	}
	
	public void setFullScreen(bool b){
		if(b){
			this.WindowState = WindowState.Fullscreen;
			this.fullScreened = true;
		} else {
			this.WindowState = WindowState.Normal;
			this.fullScreened = false;
		}
	}
	
	private void UniformResize(){
		if(this.mainShader != null){
			this.mainShader.setVector2("iResolution", new Vector2(this.width, this.height));
		}
	}
	
	private void handleIcon(){
		byte[] imageBytes;
		
		if(iconPath == null || !File.Exists(iconPath)){
			//Get the image bytes
			var assembly = Assembly.GetExecutingAssembly();
			
			string resourceName = assembly.GetManifestResourceNames()
			.Single(str => str.EndsWith("icon.png"));
			
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			using (MemoryStream memoryStream = new MemoryStream())
			{
				stream.CopyTo(memoryStream);  // Copy the stream to memory
				imageBytes = memoryStream.ToArray();
			}
		} else{
			imageBytes = File.ReadAllBytes(iconPath);
		}
		
		//Generate the image and put it as icon
		ImageResult image = ImageResult.FromMemory(imageBytes, ColorComponents.RedGreenBlueAlpha);
		if (image == null || image.Data == null){
			Console.WriteLine("Failed to load icon");
			showMessage("Failed to load the icon");
			return;
		}
		
		OpenTK.Windowing.Common.Input.Image i = new OpenTK.Windowing.Common.Input.Image(image.Width, image.Height, image.Data);
		WindowIcon w = new WindowIcon(i);
		
		this.Icon = w;
	}
	
	protected override void OnLoad(){
		dh = new DeltaHelper();
		dh.Start();
		
		string shaderCode = loadFile();
		
		handleIcon();		
		
		float[] vertices = { //Just the full screen will be
			-1f, -1f, 0f,
			-1f, 1f, 0f,
			1f, -1f, 0f,
			1f,  1f, 0f,
			1f, -1f, 0f,
			-1f, 1f, 0f
		};  
		
		int VBO = GL.GenBuffer(); //Initialize VBO
		GL.BindBuffer(BufferTarget.ArrayBuffer, VBO); //Bind VBO
		GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw); //Set VBO to vertices
		
		VAO = GL.GenVertexArray(); //Initialize VAO
		GL.BindVertexArray(VAO); //Bind VAO
		
		GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0); //Set parameters so it knows how to process it. 
																								  //This is for the vertex position argument, the only one
		GL.EnableVertexAttribArray(0); //It is in layout 0, so we set it
		
		GL.BindBuffer(BufferTarget.ArrayBuffer, 0); //Unbind VBO
		GL.BindVertexArray(0); //Unbind VAO
		GL.DeleteBuffer(VBO); //Delete VBO, we wont even need it anymore. If we delete before unbinding the VAO, it will unbind or something idk just dont do it
		
		try{
			mainShader = new Shader(vertexShader, shaderCode, null);
		} catch(Exception e){
			Console.WriteLine("Exception caught!");
			Console.WriteLine(e);
			
			mainShader = new Shader(vertexShader, warningShader, null);
			this.Title = "FragRoom - Warning! Error in shader";
			secondsToClose = 15;
			showMessage("EXCEPTION caught:\n" + e);
		}
		
		base.OnLoad();
	}
	
	protected override void OnUnload(){
		GL.BindVertexArray(0); //Unbind VAO
		GL.DeleteVertexArray(VAO); //Delete VAO for clearing resources
		
		GL.UseProgram(0); //Use no shader program
		GL.DeleteProgram(mainShader.id); //Delete the shader for clearing resources
		
		base.OnUnload();
	}
	
	protected override void OnResize(ResizeEventArgs args){
		GL.Viewport(0, 0, args.Width, args.Height);
		this.width = args.Width;
		this.height = args.Height;
		UniformResize(); //Update the uniform
		base.OnResize(args);
	}
	
	protected override void OnUpdateFrame(FrameEventArgs args){
		if(allowClose && KeyboardState.IsKeyDown(closeKey))
		{
			Close();
		}
		if(secondsToClose != -1 && dh.GetTime() > (double) secondsToClose){
			Close();
		}
		if(allowToggleFullscreen && !fullscreenKeyPressed && KeyboardState.IsKeyDown(fullscreenKey))
		{
			fullscreenKeyPressed = true;
			setFullScreen(!fullScreened);
		}
		if(fullscreenKeyPressed && !KeyboardState.IsKeyDown(fullscreenKey)){
			fullscreenKeyPressed = false;
		}
		base.OnUpdateFrame(args);
	}
	
	protected override void OnRenderFrame(FrameEventArgs args){
		GL.ClearColor(new Color4(0.0f, 0.0f, 0.0f, 1.0f));
		GL.Clear(ClearBufferMask.ColorBufferBit);
		
		mainShader.use();
		GL.BindVertexArray(VAO); //Bind the VAO containg the object
		mainShader.setFloat("iTime", (float) dh.GetTime()); //Set the time
		UniformResize(); //Update iResolution
		GL.DrawArrays(PrimitiveType.Triangles, 0, 6); //IMPORTANT: 6 IS THE NUMBER OF VERTICES, NOT TRIAGNLES
		
		this.Context.SwapBuffers();
		base.OnRenderFrame(args);
		
		dh.Target((double)maxFps);
		dh.Frame();
	}
	
	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public static void showMessage(string message)
    {
        new Thread(() => MessageBox(IntPtr.Zero, message, "Error", 0x00000030)).Start();
    }
}