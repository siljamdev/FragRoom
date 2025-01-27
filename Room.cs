using System;
using System.Text;
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

partial class Room : GameWindow{
	
	private int VAO;
	private Shader mainShader; //We'll keep the shader class because why not Im too lazy
	private Shader bufferShader;
	private int height;
	private int width;
	private DeltaHelper dh;
	private int frameCounter;
	
	private Vector2 mouse;
	
	private Keys closeKey = Keys.Escape;
	private Keys fullscreenKey = Keys.F11;
	private bool allowClose = true;
	private int secondsToClose = -1;
	private int maxFps = 144;
	private bool useVsync = false;
	private bool fullScreened = false;
	private bool allowToggleFullscreen = true;
	private bool fullscreenKeyPressed = false;
	private string? iconPath = null;
	
	private string? filePath;
	private string[] textures = new string[8];
	
	private int bufferA;
	private int bufferB;
	private int textureA;
	private int textureB;
	
	private bool currentBuffer;
	
	private bool uniformTime;
	private bool uniformFrame;
	private bool uniformResolution;
	private bool uniformHour;
	private bool uniformDate;
	private bool uniformFPS;
	private bool uniformMouse;
	private bool uniformTextures;
	private bool uniformBackBuffer;
	
	public const string version = "v1.4.3";
	
	const string vertexShader = "#version 330 core\nlayout (location = 0) in vec2 aPos;out vec2 fragCoord;void main(){gl_Position = vec4(aPos, 0.0, 1.0); fragCoord = gl_Position.xy;}";
	const string bufferFragmentShader = "#version 330 core\nout vec4 fragColor;in vec2 fragCoord;uniform sampler2D buffer;void main(){fragColor = texture(buffer, fragCoord / 2.0 + 0.5);}";
	const string warningShader = "#version 330 core\nout vec4 fragColor;in vec2 fragCoord;uniform float iTime;uniform vec2 iResolution;float sdEquilateralTriangle(vec2 p,float r){float k=sqrt(3.0);p.x=abs(p.x)-r;p.y+=r/k;if(p.x+k*p.y>0.0)p=vec2(p.x-k*p.y,-k*p.x-p.y)/2.0;p.x-=clamp(p.x,-2.0*r,0.0);return-length(p)*sign(p.y);}float sdSegment(vec2 p,vec2 a,vec2 b){vec2 pa=p-a,ba=b-a;float h=clamp(dot(pa,ba)/dot(ba,ba),0.0,1.0);return length(pa-ba*h);}vec3 palette(float n){float g=sin(n)/3.0+0.5;return vec3(0.9,g,0.0);}void main(){vec2 center=fragCoord;center.x*=iResolution.x/iResolution.y*0.9;center.y+=0.2;float dist=smoothstep(0.04,0.03,sdEquilateralTriangle(center,0.8))-smoothstep(0.10,0.09,distance(center,vec2(0.0,-0.3)))-smoothstep(0.1,0.09,sdSegment(center,vec2(0.0,-0.02),vec2(0.0,0.63)));vec3 color=palette(iTime*2.0)*dist;fragColor=vec4(color,0.0);}";
	const string noFileShader = "#version 330 core\nout vec4 fragColor;in vec2 fragCoord;uniform float iTime;uniform vec2 iResolution;float sdArc(vec2 p,vec2 sc,float ra,float rb){p=vec2(cos(1.25)*p.x-sin(1.25)*p.y,sin(1.25)*p.x+cos(1.25)*p.y);p.x=abs(p.x);return((sc.y*p.x>sc.x*p.y)?length(p-sc*ra):abs(length(p)-ra))-rb;}float sdSegment(vec2 p,vec2 a,vec2 b){vec2 pa=p-a,ba=b-a;float h=clamp(dot(pa,ba)/dot(ba,ba),0.0,1.0);return length(pa-ba*h);}vec3 palette(float n){float g=sin(n)/3.0+0.8;return vec3(0.1,g,0.5);}void main(){vec2 center=fragCoord;center.x*=iResolution.x/iResolution.y;vec2 semicircle=center;semicircle.y-=0.39;semicircle.x-=0.12;float dist=smoothstep(0.03,0.01,sdArc(semicircle,vec2(sin(2.1),cos(2.1)),0.4,0.09))+smoothstep(0.11,0.09,sdSegment(center,vec2(0.0,-0.5),vec2(0.0,0.0)))+smoothstep(0.11,0.09,distance(center,vec2(0.0,-0.8)));dist=min(dist,1.0);vec3 color=palette(iTime*2.0)*dist;fragColor=vec4(color,0.0);}";
	
