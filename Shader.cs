using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Shader {
	public int id;
	
	
	public Shader(string vertex, string fragment, string? geometry) {
		int vertexShader = GL.CreateShader(ShaderType.VertexShader);
		GL.ShaderSource(vertexShader, vertex);
		GL.CompileShader(vertexShader);
		
		int compileStatus;
		GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out compileStatus);
		if (compileStatus == 0)
		{
			string log = GL.GetShaderInfoLog(vertexShader);
			throw new Exception("GLSL VERTEX SHADER COMPILING ERROR:\n"+log);
		}
		
		int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
		GL.ShaderSource(fragmentShader, fragment);
		GL.CompileShader(fragmentShader);
		
		GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out compileStatus);
		if (compileStatus == 0)
		{
			string log = GL.GetShaderInfoLog(fragmentShader);
			throw new Exception("GLSL FRAGMENT SHADER COMPILING ERROR:\n"+log);
		}
		
		int geometryShader = 0;
		if(geometry != null) {
			geometryShader = GL.CreateShader(ShaderType.GeometryShader);
			GL.ShaderSource(geometryShader, geometry);
			GL.CompileShader(geometryShader);
			
			GL.GetShader(geometryShader, ShaderParameter.CompileStatus, out compileStatus);
			if (compileStatus == 0)
			{
				string log = GL.GetShaderInfoLog(geometryShader);
				throw new Exception("GLSL GEOMETRY SHADER COMPILING ERROR:\n"+log);
			}
		}
		
		this.id = GL.CreateProgram();
		GL.AttachShader(this.id, vertexShader);
		GL.AttachShader(this.id, fragmentShader);
		if(geometry != null) {
			GL.AttachShader(this.id, geometryShader);
		}
		GL.LinkProgram(this.id);
		GL.ValidateProgram(this.id);
		
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
	}
	
	public void use() {
		GL.UseProgram(this.id);
	}
	
	public void setBool(string name, bool data){
		GL.Uniform1(GL.GetUniformLocation(this.id, name), data ? 1 : 0);
	}
	
	public void setInt(string name, int data){
		GL.Uniform1(GL.GetUniformLocation(this.id, name), data);
	}
	public void setFloat(string name, float data){
		GL.Uniform1(GL.GetUniformLocation(this.id, name), data);
	}
	public void setMatrix4(string name, Matrix4 data){
		GL.UniformMatrix4(GL.GetUniformLocation(this.id, name), false, ref data);
	}
	public void setVector4(string name, Vector4 data){
		GL.Uniform4(GL.GetUniformLocation(this.id, name), data.X, data.Y, data.Z, data.W);
	}
	public void setVector3(string name, Vector3 data){
		GL.Uniform3(GL.GetUniformLocation(this.id, name), data.X, data.Y, data.Z);
	}
	public void setVector2(string name, Vector2 data){
		GL.Uniform2(GL.GetUniformLocation(this.id, name), data.X, data.Y);
	}
}