using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;

public class Texture2D : IDisposable{
	
	static int[] activeTexture = new int[8];
	
	public int id{get; private set;}
	
	public int width{get; private set;}
	public int height{get; private set;}
	
	public PixelInternalFormat internalFormat{get; private set;}
	
	int _unit;
	
	public TextureUnit unit => unitFromInt(_unit);
	
	public Texture2D(ImageResult image, TextureParams tp, int u = 0, string name = null){
		this.internalFormat = ConvertFormatInternal(image.Comp);
		
		this.width = image.Width; //Extract this needed values
		this.height = image.Height;
		
		this._unit = u;
		
		this.id = GL.GenTexture(); //Generate the handle for the texture
		
		#if DEBUG
			GL.ObjectLabel(
				ObjectLabelIdentifier.Texture,
				this.id,
				-1,
				name + " <texture>"
			);
		#endif
		
		GL.ActiveTexture(unit);
		GL.BindTexture(TextureTarget.Texture2D, this.id); //bind it
		
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) tp.wrapS); //Set the wrap parameters
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) tp.wrapT);
		
		if(tp.wrapS == TextureWrapMode.ClampToBorder || tp.wrapT == TextureWrapMode.ClampToBorder){
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[]{tp.borderColor.X, tp.borderColor.Y, tp.borderColor.Z}); //if needed set the border color
		}
		
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) tp.filterMin); //set upscaling/downscaling filter options
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) tp.filterMax);
		
		
		GL.TexImage2D(TextureTarget.Texture2D, 0, this.internalFormat, this.width, this.height, 0, ConvertFormatSource(image.Comp), PixelType.UnsignedByte, image.Data); //actually generate the texture
		
		//if needed, generate mipmaps
		if(tp.filterMin == TextureMinFilter.NearestMipmapNearest || tp.filterMin == TextureMinFilter.LinearMipmapNearest || tp.filterMin == TextureMinFilter.NearestMipmapLinear || tp.filterMin == TextureMinFilter.LinearMipmapLinear){
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
		}
    	
    	GL.BindTexture(TextureTarget.Texture2D, 0); //unbind texture
		activeTexture[_unit] = 0;
	}
	
	//Disposes of the stream
	public static Texture2D fromStream(Stream s, TextureParams tp, int u = 0, string name = null){
		ImageResult image = ImageResult.FromStream(s, ConvertFormatSTB(tp.imageFormat));
		if(image == null || image.Data == null){
			throw new Exception("Image loading failed from stream (" + name + ")");
		}
		Texture2D t = new Texture2D(image, tp, u, name);
		return t;
	}
	
	public static Texture2D fromFile(string path, TextureParams tp, int u = 0){
		using FileStream fs = File.OpenRead(path);
		try{
			return fromStream(fs, tp, u, path);
		}catch(Exception e){
			throw new Exception("Image loading failed from file: " + path, e);
		}
	}
	
	public static Texture2D fromBytes(byte[] data, TextureParams tp, int u = 0, string name = null){
		ImageResult image = ImageResult.FromMemory(data, ConvertFormatSTB(tp.imageFormat));
		if (image == null || image.Data == null){
			throw new Exception("Image loading failed from byte array (" + name + ")");
		}
		Texture2D t = new Texture2D(image, tp, u, name);
		return t;
	}
	
	static ColorComponents ConvertFormatSTB(PixelFormat pixelFormat){
        switch(pixelFormat){
            case PixelFormat.Rgb:
                return ColorComponents.RedGreenBlue;
            case PixelFormat.Rgba:
                return ColorComponents.RedGreenBlueAlpha;
			case PixelFormat.Red:
                return ColorComponents.Grey;
			case PixelFormat.Rg:
                return ColorComponents.GreyAlpha;
            // Add more cases for other pixel formats as needed
            default:
                throw new ArgumentException("Unsupported pixel format in conversion: " + pixelFormat);
        }
    }
	
	static PixelInternalFormat ConvertFormatInternal(ColorComponents cc){
        switch(cc){
            case ColorComponents.Grey:
                return PixelInternalFormat.R8;
            case ColorComponents.GreyAlpha:
                return PixelInternalFormat.Rg8;
			case ColorComponents.RedGreenBlue:
                return PixelInternalFormat.Rgb8;
            case ColorComponents.RedGreenBlueAlpha:
                return PixelInternalFormat.Rgba8;
            // Add more cases for other pixel formats as needed
            default:
                throw new ArgumentException("Unsupported pixel format in conversion: " + cc);
        }
    }
	
	static PixelFormat ConvertFormatSource(ColorComponents cc){
        switch(cc){
            case ColorComponents.Grey:
                return PixelFormat.Red;
            case ColorComponents.GreyAlpha:
                return PixelFormat.Rg;
			case ColorComponents.RedGreenBlue:
                return PixelFormat.Rgb;
            case ColorComponents.RedGreenBlueAlpha:
                return PixelFormat.Rgba;
            // Add more cases for other pixel formats as needed
            default:
                throw new ArgumentException("Unsupported pixel format in conversion: " + cc);
        }
    }
	
	public void bind(){
		if(activeTexture[_unit] == this.id){
			return;
		}
		GL.ActiveTexture(unit);
		GL.BindTexture(TextureTarget.Texture2D, this.id);
		activeTexture[_unit] = this.id;
	}
	
	public static void unbind(int unit = 0){
		GL.ActiveTexture(unitFromInt(unit));
		GL.BindTexture(TextureTarget.Texture2D, 0);
		activeTexture[unit] = 0;
	}
	
	private static TextureUnit unitFromInt(int u){
		switch(u){
			case 0:
			return TextureUnit.Texture0;
			
			case 1:
			return TextureUnit.Texture1;
			
			case 2:
			return TextureUnit.Texture2;
			
			case 3:
			return TextureUnit.Texture3;
			
			case 4:
			return TextureUnit.Texture4;
			
			case 5:
			return TextureUnit.Texture5;
			
			case 6:
			return TextureUnit.Texture6;
			
			case 7:
			return TextureUnit.Texture7;
			
			default:
			return TextureUnit.Texture0;
		}
	}
	
	public static void cleanup(int tid){
		GL.DeleteTexture(tid);
	}
	
	public void Dispose(){
		cleanup(this.id);
		
		if(activeTexture[_unit] == this.id){
			activeTexture[_unit] = 0;
		}
	}
}

public struct TextureParams{
	
	public static readonly TextureParams Default = new TextureParams();
	public static readonly TextureParams Smooth = new TextureParams(){filterMin = TextureMinFilter.LinearMipmapLinear, filterMax = TextureMagFilter.Linear};
	public static readonly TextureParams Noise = new TextureParams(){filterMin = TextureMinFilter.NearestMipmapLinear, filterMax = TextureMagFilter.Linear, wrapS = TextureWrapMode.MirroredRepeat, wrapT = TextureWrapMode.MirroredRepeat};
	
	public PixelFormat imageFormat = PixelFormat.Rgba;
	
	public TextureWrapMode wrapS = TextureWrapMode.Repeat;
	public TextureWrapMode wrapT = TextureWrapMode.Repeat;
	public Vector3 borderColor = new Vector3(0f, 0f, 0f);
	
	public TextureMinFilter filterMin = TextureMinFilter.NearestMipmapNearest;
	public TextureMagFilter filterMax = TextureMagFilter.Nearest;
	
	public TextureParams(){ //Constructor for setting defaults

	}
}