	public Room(string? s) : base(GameWindowSettings.Default, NativeWindowSettings.Default){
		this.width = 640;
		this.height = 480;
		this.CenterWindow(new Vector2i(640, 480));
		this.Title = "FragRoom";
		this.filePath = s;
	}
	
	public Room() : base(GameWindowSettings.Default, new NativeWindowSettings{StartVisible = false}){
		this.Title = "FragRoom";
    }
	
	public static void Main(string[] args){	
		string path = null;
		if(args.Length > 0){
			switch(args[0]){
				case "view": //view command
				if(args.Length < 2){
					showMessage("Not enough arguments");
					return;
				}
				
				path = removeQuotes(args[1]);
				using(Room ro = new Room(path)){
					ro.Run();
				}
				break;
				
				default: //view command
				path = removeQuotes(args[0]);
				using(Room ro = new Room(path)){
					ro.Run();
				}
				break;
				
				case "translate": //translate command
				if(args.Length < 4){
					showMessage("Not enough arguments");
					return;
				}
				
				Translator.handleTranslation(args[1], args[2], removeQuotes(args[3]));
				break;
				
				case "web": //web
				if(args.Length < 2){
					showMessage("Not enough arguments");
					return;
				}
				using(Room ro = new Room()){
					ro.assembleWeb(removeQuotes(args[1]));
				}
				break;
			}
		}else{
			using(Room ro = new Room(path)){
				ro.Run();
			}
		}
	}
	
	private void chooseFileName(){
		if(filePath != null){
			return;
		}
		
		if(File.Exists("fragment.fgrom")){
			filePath = "fragment.fgrom";
		}else if(File.Exists("fragment.glsl")){
			filePath = "fragment.glsl";
		}
	}
	
	private string preprocess(){ //load the code and all the special properties
		chooseFileName();
		if(!File.Exists(filePath)){
			Console.WriteLine("File not found!");
			Console.WriteLine("Path: " + filePath);
			
			this.Title = "FragRoom - File not found";
			secondsToClose = 15;
			this.uniformTime = true;
			this.uniformResolution = true;
			showMessage("The file \"" + filePath + "\" couldn't be found");
			return noFileShader;
		}
		
		if(Path.GetDirectoryName(filePath) != ""){
			Directory.SetCurrentDirectory(Path.GetDirectoryName(filePath));
		}
		
		Uri uri = new Uri(Path.GetFullPath(filePath));
		this.Title = "FragRoom - " + Path.GetFileNameWithoutExtension(uri.LocalPath); //Without extension
		
		string shaderCode = File.ReadAllText(filePath);
		
		string[] codeLines = shaderCode.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
		int index = 0;
		while(true){
			if(codeLines[index].Length > 0 && codeLines[index][0] == '@'){
				loadProperty(codeLines[index]);
				codeLines[index] = "";
				index++;
			}else{
				break;
			}
		}
		
		while(true){
			if(codeLines[index] != ""){
				break;
			}
			index++;
		}
		
		if(index != 0){
			//Search where the actual code starts, and put the first line on line 0, instead of just deleting the first part of the file, so errors will show up in the lines they actually are in
			codeLines[0] = codeLines[index];
			codeLines[index] = "";
		}
		
		shaderCode = string.Join('\n', codeLines);
		
		shaderCode = shaderCode.TrimStart();
		
		return shaderCode;
	}
	
