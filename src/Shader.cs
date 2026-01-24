using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using AshLib;

public class Shader : IDisposable{
	
	static int activeShader;
	
	public int id {get; private set;}
	
	Dictionary<string, int> uniforms;
	
	public Shader(string vertex, string fragment, string? geometry, string name = null){
		int vertexShader = GL.CreateShader(ShaderType.VertexShader);
		
		#if DEBUG
			GL.ObjectLabel(
				ObjectLabelIdentifier.Shader,
				vertexShader,
				-1,
				name + " <vert>"
			);
		#endif
		
		GL.ShaderSource(vertexShader, vertex);
		GL.CompileShader(vertexShader);
		
		int compileStatus;
		GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out compileStatus);
		if(compileStatus == 0){
			string log = GL.GetShaderInfoLog(vertexShader);
			throw new Exception("GLSL VERTEX SHADER (" + name + ".vert) COMPILING ERROR:\n" + log);
		}
		
		int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
		
		#if DEBUG
			GL.ObjectLabel(
				ObjectLabelIdentifier.Shader,
				fragmentShader,
				-1,
				name + " <frag>"
			);
		#endif
		
		GL.ShaderSource(fragmentShader, fragment);
		GL.CompileShader(fragmentShader);
		
		GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out compileStatus);
		if(compileStatus == 0){
			string log = GL.GetShaderInfoLog(fragmentShader);
			throw new Exception("GLSL FRAGMENT SHADER (" + name + ".frag) COMPILING ERROR:\n" + log);
		}
		
		int geometryShader = 0;
		if(geometry != null){
			geometryShader = GL.CreateShader(ShaderType.GeometryShader);
			
			#if DEBUG
				GL.ObjectLabel(
					ObjectLabelIdentifier.Shader,
					geometryShader,
					-1,
					name + " <geom>"
				);
			#endif
			
			GL.ShaderSource(geometryShader, geometry);
			GL.CompileShader(geometryShader);
			
			GL.GetShader(geometryShader, ShaderParameter.CompileStatus, out compileStatus);
			if(compileStatus == 0){
				string log = GL.GetShaderInfoLog(geometryShader);
				throw new Exception("GLSL GEOMETRY SHADER (" + name + ".geom) COMPILING ERROR:\n" + log);
			}
		}
		
		this.id = GL.CreateProgram();
		
		#if DEBUG
			GL.ObjectLabel(
				ObjectLabelIdentifier.Program,
				this.id,
				-1,
				name + " <program>"
			);
		#endif
		
		GL.AttachShader(this.id, vertexShader);
		GL.AttachShader(this.id, fragmentShader);
		if(geometry != null){
			GL.AttachShader(this.id, geometryShader);
		}
		GL.LinkProgram(this.id);
		
		int linkStatus;
		GL.GetProgram(this.id, GetProgramParameterName.LinkStatus, out linkStatus);
		
		if(linkStatus == (int)All.False){
			string log = GL.GetProgramInfoLog(this.id);
			throw new Exception("GLSL SHADER (" + name + ") LINKING ERROR:\n" + log);
		}
		
		GL.ValidateProgram(this.id);
		
		int validateStatus;
		GL.GetProgram(this.id, GetProgramParameterName.ValidateStatus, out validateStatus);
		
		if(validateStatus == (int)All.False){
			string log = GL.GetProgramInfoLog(this.id);
			throw new Exception("GLSL SHADER (" + name + ") VALIDATING ERROR:\n" + log);
		}
		
		GL.DetachShader(this.id, vertexShader);
		GL.DetachShader(this.id, fragmentShader);
		if(geometry != null) {
	    	GL.DetachShader(this.id, geometryShader);
	    }
		
		GL.DeleteShader(vertexShader);
	    GL.DeleteShader(fragmentShader);
	    if(geometry != null) {
	    	GL.DeleteShader(geometryShader);
	    }
		
		//Pre-cache uniforms
		
		GL.GetProgram(this.id, GetProgramParameterName.ActiveUniforms, out int count);
		
		uniforms = new Dictionary<string, int>(count);
		
		for(int i = 0; i < count; i++){
			GL.GetActiveUniform(this.id, i, 256, out _, out _, out _, out string uname);
			int location = GL.GetUniformLocation(this.id, uname);
			uniforms[uname] = location;
		}
	}
	
	public void use(){
		if(activeShader == this.id){
			return;
		}
		GL.UseProgram(this.id);
		activeShader = this.id;
	}
	
	public static void stopUsing(){
		GL.UseProgram(0);
		activeShader = 0;
	}
	
	public void setBool(string name, bool data){
		this.use();
		GL.Uniform1(uniforms[name], data ? 1 : 0);
	}
	
	public void setInt(string name, int data){
		this.use();
		GL.Uniform1(uniforms[name], data);
	}
	
	public void setIntArray(string name, int[] data){
		this.use();
		if(!name.EndsWith("[0]")){
			GL.Uniform1(uniforms[name], data.Length, data);
		}else{
			GL.Uniform1(GL.GetUniformLocation(this.id, name), data.Length, data);
		}
	}
	
	//Set one of the elements
	public void setIntArrayMember(string name, int index, int data){
		this.use();
		GL.Uniform1(GL.GetUniformLocation(this.id, name + "[" + index + "]"), data);
	}
	
	public void setFloat(string name, float data){
		this.use();
		GL.Uniform1(uniforms[name], data);
	}
	
	public void setFloatArray(string name, float[] data){
		this.use();
		if(!name.EndsWith("[0]")){
			GL.Uniform1(uniforms[name], data.Length, data);
		}else{
			GL.Uniform1(GL.GetUniformLocation(this.id, name), data.Length, data);
		}
	}
	
	//Set one of the elements
	public void setFloatArrayMember(string name, int index, float data){
		this.use();
		GL.Uniform1(GL.GetUniformLocation(this.id, name + "[" + index + "]"), data);
	}
	
	public void setMatrix4(string name, Matrix4 data){
		this.use();
		GL.UniformMatrix4(uniforms[name], false, ref data);
	}
	
	public void setVector4(string name, Vector4 data){
		this.use();
		GL.Uniform4(uniforms[name], data.X, data.Y, data.Z, data.W);
	}
	
	public void setVector4(string name, Color3 data, float a){
		this.use();
		GL.Uniform4(uniforms[name], (float)data.R / 255f, (float)data.G / 255f, (float)data.B / 255f, a);
	}
	
	public void setVector3(string name, Color3 data){
		this.use();
		GL.Uniform3(uniforms[name], (float)data.R / 255f, (float)data.G / 255f, (float)data.B / 255f);
	}
	
	public void setVector3(string name, Vector3 data){
		this.use();
		GL.Uniform3(uniforms[name], data.X, data.Y, data.Z);
	}
	
	public void setVector2(string name, Vector2 data){
		this.use();
		GL.Uniform2(uniforms[name], data.X, data.Y);
	}
	
	public void setVector2Array(string name, Vector2[] data){
		this.use();
		if(!name.EndsWith("[0]")){
			GL.Uniform2(uniforms[name], data.Length, data.SelectMany(h => new float[]{h.X, h.Y}).ToArray());
		}else{
			GL.Uniform2(GL.GetUniformLocation(this.id, name), data.Length, data.SelectMany(h => new float[]{h.X, h.Y}).ToArray());
		}
	}
	
	//Set one of the elements
	public void setVector2ArrayMember(string name, int index, Vector2 data){
		this.use();
		GL.Uniform2(GL.GetUniformLocation(this.id, name + "[" + index + "]"), data.X, data.Y);
	}
	
	public void setVector2i(string name, Vector2i data){
		this.use();
		GL.Uniform2(uniforms[name], data.X, data.Y);
	}
	
	public void Dispose(){
		if(activeShader == this.id){
			activeShader = 0;
		}
		GL.DeleteProgram(this.id);
		GC.SuppressFinalize(this);
	}
	
	~Shader(){
		Dispose();
	}
}