	private void loadProperty(string line){
		try{
			line = line.Split("//")[0];
			string[] words = line.Split(":");
			string arg = string.Join(":", words.Skip(1)).TrimStart();
			words[1] = words[1].Trim();
			switch(words[0].TrimEnd()){
				case "@title":
				this.Title = replaceTitleValues(arg);
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
				
				case "@vsync":
				useVsync = words[1] == "1" ? true : false;
				VSync = VSyncMode.On;
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
				this.iconPath = removeQuotes(arg);
				break;
				
				case "@texture0L":
				this.textures[0] = "L" + removeQuotes(arg);
				break;
				
				case "@texture1L":
				this.textures[1] = "L" + removeQuotes(arg);
				break;
				
				case "@texture2L":
				this.textures[2] = "L" + removeQuotes(arg);
				break;
				
				case "@texture3L":
				this.textures[3] = "L" + removeQuotes(arg);
				break;
				
				case "@texture4L":
				this.textures[4] = "L" + removeQuotes(arg);
				break;
				
				case "@texture5L":
				this.textures[5] = "L" + removeQuotes(arg);
				break;
				
				case "@texture6L":
				this.textures[6] = "L" + removeQuotes(arg);
				break;
				
				case "@texture7L":
				this.textures[7] = "L" + removeQuotes(arg);
				break;
				
				case "@texture0N":
				this.textures[0] = "N" + removeQuotes(arg);
				break;
				
				case "@texture1N":
				this.textures[1] = "N" + removeQuotes(arg);
				break;
				
				case "@texture2N":
				this.textures[2] = "N" + removeQuotes(arg);
				break;
				
				case "@texture3N":
				this.textures[3] = "N" + removeQuotes(arg);
				break;
				
				case "@texture4N":
				this.textures[4] = "N" + removeQuotes(arg);
				break;
				
				case "@texture5N":
				this.textures[5] = "N" + removeQuotes(arg);
				break;
				
				case "@texture6N":
				this.textures[6] = "N" + removeQuotes(arg);
				break;
				
				case "@texture7N":
				this.textures[7] = "N" + removeQuotes(arg);
				break;
			}
		} catch(Exception e){
			Console.WriteLine(e);
		}
	}
	
	private string replaceTitleValues(string s){
		Uri uri = new Uri(Path.GetFullPath(filePath));
		
		s = s.Replace("%p", Path.GetFullPath(uri.LocalPath)); //Full path
		
		s = s.Replace("%f", Path.GetFileName(uri.LocalPath)); //With extension
		
		s = s.Replace("%n", Path.GetFileNameWithoutExtension(uri.LocalPath)); //Without extension
		
		s = s.Replace("%d", DateTime.Now.ToString());
		
		return s;
	}
	
	private void initializeUniforms(){
		try{
			// Get the number of active uniforms
			GL.GetProgram(mainShader.id, GetProgramParameterName.ActiveUniforms, out int uniformCount);
			
			// Iterate through all active uniforms
			for (int i = 0; i < uniformCount; i++){
				// Get the uniform name, size, and type
				GL.GetActiveUniform(mainShader.id, i, 256, out _, out int size, out ActiveUniformType type, out string name);
				
				if(size == 1 && type == ActiveUniformType.Float && name == "iTime"){
					uniformTime = true;
				}else if(size == 1 && type == ActiveUniformType.Int && name == "iFrame"){
					uniformFrame = true;
				}else if(size == 1 && type == ActiveUniformType.FloatVec2 && name == "iResolution"){
					uniformResolution = true;
				}else if(size == 1 && type == ActiveUniformType.FloatVec3 && name == "iHour"){
					uniformHour = true;
				}else if(size == 1 && type == ActiveUniformType.FloatVec3 && name == "iDate"){
					uniformDate = true;
				}else if(size == 1 && type == ActiveUniformType.Float && name == "iFps"){
					uniformFPS = true;
				}else if(size == 1 && type == ActiveUniformType.FloatVec2 && name == "iMouse"){
					uniformMouse = true;
				}else if(size == 8 && type == ActiveUniformType.Sampler2D && name == "iTexture[0]"){
					uniformTextures = true;
					this.tryLoadTextures();
				}else if(size == 1 && type == ActiveUniformType.Sampler2D && name == "iBackBuffer"){
					uniformBackBuffer = true;
				}
			}
		}catch(Exception e){
			Console.WriteLine(e);
		}
	}
	
	private void tryLoadTextures(){
		for(int i = 0; i < textures.Length; i++){
			if(textures[i] != null && File.Exists(textures[i].Substring(1))){
				Texture2D t;
				if(textures[i][0] == 'L'){
					t = Texture2D.generateFromFile(textures[i].Substring(1), TextureParams.Smooth, i + 1);
				}else if(textures[i][0] == 'N'){
					t = Texture2D.generateFromFile(textures[i].Substring(1), TextureParams.Default, i + 1);
				}else{
					t = Texture2D.generateFromFile(textures[i].Substring(1), TextureParams.Default, i + 1);
				}
				
				t.bind();
				mainShader.setInt("iTexture[" + i + "]", i + 1);
			}
		}
	}
	
	void setFullScreen(bool b){
		if(b){
			MonitorInfo mi = Monitors.GetMonitorFromWindow(this);
			WindowState = WindowState.Fullscreen;
			this.CurrentMonitor = mi.Handle;
			fullScreened = true;
			if(useVsync){
				VSync = VSyncMode.On;
			}
		}else{
			WindowState = WindowState.Normal;
			fullScreened = false;
			if(useVsync){
				VSync = VSyncMode.On;
			}
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
	
	private void generateBuffers(){
		bufferShader = new Shader(vertexShader, bufferFragmentShader, null);
		
		bufferA = GL.GenFramebuffer();
		GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferA);
		
		textureA = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, textureA);
		
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
		
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
		
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureA, 0);
		
		if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete){
			throw new Exception("Framebuffer A is not complete!");
		}
		
		bufferB = GL.GenFramebuffer();
		GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferB);
		
		textureB = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, textureB);
		
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
		
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
		
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureB, 0);
		
		if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete){
			throw new Exception("Framebuffer B is not complete!");
		}
	}
	
	private void uniformResize(){
		if(this.mainShader != null && uniformResolution){
			this.mainShader.setVector2("iResolution", new Vector2(this.width, this.height));
		}
	}
	
	protected override void OnLoad(){
		dh = new DeltaHelper();
		
		string shaderCode = preprocess();
		
		handleIcon(); //set app icon
		
		//create shader program
		try{
			mainShader = new Shader(vertexShader, shaderCode, null);
		} catch(Exception e){
			Console.WriteLine("Exception caught!");
			Console.WriteLine(e);
			
			mainShader = new Shader(vertexShader, warningShader, null);
			this.uniformTime = true;
			this.uniformResolution = true;
			this.Title = "FragRoom - Warning! Error in shader";
			secondsToClose = 15;
			showMessage("EXCEPTION caught:\n" + e);
		}
		
		//create mesh
		float[] vertices = { //Just the full screen will be
			-1f, -1f,
			-1f, 1f,
			1f, -1f,
			1f,  1f,
			1f, -1f,
			-1f, 1f
		};
		
		int VBO = GL.GenBuffer(); //Initialize VBO
		GL.BindBuffer(BufferTarget.ArrayBuffer, VBO); //Bind VBO
		GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw); //Set VBO to vertices
		
		VAO = GL.GenVertexArray(); //Initialize VAO
		GL.BindVertexArray(VAO); //Bind VAO
		
		GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0); //Set parameters so it knows how to process it. 
																								  //This is for the vertex position argument, the only one
		GL.EnableVertexAttribArray(0); //It is in layout 0, so we set it
		
		GL.BindBuffer(BufferTarget.ArrayBuffer, 0); //Unbind VBO
		GL.BindVertexArray(0); //Unbind VAO
		GL.DeleteBuffer(VBO); //Delete VBO, we wont even need it anymore. If we delete before unbinding the VAO, it will unbind or something idk just dont do it
		
		GL.BindVertexArray(VAO); //Bind the VAO containg the object
		
		mainShader.use();
		
		this.initializeUniforms();
		
		if(uniformBackBuffer){
			generateBuffers();
			GL.ActiveTexture(TextureUnit.Texture0); //all buffer operations are here
		}
		
		GL.ClearColor(new Color4(0.0f, 0.0f, 0.0f, 1.0f));
		
		frameCounter = 0;
		dh.Start();
		
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
		uniformResize(); //Update the uniform
		
		if(uniformBackBuffer){
			GL.BindTexture(TextureTarget.Texture2D, textureA);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			
			GL.BindTexture(TextureTarget.Texture2D, textureB);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			
			if(currentBuffer){
				GL.BindTexture(TextureTarget.Texture2D, textureB);
			}else{
				GL.BindTexture(TextureTarget.Texture2D, textureA);
			}
		}
		
		base.OnResize(args);
	}
	
	protected override void OnMouseMove(MouseMoveEventArgs e){
		if(uniformMouse){
			mouse = new Vector2((e.X / this.width) * 2f - 1f, (e.Y / this.height) * 2f - 1f);
			mouse.Y = -mouse.Y;
		}
        
		base.OnMouseMove(e);
    }
	
	protected override void OnUpdateFrame(FrameEventArgs args){
		if(allowClose && KeyboardState.IsKeyDown(closeKey)){
			Close();
		}
		
		if(secondsToClose != -1 && dh.GetTime() > (double) secondsToClose){
			Close();
		}
		
		if(allowToggleFullscreen && !fullscreenKeyPressed && KeyboardState.IsKeyDown(fullscreenKey)){
			fullscreenKeyPressed = true;
			setFullScreen(!fullScreened);
		}
		
		if(fullscreenKeyPressed && !KeyboardState.IsKeyDown(fullscreenKey)){
			fullscreenKeyPressed = false;
		}
		
		base.OnUpdateFrame(args);
	}
	
	protected override void OnRenderFrame(FrameEventArgs args){
		if(uniformBackBuffer){
			if(currentBuffer){
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferA);
			}else{
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferB);
			}
			
			GL.Clear(ClearBufferMask.ColorBufferBit);
			
			mainShader.use();
		}
		
		if(uniformTime){
			mainShader.setFloat("iTime", (float) dh.GetTime()); //Set the running time
		}
		
		if(uniformFrame){
			mainShader.setInt("iFrame", frameCounter); //Set the current frame number
		}
		
		if(uniformHour){
			mainShader.setVector3("iHour", new Vector3(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)); //Set the hour
		}
		
		if(uniformDate){
			mainShader.setVector3("iDate", new Vector3(DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year)); //Set the date
		}
		
		if(uniformFPS){
			mainShader.setFloat("iFps", (float) dh.fps); //Set the fps
		}
		
		if(uniformMouse){
			mainShader.setVector2("iMouse", mouse); //Set the mouse pointer coords
		}
		
		GL.DrawArrays(PrimitiveType.Triangles, 0, 6); //IMPORTANT: 6 IS THE NUMBER OF VERTICES, NOT TRIAGNLES
		
		if(uniformBackBuffer){
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			bufferShader.use();
			
			if(currentBuffer){
				GL.BindTexture(TextureTarget.Texture2D, textureA);
			}else{
				GL.BindTexture(TextureTarget.Texture2D, textureB);
			}
			
			GL.Clear(ClearBufferMask.ColorBufferBit);
			
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6); //IMPORTANT: 6 IS THE NUMBER OF VERTICES, NOT TRIAGNLES
			
			currentBuffer = !currentBuffer;
		}
		
		this.Context.SwapBuffers();
		base.OnRenderFrame(args);
		
		dh.Target((double)maxFps);
		frameCounter++;
		dh.Frame();
	}
	
	//utility
	public static string removeQuotes(string p){
		if(p.Length < 1){
			return p;
		}
		p = p.Trim();
		
		if(p[0] == '\"' && p[p.Length - 1] == '\"'){
			if(p.Length < 2){
				return "";
			}
			return p.Substring(1, p.Length - 2);
		}
		return p;
	}
	
	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public static void showMessage(string message)
    {
        new Thread(() => MessageBox(IntPtr.Zero, message, "Error", 0x00000030)).Start();
    }
}